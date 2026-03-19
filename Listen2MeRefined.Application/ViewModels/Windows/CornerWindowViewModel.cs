using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Core.Models;
using MediatR;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Windows;

public sealed partial class CornerWindowViewModel :
    ViewModelBase,
    INotificationHandler<CurrentSongNotification>,
    INotificationHandler<FontFamilyChangedNotification>
{
    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private AudioModel _song = new()
    {
        Artist = "Artist",
        Title = "Title",
        Genre = "Genre",
        Path = ""
    };

    private readonly IAppSettingsReader _settingsReader;
    private readonly IWindowPositionPolicyService _windowPositionPolicyService;

    public bool IsTopmost { get; set; }

    public CornerWindowViewModel(
        IAppSettingsReader settingsReader,
        IWindowPositionPolicyService windowPositionPolicyService,
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger) : base(errorHandler, logger, messenger)
    {
        _settingsReader = settingsReader;
        _windowPositionPolicyService = windowPositionPolicyService;

        Logger.Debug("[NewSongWindowViewModel] initialized");
    }

    public override Task InitializeAsync(CancellationToken ct = default)
    {
        RegisterMessage<CornerWindowPositionChangedMessage>(OnCornerWindowPositionChangedMessage);
        IsTopmost = _windowPositionPolicyService.IsTopmost(_settingsReader.GetNewSongWindowPosition());

        Logger.Debug("[NewSongWindowViewModel] Finished InitializeCoreAsync");
        
        return Task.CompletedTask;
    }

    private void OnCornerWindowPositionChangedMessage(CornerWindowPositionChangedMessage message)
    {
        Logger.Information(
            "[CornerWindowViewModel] Received CornerWindowPositionChangedMessage: {Position}",
            message.Value);
        IsTopmost = _windowPositionPolicyService.IsTopmost(message.Value);
        OnPropertyChanged(nameof(IsTopmost));
    }

    public Task Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
    {
        Logger.Information("[NewSongWindowViewModel] Received CurrentSongNotification: {@Audio}", notification.Audio);
        Song = notification.Audio;
        return Task.CompletedTask;
    }

    public Task Handle(FontFamilyChangedNotification notification, CancellationToken cancellationToken)
    {
        FontFamilyName = notification.FontFamily;
        return Task.CompletedTask;
    }
}
