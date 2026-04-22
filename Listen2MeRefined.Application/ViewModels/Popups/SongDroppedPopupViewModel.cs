using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Popups;

public partial class SongDroppedPopupViewModel : PopupViewModelBase
{
    public SongDroppedPopupViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger) : base(errorHandler, logger, messenger)
    { }

    public override string DisplayTitle => "Handle new folder";

    [ObservableProperty] private string _folderPath = string.Empty;
    [ObservableProperty] private bool _dontAskAgain;

    public void SetFolderPath(string folderPath)
    {
        FolderPath = folderPath;
    }
}
