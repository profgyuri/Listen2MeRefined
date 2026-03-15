using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Application.ViewModels.Shells;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Widgets;

public partial class SearchbarViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly IAudioSearchExecutionService _audioSearchExecutionService;
    private readonly IWindowManager _windowManager;

    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private string _searchTerm = "";
    
    public SearchbarViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IAudioSearchExecutionService audioSearchExecutionService, 
        IWindowManager windowManager) : base(errorHandler, logger, messenger)
    {
        _logger = logger;
        _audioSearchExecutionService = audioSearchExecutionService;
        _windowManager = windowManager;
        
        _logger.Debug("[SearchbarViewModel] initialized");
    }

    public override Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);
        
        Logger.Debug("[SearchbarViewModel] Finished InitializeCoreAsync");
        return base.InitializeAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task QuickSearch()
    {
        await ExecuteSafeAsync(async ct =>
        {
            _logger.Information("[SearchbarViewModel] Searching for \'{SearchTerm}\'", SearchTerm);
            var result = (await _audioSearchExecutionService.ExecuteQuickSearchAsync(SearchTerm)).ToArray();
            _logger.Information("[SearchbarViewModel] Found {ResultCount} results", result.Length);
            if (result.Length > 0)
            {
                _logger.Verbose(
                    "[SearchbarViewModel] First {Shown} results are: {@Results}",
                    Math.Min(5, result.Length),
                    result.Take(5));
            }

            Messenger.Send(new QuickSearchExecutedMessage(result));
        });
    }

    [RelayCommand]
    private async Task OpenAdvancedSearchWindow()
    {
        await ExecuteSafeAsync(async ct =>
        {
            Logger.Information("[SearchbarViewModel] Opening advanced search window");
            await _windowManager.ShowWindowAsync<AdvancedSearchShellViewModel>(WindowShowOptions.CenteredOnMainWindow(), ct);
        });
    }
    
    private void OnFontFamilyChangedMessage(FontFamilyChangedMessage message)
    {
        Logger.Debug("[SearchbarViewModel] Received FontFamilyChangedMessage: {message}", message.Value);
        FontFamilyName = message.Value;
    }
}
