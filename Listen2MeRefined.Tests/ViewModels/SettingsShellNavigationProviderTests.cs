using Listen2MeRefined.Application.ViewModels.Shells;

namespace Listen2MeRefined.Tests.ViewModels;

public sealed class SettingsShellNavigationProviderTests
{
    [Fact]
    public void CreateNavigationItems_State_ReturnsLegacyOrderAndMetadata()
    {
        var provider = new SettingsShellNavigationProvider();

        var items = provider.CreateNavigationItems();

        Assert.Collection(
            items,
            item => AssertItem(item, "settings/general", "General", "Tune"),
            item => AssertItem(item, "settings/playback", "Playback", "VolumeHigh"),
            item => AssertItem(item, "settings/library", "Library", "FolderMusicOutline"),
            item => AssertItem(item, "settings/playlists", "Playlists", "PlaylistMusic"),
            item => AssertItem(item, "settings/hooksAndAlerts", "Hooks & Alerts", "BellOutline"),
            item => AssertItem(item, "settings/advanced", "Advanced", "AlertCircleOutline"));
    }

    [Fact]
    public void CreateNavigationItems_State_ReturnsNewItemInstancesEachCall()
    {
        var provider = new SettingsShellNavigationProvider();

        var first = provider.CreateNavigationItems();
        var second = provider.CreateNavigationItems();

        Assert.Equal(first.Count, second.Count);
        Assert.All(
            Enumerable.Range(0, first.Count),
            index => Assert.NotSame(first[index], second[index]));
    }

    private static void AssertItem(
        SettingsShellNavigationItem item,
        string route,
        string label,
        string iconKind)
    {
        Assert.Equal(route, item.Route);
        Assert.Equal(label, item.Label);
        Assert.Equal(iconKind, item.IconKind);
    }
}
