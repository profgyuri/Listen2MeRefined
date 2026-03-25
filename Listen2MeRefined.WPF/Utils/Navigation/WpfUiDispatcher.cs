using System.Windows.Threading;
using Listen2MeRefined.Application.Utils;

namespace Listen2MeRefined.WPF.Utils.Navigation;

public sealed class WpfUiDispatcher : IUiDispatcher
{
    private readonly Dispatcher _dispatcher;

    public WpfUiDispatcher(Dispatcher dispatcher)
        => _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

    public bool CheckAccess() => _dispatcher.CheckAccess();

    public Task InvokeAsync(Action action, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (ct.IsCancellationRequested)
        {
            return Task.FromCanceled(ct);
        }

        if (_dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return _dispatcher.InvokeAsync(action, DispatcherPriority.DataBind, ct).Task;
    }

    public Task InvokeAsync(Func<Task> func, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (ct.IsCancellationRequested)
        {
            return Task.FromCanceled(ct);
        }

        if (_dispatcher.CheckAccess())
        {
            return func();
        }

        return _dispatcher
            .InvokeAsync(func, DispatcherPriority.DataBind, ct)
            .Task
            .Unwrap();
    }

    public Task<T> InvokeAsync<T>(Func<T> func, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (ct.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(ct);
        }

        return _dispatcher.CheckAccess()
            ? Task.FromResult(func())
            : _dispatcher.InvokeAsync(func, DispatcherPriority.DataBind, ct).Task;
    }
}
