using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Playlist.Formats;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.ContextMenus;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Core.Repositories;
using Listen2MeRefined.Infrastructure.Data.Repositories;
using Listen2MeRefined.Infrastructure.Playlist;
using Listen2MeRefined.Infrastructure.Playlist.Formats;
using Listen2MeRefined.Infrastructure.Startup;
using Listen2MeRefined.WPF.Utils;
using Listen2MeRefined.WPF.Views.Widgets;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Modules;

public sealed class PlaylistModule : IModule
{
    public string Name { get; } = "Playlist";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<IRepository<PlaylistModel>, PlaylistRepository>();
        services.AddTransient<IRepository<MusicFolderModel>, MusicFolderRepository>();

        services.AddTransient<IPlaylistLibraryService, PlaylistLibraryService>();
        services.AddTransient<IPlaylistSelectionService, PlaylistSelectionService>();
        services.AddTransient<IPlaylistMembership, PlaylistMembership>();
        services.AddTransient<ISongContextSelectionService, SongContextSelectionService>();
        services.AddTransient<IObservableCollectionUpdater, ObservableCollectionUpdater>();
        services.AddTransient<IPlaylistSortService, PlaylistSortService>();
        services.AddSingleton<IDefaultPlaylistService, DefaultPlaylistService>();

        // Playlist file formats (import / export)
        services.AddSingleton<IPlaylistFileFormat, M3u8PlaylistFormat>();
        services.AddSingleton<IPlaylistFileFormat, PlsPlaylistFormat>();
        services.AddSingleton<IPlaylistFileFormat, JsonPlaylistFormat>();
        services.AddSingleton<IPlaylistFormatRegistry, PlaylistFormatRegistry>();

        services.AddSingleton<IPlaylistImportService, PlaylistImportService>();
        services.AddSingleton<IPlaylistExportService, PlaylistExportService>();
        services.AddSingleton<IReplaceDefaultPlaylistPrompt, ReplaceDefaultPlaylistPromptService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();

        services.AddTransient<IDroppedSongFolderPromptService, DroppedSongFolderPromptService>();
        services.AddSingleton<IExternalDropImportService, ExternalDropImportService>();
        services.AddSingleton<IExternalAudioOpenService, ExternalAudioOpenService>();
        services.AddSingleton<IExternalAudioOpenInbox, ExternalAudioOpenInbox>();

        services.AddTransient<PlaylistSidebarViewModel>();
        services.AddTransient<PlaylistPaneViewModel>();
        services.AddTransient<PlaylistPaneView>();
        services.AddTransient<SongContextMenuViewModel>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
    }
}
