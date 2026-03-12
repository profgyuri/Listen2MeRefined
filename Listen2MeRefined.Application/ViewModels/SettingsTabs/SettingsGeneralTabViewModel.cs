using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.SettingsTabs;

public class SettingsGeneralTabViewModel : ViewModelBase
{
    public SettingsGeneralTabViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger) : base(errorHandler, logger, messenger)
    {
    }
}