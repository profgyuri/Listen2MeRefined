using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Core.DomainObjects;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.SettingsTabs;

public partial class SettingsPlaybackTabViewModel : ViewModelBase
{
    private const int MinStartupVolumePercent = 0;
    private const int MaxStartupVolumePercent = 100;

    private readonly IOutputDevice _outputDevice;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IAppSettingsWriter _settingsWriter;
    private readonly IPlaybackDefaultsService _playbackDefaultsService;
    private bool _isLoadingSettings;

    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private AudioOutputDevice? _selectedAudioOutputDevice;
    [ObservableProperty] private ObservableCollection<AudioOutputDevice> _audioOutputDevices = [];
    [ObservableProperty] private int _startupVolumePercent = 70;
    [ObservableProperty] private bool _startMuted;

    public SettingsPlaybackTabViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IOutputDevice outputDevice,
        IAppSettingsReader settingsReader,
        IAppSettingsWriter settingsWriter,
        IPlaybackDefaultsService playbackDefaultsService) : base(errorHandler, logger, messenger)
    {
        _outputDevice = outputDevice;
        _settingsReader = settingsReader;
        _settingsWriter = settingsWriter;
        _playbackDefaultsService = playbackDefaultsService;
    }

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);

        _isLoadingSettings = true;
        try
        {
            StartupVolumePercent = _playbackDefaultsService.ToVolumePercent(_settingsReader.GetStartupVolume());
            StartMuted = _settingsReader.GetStartMuted();
            FontFamilyName = _settingsReader.GetFontFamily();
            await LoadAudioOutputDevicesAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[SettingsPlaybackTabViewModel] Failed to initialize");
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    partial void OnSelectedAudioOutputDeviceChanged(AudioOutputDevice? value)
    {
        if (_isLoadingSettings || value is null)
        {
            return;
        }

        Logger.Information("[SettingsPlaybackTabViewModel] Audio output device changed to: {DeviceName}", value.Name);
        _settingsWriter.SetAudioOutputDeviceName(value.Name);
        Messenger.Send(new AudioOutputDeviceChangedMessage(value));
    }

    partial void OnStartupVolumePercentChanged(int value)
    {
        var clamped = Math.Clamp(value, MinStartupVolumePercent, MaxStartupVolumePercent);
        if (clamped != value)
        {
            StartupVolumePercent = clamped;
            return;
        }

        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetStartupVolume(_playbackDefaultsService.FromVolumePercent(clamped));
        if (clamped > 0 && StartMuted)
        {
            StartMuted = false;
        }
    }

    partial void OnStartMutedChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetStartMuted(value);
    }

    private async Task LoadAudioOutputDevicesAsync()
    {
        AudioOutputDevices.Clear();

        var devices = await Task.Run(() =>
        {
            var result = Enumerable.Empty<AudioOutputDevice>();
            try
            {
                result = _outputDevice.EnumerateOutputDevices();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[SettingsPlaybackTabViewModel] Could not enumerate output devices");
            }

            return result;
        });

        foreach (var device in devices)
        {
            AudioOutputDevices.Add(device);
        }

        if (AudioOutputDevices.Count == 0)
        {
            return;
        }

        var selectedIndex = 0;
        var savedName = _settingsReader.GetAudioOutputDeviceName();
        if (!string.IsNullOrWhiteSpace(savedName))
        {
            var selectedDevice = AudioOutputDevices
                .FirstOrDefault(x => x.Name.Equals(savedName, StringComparison.OrdinalIgnoreCase));
            if (selectedDevice is not null)
            {
                selectedIndex = AudioOutputDevices.IndexOf(selectedDevice);
            }
        }

        SelectedAudioOutputDevice = AudioOutputDevices[selectedIndex];
    }

    private void OnFontFamilyChangedMessage(FontFamilyChangedMessage message)
    {
        FontFamilyName = message.Value;
        Logger.Debug("[SettingsPlaybackTabViewModel] Received FontFamilyChangedMessage: {message}", message.Value);
    }
}
