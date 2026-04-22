using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Popups;

public partial class ReplaceDefaultPlaylistPopupViewModel : PopupViewModelBase
{
    public ReplaceDefaultPlaylistPopupViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger) : base(errorHandler, logger, messenger)
    { }

    public override string DisplayTitle => "Replace default playlist?";

    public override string PrimaryButtonText => "Yes";

    public override string SecondaryButtonText => "Cancel";

    [ObservableProperty] private int _existingCount;
    [ObservableProperty] private int _importedCount;

    public string BodyText =>
        $"The default playlist currently contains {ExistingCount} track{(ExistingCount == 1 ? string.Empty : "s")}. " +
        $"Replace them with the {ImportedCount} imported track{(ImportedCount == 1 ? string.Empty : "s")}?";

    public void SetCounts(int existing, int imported)
    {
        ExistingCount = existing;
        ImportedCount = imported;
        OnPropertyChanged(nameof(BodyText));
    }
}
