using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;

namespace Listen2MeRefined.Infrastructure.Mvvm.MainWindow;

public partial class SearchbarViewModel : 
    ViewModelBase,
    INotificationHandler<FontFamilyChangedNotification>
{
    private readonly ILogger _logger;
    private readonly IRepository<AudioModel> _audioRepository;
    private readonly IMediator _mediator;

    [ObservableProperty] private string _fontFamily = "";
    [ObservableProperty] private string _searchTerm = "";

    public SearchbarViewModel(
        ILogger logger,
        IRepository<AudioModel> audioRepository,
        IMediator mediator)
    {
        _logger = logger;
        _audioRepository = audioRepository;
        _mediator = mediator;

        _logger.Debug("[SearchbarViewModel] initialized");
    }

    [RelayCommand]
    private async Task QuickSearch()
    {
        _logger.Information<string>("[SearchbarViewModel] Searching for \'{SearchTerm}\'", SearchTerm);
        var results = string.IsNullOrEmpty(SearchTerm)
                ? await _audioRepository.ReadAsync()
                : await _audioRepository.ReadAsync(SearchTerm);

        if (results is null)
        {
            return;
        }

        var result = results.ToArray();
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