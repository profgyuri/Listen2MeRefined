using CommunityToolkit.Mvvm.ComponentModel;

namespace Listen2MeRefined.Application.ViewModels.Shells;

public sealed partial class SettingsShellNavigationItem : ObservableObject
{
    public SettingsShellNavigationItem(string route, string label, string iconKind)
    {
        Route = route;
        Label = label;
        IconKind = iconKind;
    }

    public string Route { get; }

    public string Label { get; }

    public string IconKind { get; }

    [ObservableProperty]
    private bool _isActive;
}
