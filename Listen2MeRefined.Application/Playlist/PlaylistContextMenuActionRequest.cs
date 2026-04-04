using Listen2MeRefined.Application.ViewModels.ContextMenus;
using Listen2MeRefined.Core.Enums;

namespace Listen2MeRefined.Application.Playlist;

public sealed record PlaylistContextMenuActionRequest(
    ISongContextMenuHost SourceHost,
    PlaylistContextMenuAction Action);