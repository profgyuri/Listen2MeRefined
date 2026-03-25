using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.Messages;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Listen2MeRefined.WPF.Utils;

internal sealed class SingleInstanceFileOpenBridge : IDisposable
{
    private const string MutexName = "Listen2MeRefined.SingleInstance";
    private const string PipeName = "Listen2MeRefined.OpenAudioPipe";
    private const int ConnectTimeoutPerAttemptMs = 1000;
    private const int MaxConnectAttempts = 8;
    private const int RetryDelayMs = 200;

    private readonly ILogger _logger;
    private readonly Mutex _mutex;
    private readonly bool _isPrimaryInstance;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Lock _listenerGate = new();

    private IMessenger? _messenger;
    private bool _listenerStarted;

    /// <summary>
    /// Initializes single-instance coordination.
    /// </summary>
    /// <param name="logger">Logger used for bridge diagnostics.</param>
    public SingleInstanceFileOpenBridge(ILogger logger)
    {
        _logger = logger;
        _mutex = new Mutex(true, MutexName, out var createdNew);
        _isPrimaryInstance = createdNew;
    }

    /// <summary>
    /// Gets whether this process is the primary instance responsible for handling forwarded open requests.
    /// </summary>
    public bool IsPrimaryInstance => _isPrimaryInstance;

    /// <summary>
    /// Starts pipe listening in the primary instance using the provided messenger.
    /// </summary>
    public void AttachMessenger(IMessenger messenger)
    {
        ArgumentNullException.ThrowIfNull(messenger);

        if (!_isPrimaryInstance)
        {
            return;
        }

        lock (_listenerGate)
        {
            _messenger = messenger;
            if (_listenerStarted || _shutdown.IsCancellationRequested)
            {
                return;
            }

            _listenerStarted = true;
            _ = Task.Run(() => ListenAsync(_shutdown.Token));
        }
    }

    /// <summary>
    /// Forwards shell-open file paths to the primary process through the named pipe.
    /// </summary>
    /// <param name="paths">File paths to forward.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> when forwarding succeeded; otherwise <see langword="false"/>.</returns>
    public async Task<bool> ForwardToPrimaryAsync(IEnumerable<string> paths, CancellationToken ct = default)
    {
        var normalized = paths
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (normalized.Length == 0)
        {
            _logger.Debug("[SingleInstanceFileOpenBridge] No paths to forward to primary instance");
            return true;
        }

        var payload = JsonSerializer.Serialize(normalized);

        for (var attempt = 1; attempt <= MaxConnectAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                await client.ConnectAsync(ConnectTimeoutPerAttemptMs, ct).ConfigureAwait(false);

                await using var writer = new StreamWriter(client, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
                await writer.WriteAsync(payload.AsMemory(), ct).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);

                _logger.Information(
                    "[SingleInstanceFileOpenBridge] Forwarded {Count} path(s) to primary instance on attempt {Attempt}/{MaxAttempts}",
                    normalized.Length,
                    attempt,
                    MaxConnectAttempts);
                return true;
            }
            catch (TimeoutException) when (attempt < MaxConnectAttempts && !ct.IsCancellationRequested)
            {
                _logger.Debug(
                    "[SingleInstanceFileOpenBridge] Primary pipe not ready (attempt {Attempt}/{MaxAttempts}), retrying",
                    attempt,
                    MaxConnectAttempts);
                await Task.Delay(RetryDelayMs, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < MaxConnectAttempts && !ct.IsCancellationRequested)
            {
                _logger.Debug(
                    ex,
                    "[SingleInstanceFileOpenBridge] Forward attempt {Attempt}/{MaxAttempts} failed, retrying",
                    attempt,
                    MaxConnectAttempts);
                await Task.Delay(RetryDelayMs, ct).ConfigureAwait(false);
            }
        }

        _logger.Warning(
            "[SingleInstanceFileOpenBridge] Failed forwarding open request to primary instance after {MaxAttempts} attempts",
            MaxConnectAttempts);
        return false;
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        _logger.Information("[SingleInstanceFileOpenBridge] Primary instance pipe listener started");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(ct).ConfigureAwait(false);

                using var reader = new StreamReader(server, Encoding.UTF8, leaveOpen: true);
                var payload = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

                var paths = JsonSerializer.Deserialize<string[]>(payload) ?? [];
                if (paths.Length == 0)
                {
                    _logger.Debug("[SingleInstanceFileOpenBridge] Received empty forwarded path payload");
                    continue;
                }

                _logger.Information(
                    "[SingleInstanceFileOpenBridge] Received {Count} forwarded path(s) from secondary instance",
                    paths.Length);

                if (_messenger is null)
                {
                    _logger.Warning("[SingleInstanceFileOpenBridge] Messenger is not attached yet; dropping forwarded paths.");
                    continue;
                }

                _messenger.Send(new ExternalAudioFilesOpenedMessage(paths));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "[SingleInstanceFileOpenBridge] Pipe listener error");
                await Task.Delay(250, ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Stops listening and releases single-instance resources.
    /// </summary>
    public void Dispose()
    {
        _shutdown.Cancel();
        _shutdown.Dispose();

        if (_isPrimaryInstance)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }
}
