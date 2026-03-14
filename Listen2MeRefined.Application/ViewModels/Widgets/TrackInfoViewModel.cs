using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Widgets;

public class TrackInfoViewModel : ViewModelBase
{
    public TrackInfoViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger) : base(errorHandler, logger, messenger)
    {
    }
}