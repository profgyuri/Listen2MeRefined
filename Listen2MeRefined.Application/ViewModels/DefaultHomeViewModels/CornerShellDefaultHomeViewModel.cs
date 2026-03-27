using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;

public partial class CornerShellDefaultHomeViewModel : ViewModelBase
{
    private readonly IAppSettingsReader _settingsReader;
    private readonly IWindowPositionPolicyService _windowPositionPolicyService;

    public TrackInfoViewModel TrackInfoViewModel { get; }

    [ObservableProperty]
    private string _fontFamilyName = string.Empty;

    [ObservableProperty]
    private bool _isTopmost;

    public CornerShellDefaultHomeViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger,
        TrackInfoViewModel trackInfoViewModel,
        IAppSettingsReader settingsReader,
        IWindowPositionPolicyService windowPositionPolicyService) : base(errorHandler, logger, messenger)
    {
        TrackInfoViewModel = trackInfoViewModel;
        _settingsReader = settingsReader;
        _windowPositionPolicyService = windowPositionPolicyService;
    }

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        await ExecuteSafeAsync(
            async token =>
            {
                RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);
                RegisterMessage<CornerWindowPositionChangedMessage>(OnCornerWindowPositionChangedMessage);

                FontFamilyName = _settingsReader.GetFontFamily();
                IsTopmost = _windowPositionPolicyService.IsTopmost(_settingsReader.GetNewSongWindowPosition());

                await TrackInfoViewModel.EnsureInitializedAsync(token);
            },
            cancellationToken: ct);
    }

    private void OnFontFamilyChangedMessage(FontFamilyChangedMessage message)
    {
        FontFamilyName = message.Value;
    }

    private void OnCornerWindowPositionChangedMessage(CornerWindowPositionChangedMessage message)
    {
        IsTopmost = _windowPositionPolicyService.IsTopmost(message.Value);
    }
}
