using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Shells;

public partial class AdvancedSearchShellViewModel : ShellViewModelBase
{
    [ObservableProperty] private string _fontFamilyName = string.Empty;

    public AdvancedSearchShellViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IShellContextFactory context) : base(errorHandler, logger, messenger, context.Create())
    {
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);

        await NavigationService.NavigateAsync<AdvancedSearchShellDefaultHomeViewModel>(cancellationToken).ConfigureAwait(true);

        await base.InitializeAsync(cancellationToken);
    }

    private void OnFontFamilyChangedMessage(FontFamilyChangedMessage message)
    {
        Logger.Debug("[AdvancedSearchShellViewModel] Received FontFamilyChangedMessage: {value}", message.Value);
        FontFamilyName = message.Value;
    }
}
