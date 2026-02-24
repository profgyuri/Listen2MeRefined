using Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;

namespace Listen2MeRefined.WPF.Dependency.Modules;
using Autofac;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Services;
using Listen2MeRefined.Infrastructure.Services.Contracts;
using Listen2MeRefined.Infrastructure.Storage;
using Listen2MeRefined.Infrastructure.Mvvm.MainWindow;
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
            .RegisterType<AppSettingsReadService>()
            .As<IAppSettingsReadService>();
        builder
            .RegisterType<AppSettingsWriteService>()
            .As<IAppSettingsWriteService>();
        builder
            .RegisterType<AppUpdateCheckService>()
            .As<IAppUpdateCheckService>();
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
