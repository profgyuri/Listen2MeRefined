using Listen2MeRefined.Application.Utils;
using Serilog;

namespace Listen2MeRefined.Infrastructure.Startup;

public sealed class ExternalAudioOpenInbox : IExternalAudioOpenInbox
{
    private readonly ILogger _logger;
    private readonly Lock _gate = new();
    private readonly Queue<IReadOnlyList<string>> _pending = [];
    private Action<IReadOnlyList<string>>? _consumer;

    public ExternalAudioOpenInbox(ILogger logger)
    {
        _logger = logger;
    }

    public void Enqueue(IReadOnlyList<string> paths)
    {
        var normalized = NormalizePaths(paths);
        if (normalized.Count == 0)
        {
            return;
        }

        Action<IReadOnlyList<string>>? consumer;
        lock (_gate)
        {
            consumer = _consumer;
            if (consumer is null)
            {
                _pending.Enqueue(normalized);
                return;
            }
        }

        try
        {
            consumer(normalized);
        }
        catch (Exception e)
        {
            _logger.Warning(e, "[ExternalAudioOpenInbox] Consumer threw while handling shell-open request.");
        }
    }

    public IDisposable RegisterConsumer(Action<IReadOnlyList<string>> consumer, bool replayPending = true)
    {
        ArgumentNullException.ThrowIfNull(consumer);

        IReadOnlyList<string>[] backlog = [];
        lock (_gate)
        {
            _consumer = consumer;
            if (replayPending && _pending.Count > 0)
            {
                backlog = _pending.ToArray();
                _pending.Clear();
            }
        }

        foreach (var pending in backlog)
        {
            try
            {
                consumer(pending);
            }
            catch (Exception e)
            {
                _logger.Warning(e, "[ExternalAudioOpenInbox] Consumer threw while replaying pending shell-open request.");
            }
        }

        return new ConsumerRegistration(this, consumer);
    }

    private void UnregisterConsumer(Action<IReadOnlyList<string>> consumer)
    {
        lock (_gate)
        {
            if (ReferenceEquals(_consumer, consumer))
            {
                _consumer = null;
            }
        }
    }

    private static IReadOnlyList<string> NormalizePaths(IReadOnlyList<string> paths)
    {
        return paths
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed class ConsumerRegistration : IDisposable
    {
        private readonly ExternalAudioOpenInbox _owner;
        private Action<IReadOnlyList<string>>? _consumer;

        public ConsumerRegistration(ExternalAudioOpenInbox owner, Action<IReadOnlyList<string>> consumer)
        {
            _owner = owner;
            _consumer = consumer;
        }

        public void Dispose()
        {
            var consumer = Interlocked.Exchange(ref _consumer, null);
            if (consumer is null)
            {
                return;
            }

            _owner.UnregisterConsumer(consumer);
        }
    }
}
