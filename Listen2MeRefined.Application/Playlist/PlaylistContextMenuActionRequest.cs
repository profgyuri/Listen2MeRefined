using Listen2MeRefined.Application.ViewModels;
using Listen2MeRefined.Core.Enums;

namespace Listen2MeRefined.Application.Playlist;

public sealed record PlaylistContextMenuActionRequest(
    ViewModelBase SourceViewModel,
    PlaylistContextMenuAction Action);