using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels;

/// <summary>
/// Provides base observable behavior, one-time async initialization, and safe async execution helpers.
/// </summary>
public abstract class ViewModelBase : ObservableObject, IInitializeAsync, IDisposable
{
    private readonly IErrorHandler _errorHandler;
    private readonly SemaphoreSlim _initializeSync = new(1, 1);
    private int _hasMessageRegistrations;
    private int _isInitialized;
    private int _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModelBase"/> class.
    /// </summary>
    /// <param name="errorHandler">The centralized error handler.</param>
    /// <param name="logger">The logger used for diagnostics.</param>
    /// <param name="messenger">The messenger used for cross-VM communication.</param>
    protected ViewModelBase(IErrorHandler errorHandler, ILogger logger, IMessenger messenger)
    {
        ArgumentNullException.ThrowIfNull(errorHandler);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(messenger);

        _errorHandler = errorHandler;
        Logger = logger.ForContext(GetType());
        Messenger = messenger;
    }

    /// <summary>
    /// Gets the messenger used for decoupled communication.
    /// </summary>
    protected IMessenger Messenger { get; }
    
    /// <summary>
    /// Gets the logger used for diagnostics.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Gets a value that indicates whether this instance was initialized successfully.
    /// </summary>
    public bool IsInitialized => Volatile.Read(ref _isInitialized) == 1;

    /// <summary>
    /// Registers a message handler for the current view model instance.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="handler">The message handler action.</param>
    protected void RegisterMessage<TMessage>(Action<TMessage> handler) where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(handler);
        ThrowIfDisposed();

        Messenger.Register<ViewModelBase, TMessage>(this, (_, message) => handler(message));
        Volatile.Write(ref _hasMessageRegistrations, 1);
    }

    /// <summary>
    /// Unregisters all messenger handlers for the current view model instance.
    /// </summary>
    protected void UnregisterAllMessages()
    {
        if (Interlocked.Exchange(ref _hasMessageRegistrations, 0) == 0)
        {
            return;
        }

        Messenger.UnregisterAll(this);
    }

    /// <summary>
    /// Initializes the view model.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel initialization.</param>
    /// <returns>A task representing initialization logic.</returns>
    public virtual Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Ensures <see cref="InitializeAsync"/> runs once per view model instance.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel initialization.</param>
    /// <returns>A task representing initialization coordination.</returns>
    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (Volatile.Read(ref _isInitialized) == 1)
        {
            return;
        }

        await _initializeSync.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Volatile.Read(ref _isInitialized) == 1)
            {
                return;
            }

            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            Volatile.Write(ref _isInitialized, 1);
        }
        finally
        {
            _initializeSync.Release();
        }
    }

    /// <summary>
    /// Disposes this view model instance and unregisters message handlers.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
        {
            return;
        }

        Dispose(disposing: true);
        UnregisterAllMessages();
        _initializeSync.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Called during disposal. Override to release subclass-specific resources.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    /// <summary>
    /// Executes async logic with centralized error handling and logging.
    /// </summary>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <param name="context">A short context used for diagnostics.</param>
    /// <param name="cancellationToken">A token that can cancel execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task ExecuteSafeAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string context = "")
    {
        try
        {
            await action(cancellationToken);
        }
        catch (Exception exception)
        {
            Logger.Error(exception, "ViewModel operation failed in {Context}.", context);
            await _errorHandler.HandleAsync(exception, context, cancellationToken);
        }
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _isDisposed) == 1)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
