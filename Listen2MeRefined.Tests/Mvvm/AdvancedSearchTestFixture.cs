using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Storage;
using MediatR;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Mvvm;

public class AdvancedSearchTestFixture
{
    public Mock<IMediator> MediatorMock { get; }
    public Mock<ILogger> LoggerMock { get; }
    public Mock<ISettingsManager<AppSettings>> SettingsManagerMock { get; }

    public AdvancedSearchTestFixture()
    {
        MediatorMock = new Mock<IMediator>();
        LoggerMock = new Mock<ILogger>();
        SettingsManagerMock = new Mock<ISettingsManager<AppSettings>>();
        
        // Settings setup
        SettingsManagerMock.Setup(x => x.Settings)
            .Returns(new AppSettings 
            { 
                FontFamily = "Test Font",
                MusicFolders = new List<MusicFolderModel>(),
                NewSongWindowPosition = "Default",
                ScanOnStartup = true
            });

        // Common mediator setup
        MediatorMock.Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Common logger setups
        LoggerMock.Setup(x => x.Information(It.IsAny<string>(), It.IsAny<object[]>()));
        LoggerMock.Setup(x => x.Error(It.IsAny<string>(), It.IsAny<object[]>()));
        LoggerMock.Setup(x => x.Debug(It.IsAny<string>(), It.IsAny<object[]>()));
    }
}