using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Application.ViewModels.Windows;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Listen2MeRefined.WPF.Dependency;

public static class ShellsModule
{
    internal static IHostBuilder ConfigureShells(this IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddTransient<MainShellViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            
            services.AddSingleton<AdvancedSearchShellViewModel>();
            services.AddSingleton<AdvancedSearchViewModel>();
            
            services.AddSingleton<CornerWindowShellViewModel>();
            services.AddSingleton<CornerWindowViewModel>();
            
            services.AddSingleton<SettingsShellViewModel>();
            services.AddSingleton<SettingsWindowViewModel>();
            
            services.AddTransient<FolderBrowserShellViewModel>();
            services.AddTransient<FolderBrowserViewModel>();
            
            services.AddSingleton<IWaveformViewportAware>(sp => sp.GetRequiredService<PlaybackControlsViewModel>());
            services.AddSingleton<INotificationHandler<CurrentSongNotification>>(sp =>
                sp.GetRequiredService<PlaybackControlsViewModel>());
            services.AddSingleton<INotificationHandler<AppThemeChangedNotification>>(sp =>
                sp.GetRequiredService<PlaybackControlsViewModel>());
            
            services.AddSingleton<INotificationHandler<FontFamilyChangedNotification>>(sp =>
                sp.GetRequiredService<MainWindowViewModel>());
            services.AddSingleton<INotificationHandler<CurrentSongNotification>>(sp =>
                sp.GetRequiredService<MainWindowViewModel>());
            services.AddSingleton<INotificationHandler<PlayerStateChangedNotification>>(sp =>
                sp.GetRequiredService<MainWindowViewModel>());

            services.AddSingleton<INotificationHandler<AdvancedSearchCompletedNotification>>(sp => 
                sp.GetRequiredService<AdvancedSearchViewModel>());
            services.AddSingleton<INotificationHandler<FontFamilyChangedNotification>>(sp =>
                sp.GetRequiredService<AdvancedSearchViewModel>());
            
            services.AddSingleton<INotificationHandler<NewSongWindowPositionChangedNotification>>(sp =>
                sp.GetRequiredService<CornerWindowViewModel>());
            services.AddSingleton<INotificationHandler<CurrentSongNotification>>(sp =>
                sp.GetRequiredService<CornerWindowViewModel>());
            services.AddSingleton<INotificationHandler<FontFamilyChangedNotification>>(sp =>
                sp.GetRequiredService<CornerWindowViewModel>());
            
            services.AddSingleton<INotificationHandler<FolderBrowserNotification>>(sp =>
                sp.GetRequiredService<SettingsWindowViewModel>());
            services.AddSingleton<INotificationHandler<PinnedFoldersChangedNotification>>(sp =>
                sp.GetRequiredService<SettingsWindowViewModel>());

            services.AddSingleton<INotificationHandler<CurrentSongNotification>>(sp =>
                sp.GetRequiredService<ListsViewModel>());
            services.AddSingleton<INotificationHandler<FontFamilyChangedNotification>>(sp =>
                sp.GetRequiredService<ListsViewModel>());
            services.AddSingleton<INotificationHandler<AdvancedSearchNotification>>(sp =>
                sp.GetRequiredService<ListsViewModel>());
            services.AddSingleton<INotificationHandler<QuickSearchResultsNotification>>(sp =>
                sp.GetRequiredService<ListsViewModel>());
            services.AddSingleton<INotificationHandler<ExternalAudioFilesOpenedNotification>>(sp =>
                sp.GetRequiredService<ListsViewModel>());
            services.AddSingleton<INotificationHandler<PlaylistShuffledNotification>>(sp =>
                sp.GetRequiredService<ListsViewModel>());
            
            services.AddSingleton<INotificationHandler<PlaylistViewModeChangedNotification>>(sp =>
                sp.GetRequiredService<PlaylistPaneViewModel>());
            services.AddSingleton<INotificationHandler<PlaylistCreatedNotification>>(sp =>
                sp.GetRequiredService<PlaylistPaneViewModel>());
            services.AddSingleton<INotificationHandler<PlaylistRenamedNotification>>(sp =>
                sp.GetRequiredService<PlaylistPaneViewModel>());
            services.AddSingleton<INotificationHandler<PlaylistDeletedNotification>>(sp =>
                sp.GetRequiredService<PlaylistPaneViewModel>());
            services.AddSingleton<INotificationHandler<PlaylistMembershipChangedNotification>>(sp =>
                sp.GetRequiredService<PlaylistPaneViewModel>());
            services.AddSingleton<INotificationHandler<PlaylistShuffledNotification>>(sp =>
                sp.GetRequiredService<PlaylistPaneViewModel>());
            services.AddSingleton<INotificationHandler<FontFamilyChangedNotification>>(sp =>
                sp.GetRequiredService<PlaylistPaneViewModel>());
            
            services.AddSingleton<INotificationHandler<FontFamilyChangedNotification>>(sp =>
                sp.GetRequiredService<SearchResultsPaneViewModel>());
            
            services.AddSingleton<INotificationHandler<FontFamilyChangedNotification>>(sp =>
                sp.GetRequiredService<FolderBrowserViewModel>());
            
            services.AddSingleton<INotificationHandler<FontFamilyChangedNotification>>(sp =>
                sp.GetRequiredService<SearchbarViewModel>());
        });
        
        return builder;
    }
}