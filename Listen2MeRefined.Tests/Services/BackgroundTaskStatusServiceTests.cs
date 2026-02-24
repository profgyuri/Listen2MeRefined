using Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Settings;
using Moq;

namespace Listen2MeRefined.Tests.Services;

public sealed class BackgroundTaskStatusServiceTests
{
    [Fact]
    public void GetSnapshot_MultiWorkerProgress_UsesWeightedPercent()
    {
        var settings = new AppSettings
        {
            ShowTaskPercentage = true,
            TaskPercentageReportInterval = 1
        };
        var sut = CreateSut(settings);

        var task = sut.StartTask("scan", "Scanning library", TaskProgressKind.Determinate);
        var workerA = sut.RegisterWorker(task, "A", 100);
        var workerB = sut.RegisterWorker(task, "B", 300);

        sut.ReportWorker(workerA, 50);
        sut.ReportWorker(workerB, 150);

        var snapshot = sut.GetSnapshot();
        var primary = Assert.IsType<BackgroundTaskItem>(snapshot.PrimaryTask);
        Assert.Equal(200, primary.ProcessedUnits);
        Assert.Equal(400, primary.TotalUnits);
        Assert.Equal(50, primary.Percent);
    }

    [Fact]
    public void GetSnapshot_PercentageIntervalApplied_RoundsDownToConfiguredStep()
    {
        var settings = new AppSettings
        {
            ShowTaskPercentage = true,
            TaskPercentageReportInterval = 5
        };
        var sut = CreateSut(settings);

        var task = sut.StartTask("scan", "Scanning", TaskProgressKind.Determinate);
        var worker = sut.RegisterWorker(task, "W", 100);

        sut.ReportWorker(worker, 6);
        Assert.Equal(5, sut.GetSnapshot().PrimaryTask?.Percent);

        sut.ReportWorker(worker, 3);
        Assert.Equal(5, sut.GetSnapshot().PrimaryTask?.Percent);

        sut.ReportWorker(worker, 1);
        Assert.Equal(10, sut.GetSnapshot().PrimaryTask?.Percent);
    }

    [Fact]
    public void GetSnapshot_ProcessedMilestoneCrossedByJump_UsesLatestBoundary()
    {
        var settings = new AppSettings
        {
            ShowScanMilestoneCount = true,
            ScanMilestoneBasis = TaskStatusCountBasis.Processed,
            ScanMilestoneInterval = 25
        };
        var sut = CreateSut(settings);

        var task = sut.StartTask("scan", "Scanning", TaskProgressKind.Determinate);
        var worker = sut.RegisterWorker(task, "W", 200);

        sut.ReportWorker(worker, 47);
        Assert.Equal("25 processed", sut.GetSnapshot().PrimaryTask?.CountText);

        sut.ReportWorker(worker, 36);
        Assert.Equal("75 processed", sut.GetSnapshot().PrimaryTask?.CountText);
    }

    [Fact]
    public void GetSnapshot_RemainingMilestoneCrossedByJump_UsesLatestBoundary()
    {
        var settings = new AppSettings
        {
            ShowScanMilestoneCount = true,
            ScanMilestoneBasis = TaskStatusCountBasis.Remaining,
            ScanMilestoneInterval = 25
        };
        var sut = CreateSut(settings);

        var task = sut.StartTask("scan", "Scanning", TaskProgressKind.Determinate);
        var worker = sut.RegisterWorker(task, "W", 200);

        sut.ReportWorker(worker, 20);
        sut.ReportWorker(worker, 87);

        Assert.Equal("100 remaining", sut.GetSnapshot().PrimaryTask?.CountText);
    }

    [Fact]
    public async Task CompleteTask_TerminalVisibilityExpires_HidesSnapshot()
    {
        var settings = new AppSettings();
        var sut = CreateSut(settings, TimeSpan.FromMilliseconds(30));

        var task = sut.StartTask("scan", "Scanning", TaskProgressKind.Determinate);
        var worker = sut.RegisterWorker(task, "W", 10);

        sut.ReportWorker(worker, 10);
        sut.CompleteWorker(worker);
        sut.CompleteTask(task);

        Assert.True(sut.GetSnapshot().IsVisible);
        Assert.Equal(BackgroundTaskState.Completed, sut.GetSnapshot().PrimaryTask?.State);

        await Task.Delay(90);

        Assert.False(sut.GetSnapshot().IsVisible);
    }

    [Fact]
    public void GetSnapshot_NoTasks_DoesNotReadSettings()
    {
        var readService = new Mock<IAppSettingsReader>(MockBehavior.Strict);
        var sut = new BackgroundTaskStatusService(readService.Object, Mock.Of<Serilog.ILogger>());

        var snapshot = sut.GetSnapshot();

        Assert.False(snapshot.IsVisible);
        Assert.Null(snapshot.PrimaryTask);
        Assert.Equal(0, snapshot.QueuedCount);
    }

    private static BackgroundTaskStatusService CreateSut(AppSettings settings, TimeSpan? terminalVisibility = null)
    {
        var settingsManager = new Mock<ISettingsManager<AppSettings>>();
        settingsManager.SetupGet(x => x.Settings).Returns(settings);

        var readService = new AppSettingsReader(settingsManager.Object);
        return new BackgroundTaskStatusService(readService, Mock.Of<Serilog.ILogger>(), terminalVisibility);
    }
}
