namespace Listen2MeRefined.Infrastructure.Mvvm;

using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;

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
    }

    [RelayCommand]
    private async Task QuickSearch()
    {
        _logger.Information("Searching for \'{SearchTerm}\'", SearchTerm);
        var results = string.IsNullOrEmpty(SearchTerm)
                ? await _audioRepository.ReadAsync()
                : await _audioRepository.ReadAsync(SearchTerm);

        if (results is not null)
        {
            await _mediator.Publish(new QuickSearchResultsNotification(results));
        }
    }

    public async Task Handle(FontFamilyChangedNotification notification, CancellationToken cancellationToken)
    {
        FontFamily = notification.FontFamily;
        await Task.CompletedTask;
    }
}