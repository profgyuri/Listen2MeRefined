namespace Listen2MeRefined.Application.ViewModels.ContextMenus;

public sealed class SongContextMenuItemViewModel(
    int playlistId,
    string playlistName,
    bool isChecked,
    bool allowRemove)
{
    public int PlaylistId { get; } = playlistId;
    public string PlaylistName { get; } = playlistName;
    public bool IsChecked { get; set; } = isChecked;
    public bool AllowRemove { get; } = allowRemove;
}