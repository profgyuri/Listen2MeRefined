using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Threading;
using Listen2MeRefined.Application.Updating;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.Core.Enums;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels;

public sealed class MainShellViewModelTests
{
    [Fact]
    public void Constructor_VisibleSnapshot_FormatsStatusTextAndTooltip()
    {
        var snapshot = CreateSnapshot(
            state: BackgroundTaskState.Running,
            displayName: "Scanning library",
            percent: 35,
            message: null,
            queuedCount: 1);

        var (viewModel, _, _, _, _) = CreateViewModel(snapshot);

        Assert.True(viewModel.IsTaskStatusVisible);
        Assert.Equal("Scanning library | 35% | +1", viewModel.TaskStatusText);
        Assert.Equal("Scanning library | 35% | +1", viewModel.TaskStatusTooltip);
    }

    [Fact]
    public void SnapshotChanged_State_UpdatesStatusTextAndTooltip()
    {
        var (viewModel, taskStatusService, _, _, _) = CreateViewModel(CreateHiddenSnapshot());

        var snapshot = CreateSnapshot(
            state: BackgroundTaskState.Failed,
            displayName: "Scanning library",
            percent: null,
            message: "Access denied",
            queuedCount: 0);

        taskStatusService.Raise(x => x.SnapshotChanged += null, taskStatusService.Object, snapshot);

        Assert.True(viewModel.IsTaskStatusVisible);
        Assert.Equal("Error | Scanning library", viewModel.TaskStatusText);
        Assert.Equal("Access denied", viewModel.TaskStatusTooltip);
    }

    [Fact]
    public void SnapshotChanged_HiddenSnapshot_ClearsStatusFields()
    {
        var initial = CreateSnapshot(
            state: BackgroundTaskState.Completed,
            displayName: "Library scan",
            percent: 100,
            message: null,
            queuedCount: 0);

        var (viewModel, taskStatusService, _, _, _) = CreateViewModel(initial);

        taskStatusService.Raise(x => x.SnapshotChanged += null, taskStatusService.Object, CreateHiddenSnapshot());

        Assert.False(viewModel.IsTaskStatusVisible);
        Assert.Equal(string.Empty, viewModel.TaskStatusText);
        Assert.Equal(string.Empty, viewModel.TaskStatusTooltip);
    }

    private static (
        MainShellViewModel ViewModel,
        Mock<IBackgroundTaskStatusService> TaskStatusService,
        Mock<INavigationService> NavigationService,
        Mock<IAppUpdateChecker> AppUpdateChecker,
        Mock<IUiDispatcher> UiDispatcher) CreateViewModel(BackgroundTaskSnapshot snapshot)
    {
        var navigationService = new Mock<INavigationService>();
        var navigationState = new NavigationState();
        var shellContext = new ShellContext(
            navigationState,
            navigationService.Object,
            Mock.Of<IInitializationTracker>());
        var shellContextFactory = new Mock<IShellContextFactory>();
        shellContextFactory
            .Setup(x => x.Create())
            .Returns(shellContext);

        var appUpdateChecker = new Mock<IAppUpdateChecker>();
        appUpdateChecker
            .Setup(x => x.CheckForUpdatesAsync())
            .ReturnsAsync(new AppUpdateCheckResult(false, "No updates.", false));

        var taskStatusService = new Mock<IBackgroundTaskStatusService>();
        taskStatusService
            .Setup(x => x.GetSnapshot())
            .Returns(snapshot);

        var ui = new Mock<IUiDispatcher>();
        ui.Setup(x => x.InvokeAsync(It.IsAny<Action>(), It.IsAny<CancellationToken>()))
            .Returns<Action, CancellationToken>((action, _) =>
            {
                action();
                return Task.CompletedTask;
            });
        ui.Setup(x => x.InvokeAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((func, _) => func());
        ui.Setup(x => x.InvokeAsync(It.IsAny<Func<bool>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<bool>, CancellationToken>((func, _) => Task.FromResult(func()));

        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);

        var viewModel = new MainShellViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            Mock.Of<IMessenger>(),
            shellContextFactory.Object,
            Mock.Of<IWindowManager>(),
            appUpdateChecker.Object,
            taskStatusService.Object,
            ui.Object);

        return (viewModel, taskStatusService, navigationService, appUpdateChecker, ui);
    }

    private static BackgroundTaskSnapshot CreateHiddenSnapshot() => new(false, null, 0);

    private static BackgroundTaskSnapshot CreateSnapshot(
        BackgroundTaskState state,
        string displayName,
        int? percent,
        string? message,
        int queuedCount)
    {
        var task = new BackgroundTaskItem(
            "scan-library",
            displayName,
            state,
            percent is not null,
            7,
            10,
            percent,
            null,
            message,
            DateTimeOffset.UtcNow);

        return new BackgroundTaskSnapshot(true, task, queuedCount);
    }
}
