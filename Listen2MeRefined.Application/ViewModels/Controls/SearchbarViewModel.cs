using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Searching;
using MediatR;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Controls;

public partial class SearchbarViewModel :
    ViewModelBase,
    INotificationHandler<FontFamilyChangedNotification>
{
    private readonly ILogger _logger;
    private readonly IAudioSearchExecutionService _audioSearchExecutionService;
    private readonly IMediator _mediator;

    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private string _searchTerm = "";
    
    public SearchbarViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IAudioSearchExecutionService audioSearchExecutionService,
        IMediator mediator) : base(errorHandler, logger, messenger)
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
        FontFamilyName = notification.FontFamily;
        return Task.CompletedTask;
    }
}
