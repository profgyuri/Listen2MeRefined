using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Searching;
using Listen2MeRefined.Infrastructure.Services.Contracts;

namespace Listen2MeRefined.Infrastructure.Mvvm.MainWindow;

public partial class SearchbarViewModel :
    ViewModelBase,
    INotificationHandler<FontFamilyChangedNotification>
{
    private readonly ILogger _logger;
    private readonly IAudioSearchExecutionService _audioSearchExecutionService;
    private readonly IMediator _mediator;

    [ObservableProperty] private string _fontFamily = "";
    [ObservableProperty] private string _searchTerm = "";

    public SearchbarViewModel(
        ILogger logger,
        IAudioSearchExecutionService audioSearchExecutionService,
        IMediator mediator)
    {
        _logger = logger;
        _audioSearchExecutionService = audioSearchExecutionService;
        _mediator = mediator;

        _logger.Debug("[SearchbarViewModel] initialized");
    }

    [RelayCommand]
    private async Task QuickSearch()
    {
        _logger.Information<string>("[SearchbarViewModel] Searching for \'{SearchTerm}\'", SearchTerm);
        var result = (await _audioSearchExecutionService.ExecuteQuickSearchAsync(SearchTerm)).ToArray();
        _logger.Information("[SearchbarViewModel] Found {ResultCount} results", result.Length);
        if (result.Length > 0)
        {
            _logger.Verbose(
                "[SearchbarViewModel] First {Shown} results are: {@Results}",
                Math.Min(5, result.Length),
                result.Take(5));
        }

        await _mediator.Publish(new QuickSearchResultsNotification(result));
    }

    public Task Handle(FontFamilyChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[SearchbarViewModel] Received FontFamilyChangedNotification: {FontFamily}", notification.FontFamily);
        FontFamily = notification.FontFamily;
        return Task.CompletedTask;
    }
}
