using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.ViewModels.Shells;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Widgets;

public partial class SearchbarViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly IAudioSearchExecutionService _audioSearchExecutionService;
    private readonly IWindowManager _windowManager;
    private readonly IAppSettingsReader _settingsReader;

    private CancellationTokenSource? _debounceCts;
    private int _debounceMs;

    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private string _searchTerm = "";

    public SearchbarViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IAudioSearchExecutionService audioSearchExecutionService,
        IAppSettingsReader settingsReader,
        IWindowManager windowManager) : base(errorHandler, logger, messenger)
    {
        _logger = logger;
        _audioSearchExecutionService = audioSearchExecutionService;
        _settingsReader = settingsReader;
        _windowManager = windowManager;

        _logger.Debug("[SearchbarViewModel] initialized");
    }

    public override Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);
        RegisterMessage<SearchDebounceChangedMessage>(OnSearchDebounceChangedMessage);

        FontFamilyName = _settingsReader.GetFontFamily();
        _debounceMs = _settingsReader.GetSearchDebounceMs();

        Logger.Debug("[SearchbarViewModel] Finished InitializeCoreAsync");
        return base.InitializeAsync(cancellationToken);
    }

    partial void OnSearchTermChanged(string value)
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = DebouncedSearchAsync(value, token);
    }

    private async Task DebouncedSearchAsync(string searchTerm, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_debounceMs, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        await ExecuteSafeAsync(async ct =>
        {
            _logger.Information("[SearchbarViewModel] Searching for \'{SearchTerm}\'", searchTerm);
            var result = (await _audioSearchExecutionService.ExecuteQuickSearchAsync(searchTerm)).ToArray();
            _logger.Information("[SearchbarViewModel] Found {ResultCount} results", result.Length);
            if (result.Length > 0)
            {
                _logger.Verbose(
                    "[SearchbarViewModel] First {Shown} results are: {@Results}",
                    Math.Min(5, result.Length),
                    result.Take(5));
            }

            Messenger.Send(new QuickSearchExecutedMessage(result));
        }, cancellationToken);
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

    private void OnSearchDebounceChangedMessage(SearchDebounceChangedMessage message)
    {
        Logger.Debug("[SearchbarViewModel] Received SearchDebounceChangedMessage: {value}", message.Value);
        _debounceMs = message.Value;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
        }

        base.Dispose(disposing);
    }
}
