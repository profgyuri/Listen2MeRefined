using Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;
using Listen2MeRefined.Infrastructure.FolderBrowser;
using Listen2MeRefined.Infrastructure.Searching;
using Listen2MeRefined.Infrastructure.Settings;
using Listen2MeRefined.Infrastructure.Settings.Playback;
using Listen2MeRefined.Infrastructure.Settings.WindowPosition;
using Listen2MeRefined.Infrastructure.ViewModels;
using Listen2MeRefined.Infrastructure.ViewModels.MainWindow;

namespace Listen2MeRefined.WPF.Dependency.Modules;
using Autofac;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Versioning;
using Listen2MeRefined.WPF.Utils;
using System.Windows;

public class UtilsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<DatabaseSettingsManager<AppSettings>>()
            .As<ISettingsManager<AppSettings>>();

        builder
            .RegisterType<VersionChecker>()
            .As<IVersionChecker>();
        
        builder
            .RegisterType<AppSettingsReader>()
            .As<IAppSettingsReader>();
        builder
            .RegisterType<AppSettingsWriter>()
            .As<IAppSettingsWriter>();
        builder
            .RegisterType<AppUpdateChecker>()
            .As<IAppUpdateChecker>();
        builder
            .RegisterType<BackgroundTaskStatusService>()
            .As<IBackgroundTaskStatusService>()
            .SingleInstance();
        builder
            .RegisterType<GlobalHookSettingsSyncService>()
            .As<IGlobalHookSettingsSyncService>();
        builder
            .RegisterType<FolderNavigationService>()
            .As<IFolderNavigationService>();
        builder
            .RegisterType<PinnedFoldersService>()
            .As<IPinnedFoldersService>();
        builder
            .RegisterType<AdvancedSearchCriteriaService>()
            .As<IAdvancedSearchCriteriaService>();
        builder
            .RegisterType<AudioSearchExecutionService>()
            .As<IAudioSearchExecutionService>();
        builder
            .RegisterType<PlaybackDefaultsService>()
            .As<IPlaybackDefaultsService>();
        builder
            .RegisterType<WindowPositionPolicyService>()
            .As<IWindowPositionPolicyService>();

        builder
            .RegisterType<MainWindowNavigationService>()
            .As<IMainWindowNavigationService>()
            .SingleInstance();

        builder.Register(ctx =>
            new WpfUiDispatcher(Application.Current.Dispatcher))
               .As<IUiDispatcher>()
               .SingleInstance();
    }
}
