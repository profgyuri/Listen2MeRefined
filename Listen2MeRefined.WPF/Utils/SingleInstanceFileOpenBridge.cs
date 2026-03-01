using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Autofac;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;

namespace Listen2MeRefined.WPF.Utils;

internal sealed class SingleInstanceFileOpenBridge : IDisposable
{
    private const string MutexName = "Listen2MeRefined.SingleInstance";
    private const string PipeName = "Listen2MeRefined.OpenAudioPipe";

    private readonly ILogger _logger;
    private readonly Mutex _mutex;
    private readonly bool _isPrimaryInstance;
    private readonly CancellationTokenSource _shutdown = new();

    /// <summary>
    /// Initializes single-instance coordination and starts pipe listening in the primary instance.
    /// </summary>
    /// <param name="logger">Logger used for bridge diagnostics.</param>
    public SingleInstanceFileOpenBridge(ILogger logger)
    {
        _logger = logger;
        _mutex = new Mutex(true, MutexName, out var createdNew);
        _isPrimaryInstance = createdNew;

        if (_isPrimaryInstance)
        {
            _ = Task.Run(() => ListenAsync(_shutdown.Token));
        }
    }

    /// <summary>
    /// Gets whether this process is the primary instance responsible for handling forwarded open requests.
    /// </summary>
    public bool IsPrimaryInstance => _isPrimaryInstance;

    /// <summary>
    /// Forwards shell-open file paths to the primary process through the named pipe.
    /// </summary>
    /// <param name="paths">File paths to forward.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> when forwarding succeeded; otherwise <see langword="false"/>.</returns>
    public async Task<bool> ForwardToPrimaryAsync(IEnumerable<string> paths, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(paths.ToArray());

        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            await client.ConnectAsync(1000, ct);

            await using var writer = new StreamWriter(client, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
            await writer.WriteAsync(payload.AsMemory(), ct);
            await writer.FlushAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "[SingleInstanceFileOpenBridge] Failed forwarding open request to primary instance");
            return false;
        }
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(ct);

                using var reader = new StreamReader(server, Encoding.UTF8, leaveOpen: true);
                var payload = await reader.ReadToEndAsync();

                var paths = JsonSerializer.Deserialize<string[]>(payload) ?? [];
                if (paths.Length == 0)
                {
                    continue;
                }

                using var scope = Dependency.IocContainer.GetContainer().BeginLifetimeScope();
                var mediator = scope.Resolve<IMediator>();
                await mediator.Publish(new ExternalAudioFilesOpenedNotification(paths), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "[SingleInstanceFileOpenBridge] Pipe listener error");
                await Task.Delay(250, ct);
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
