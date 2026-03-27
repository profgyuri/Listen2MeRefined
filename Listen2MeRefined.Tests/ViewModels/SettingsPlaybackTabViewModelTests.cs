using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.ViewModels.SettingsTabs;
using Listen2MeRefined.Core.DomainObjects;
using Listen2MeRefined.Infrastructure.Settings;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels;

public sealed class SettingsPlaybackTabViewModelTests
{
    [Fact]
    public async Task InitializeAsync_State_LoadsPersistedPlaybackSettings()
    {
        var settings = new AppSettings
        {
            StartupVolume = 0.42f,
            StartMuted = true,
            AudioOutputDeviceName = "Speakers"
        };
        var devices = new[]
        {
            new AudioOutputDevice(1, "Headphones"),
            new AudioOutputDevice(2, "Speakers")
        };

        var (viewModel, _, _) = CreateViewModel(settings, devices);
        await viewModel.InitializeAsync();

        Assert.Equal(42, viewModel.StartupVolumePercent);
        Assert.True(viewModel.StartMuted);
        Assert.Equal("Speakers", viewModel.SelectedAudioOutputDevice?.Name);
    }

    [Fact]
    public async Task SelectedAudioOutputDevice_State_PersistsAndPublishesMessage()
    {
        var settings = new AppSettings
        {
            AudioOutputDeviceName = "Headphones"
        };
        var devices = new[]
        {
            new AudioOutputDevice(1, "Headphones"),
            new AudioOutputDevice(2, "Speakers")
        };

        var (viewModel, _, probe) = CreateViewModel(settings, devices);
        await viewModel.InitializeAsync();

        viewModel.SelectedAudioOutputDevice = devices[1];

        Assert.Equal("Speakers", settings.AudioOutputDeviceName);
        Assert.Equal(2, probe.AudioOutputDevice?.Index);
    }

    [Fact]
    public async Task StartupVolumePercent_WhenRaisedAboveZero_ClearsMutedAndPersists()
    {
        var settings = new AppSettings
        {
            StartupVolume = 0f,
            StartMuted = true
        };

        var (viewModel, _, _) = CreateViewModel(settings, []);
        await viewModel.InitializeAsync();

        viewModel.StartupVolumePercent = 25;

        Assert.Equal(0.25f, settings.StartupVolume);
        Assert.False(viewModel.StartMuted);
        Assert.False(settings.StartMuted);
    }

    [Fact]
    public async Task StartupVolumePercent_State_ClampsToMaximum()
    {
        var settings = new AppSettings
        {
            StartupVolume = 0.2f,
            StartMuted = false
        };

        var (viewModel, _, _) = CreateViewModel(settings, []);
        await viewModel.InitializeAsync();

        viewModel.StartupVolumePercent = 150;

        Assert.Equal(100, viewModel.StartupVolumePercent);
        Assert.Equal(1f, settings.StartupVolume);
    }

    private static (SettingsPlaybackTabViewModel ViewModel, AppSettings Settings, MessageProbe Probe) CreateViewModel(
        AppSettings settings,
        IReadOnlyList<AudioOutputDevice> devices)
    {
        var settingsManager = new Mock<ISettingsManager<AppSettings>>();
        settingsManager.SetupGet(x => x.Settings).Returns(settings);
        settingsManager
            .Setup(x => x.SaveSettings(It.IsAny<Action<AppSettings>>()))
            .Callback<Action<AppSettings>>(apply => apply(settings));

        var outputDevice = new Mock<IOutputDevice>();
        outputDevice.Setup(x => x.EnumerateOutputDevices()).Returns(devices);

        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);

        var messenger = new WeakReferenceMessenger();
        var probe = new MessageProbe();
        messenger.Register<MessageProbe, AudioOutputDeviceChangedMessage>(
            probe,
            static (recipient, message) => recipient.AudioOutputDevice = message.Value);

        var viewModel = new SettingsPlaybackTabViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            outputDevice.Object,
            new AppSettingsReader(settingsManager.Object),
            new AppSettingsWriter(settingsManager.Object),
            new PlaybackDefaultsService(settingsManager.Object));

        return (viewModel, settings, probe);
    }

    private sealed class MessageProbe
    {
        public AudioOutputDevice? AudioOutputDevice { get; set; }
    }
}
