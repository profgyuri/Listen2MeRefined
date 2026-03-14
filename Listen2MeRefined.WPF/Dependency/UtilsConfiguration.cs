using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Folders;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Threading;
using Listen2MeRefined.Application.Updating;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;
using Listen2MeRefined.Infrastructure.FolderBrowser;
using Listen2MeRefined.Infrastructure.Playlist;
using Listen2MeRefined.Infrastructure.Searching;
using Listen2MeRefined.Infrastructure.Settings;
using Listen2MeRefined.Infrastructure.Startup;
using Listen2MeRefined.Infrastructure.Versioning;
using Listen2MeRefined.WPF.Utils.Navigation;
using Listen2MeRefined.WPF.Utils.Theming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Listen2MeRefined.WPF.Dependency;

public static class UtilsConfiguration
{
    internal static IHostBuilder ConfigureUtils(this IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ISettingsManager<AppSettings>, DatabaseSettingsManager<AppSettings>>();
            services.AddTransient<IVersionChecker, VersionChecker>();
            services.AddTransient<IAppSettingsReader, AppSettingsReader>();
            services.AddTransient<IAppSettingsWriter, AppSettingsWriter>();
            services.AddTransient<IDroppedSongFolderPromptService, DroppedSongFolderPromptService>();
            services.AddTransient<IAppUpdateChecker, AppUpdateChecker>();
            services.AddSingleton<IBackgroundTaskStatusService, BackgroundTaskStatusService>();
            services.AddTransient<IGlobalHookSettingsSyncService, GlobalHookSettingsSyncService>();
            services.AddTransient<IFolderNavigationService, FolderNavigationService>();
            services.AddTransient<IPinnedFoldersService, PinnedFoldersService>();
            services.AddTransient<IAdvancedSearchCriteriaService, AdvancedSearchCriteriaService>();
            services.AddTransient<IAudioSearchExecutionService, AudioSearchExecutionService>();
            services.AddTransient<IPlaybackDefaultsService, PlaybackDefaultsService>();
            services.AddTransient<IWindowPositionPolicyService, WindowPositionPolicyService>();
            services.AddTransient<IPlaylistLibraryService, PlaylistLibraryService>();
            services.AddSingleton<IAppThemeService, AppThemeService>();
            services.AddSingleton<IExternalAudioOpenService, ExternalAudioOpenService>();
            services.AddSingleton<IErrorHandler, LoggingErrorHandler>();
            services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
            services.AddSingleton<IWindowManager, WindowManager>();
            services.AddSingleton<IWindowRegistry, WindowRegistry>();
            services.AddSingleton<IUiDispatcher>(_ => new WpfUiDispatcher(System.Windows.Application.Current.Dispatcher));
        });
        
        return builder;
    }
}