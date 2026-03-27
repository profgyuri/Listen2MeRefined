using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Threading;
using Listen2MeRefined.Application.Updating;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;
using Listen2MeRefined.Core.Enums;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Shells;

public sealed partial class MainShellViewModel : ShellViewModelBase
{
    private readonly IWindowManager _windowManager;
    private readonly IAppUpdateChecker _appUpdateChecker;
    private readonly IBackgroundTaskStatusService _backgroundTaskStatusService;
    private readonly IUiDispatcher _ui;
    
    [ObservableProperty] private bool _isUpdateAvailable;
    [ObservableProperty] private bool _isTaskStatusVisible;
    [ObservableProperty] private string _taskStatusText = string.Empty;
    [ObservableProperty] private string _taskStatusTooltip = string.Empty;
    [ObservableProperty] private string _fontFamilyName = string.Empty;
    
    public MainShellViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger, 
        IShellContextFactory context, 
        IWindowManager windowManager, 
        IAppUpdateChecker appUpdateChecker,
        IBackgroundTaskStatusService backgroundTaskStatusService,
        IUiDispatcher ui) : base(errorHandler, logger, messenger, context.Create())
    {
        _windowManager = windowManager;
        _appUpdateChecker = appUpdateChecker;
        _backgroundTaskStatusService = backgroundTaskStatusService;
        _ui = ui;

        _backgroundTaskStatusService.SnapshotChanged += BackgroundTaskStatusServiceOnSnapshotChanged;
        ApplyTaskSnapshot(_backgroundTaskStatusService.GetSnapshot());
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);
        
        IsUpdateAvailable = (await _appUpdateChecker.CheckForUpdatesAsync()).IsUpdateAvailable;
        
        await NavigationService
            .NavigateAsync<MainShellDefaultHomeViewModel>(cancellationToken: cancellationToken)
            .ConfigureAwait(true);
        
        await base.InitializeAsync(cancellationToken);
        
        Logger.Debug("[MainShellViewModel] Finished InitializeAsync");
    }

    private void BackgroundTaskStatusServiceOnSnapshotChanged(object? sender, BackgroundTaskSnapshot snapshot)
    {
        _ = _ui.InvokeAsync(() => ApplyTaskSnapshot(snapshot));
    }

    private void ApplyTaskSnapshot(BackgroundTaskSnapshot snapshot)
    {
        IsTaskStatusVisible = snapshot.IsVisible && snapshot.PrimaryTask is not null;
        if (!IsTaskStatusVisible)
        {
            TaskStatusText = string.Empty;
            TaskStatusTooltip = string.Empty;
            return;
        }

        TaskStatusText = FormatTaskStatusText(snapshot.PrimaryTask!, snapshot.QueuedCount);
        TaskStatusTooltip = FormatTaskStatusTooltip(snapshot.PrimaryTask!, TaskStatusText);
    }

    private static string FormatTaskStatusText(BackgroundTaskItem task, int queuedCount)
    {
        var parts = new List<string>();

        if (task.State == BackgroundTaskState.Completed)
        {
            parts.Add("Done");
        }
        else if (task.State == BackgroundTaskState.Failed)
        {
            parts.Add("Error");
        }

        parts.Add(task.DisplayName);

        if (task.Percent is not null)
        {
            parts.Add($"{task.Percent}%");
        }

        if (queuedCount > 0)
        {
            parts.Add($"+{queuedCount}");
        }

        return string.Join(" | ", parts);
    }

    private static string FormatTaskStatusTooltip(BackgroundTaskItem task, string fallbackText)
    {
        return string.IsNullOrWhiteSpace(task.Message)
            ? fallbackText
            : task.Message!;
    }

    [RelayCommand]
    private async Task OpenSettingsWindow()
    {
        await ExecuteSafeAsync(async ct =>
        {
            await _ui.InvokeAsync(OpenSettingsOnUiAsync, ct);
            return;

            async Task OpenSettingsOnUiAsync()
            {
                await _windowManager.ShowWindowAsync<SettingsShellViewModel>(
                    WindowShowOptions.CenteredOnMainWindow(),
                    ct);
            }
        });
    }
    
    private void OnFontFamilyChangedMessage(FontFamilyChangedMessage message)
    {
        Logger.Debug("[MainShellViewModel] Received FontFamily changed message: {message}", message.Value);
        FontFamilyName = message.Value;
    }
}
