using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Widgets;

public partial class TrackInfoViewModel : ViewModelBase
{
    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private PlayerState _playerState = PlayerState.Stopped;
    [ObservableProperty] private AudioModel _song = new()
    {
        Artist = "Artist",
        Title = "Title",
        Genre = "Genre",
        Path = ""
    };
    
    public TrackInfoViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger) : base(errorHandler, logger, messenger)
    {
    }

    public override Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        RegisterMessage<CurrentSongChangedMessage>(OnCurrentSongChangedMessage);
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);
        RegisterMessage<PlayerStateChangedMessage>(OnPlayerStateChangedMessage);
        
        return base.InitializeAsync(cancellationToken);
    }

    private void OnCurrentSongChangedMessage(CurrentSongChangedMessage message)
    {
        Logger.Debug("[TrackInfoViewModel] Received CurrentSongChangedMessage: {@Audio}", message.Value);
        Song = message.Value;
    }

    private void OnFontFamilyChangedMessage(FontFamilyChangedMessage message)
    {
        Logger.Debug("[TrackInfoViewModel] Received FontFamilyChangedMessage: {message}", message.Value);
        FontFamilyName = message.Value;
    }
    
    private void OnPlayerStateChangedMessage(PlayerStateChangedMessage message)
    {
        Logger.Debug("[TrackInfoViewModel] Received PlayerStateChangedMessage: {state}", message.Value);
        PlayerState = message.Value;
    }
}