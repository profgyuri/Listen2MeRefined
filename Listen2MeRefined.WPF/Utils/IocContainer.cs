using System.Data;
using System.Linq;
using System.Windows.Media;
using Autofac;
using Listen2MeRefined.Core;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.Dapper;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.SystemOperations;
using Listen2MeRefined.WPF.Views;
using MediatR;
using Microsoft.Data.Sqlite;
using Serilog;
using Source;
using Source.Storage;
using IDataReader = Listen2MeRefined.Core.Interfaces.DataHandlers.IDataReader;

namespace Listen2MeRefined.WPF;

internal static class IocContainer
{
    private static IContainer? _container;

    private const string SeqConnection = "http://localhost:5341";

    internal static IContainer GetContainer()
    {
        if (_container is not null)
        {
            return _container;
        }

        var builder = new ContainerBuilder();

        #region Windows and Pages
        builder.RegisterType<MainWindow>().SingleInstance();
        builder.RegisterType<FolderBrowserWindow>().InstancePerLifetimeScope();
        builder.RegisterType<AdvancedSearchWindow>().InstancePerLifetimeScope();
        builder.RegisterType<SettingsWindow>().InstancePerLifetimeScope();
        builder.RegisterType<NewSongWindow>().InstancePerDependency();
        #endregion

        #region ViewModels
        builder
            .RegisterType<MainWindowViewModel>()
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();
        builder.RegisterType<FolderBrowserViewModel>();
        builder.RegisterType<AdvancedSearchViewModel>();
        builder.RegisterType<NewSongWindowViewModel>()
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();
        builder
            .RegisterType<SettingsWindowViewModel>()
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();
        #endregion

        #region Logger
        builder.Register(_ => CreateLogger())
            .As<ILogger>().SingleInstance();
        #endregion

        #region Data Access
        builder
            .RegisterType<DataContext>()
            .SingleInstance();

        builder
            .RegisterType<DbConnection>()
            .SingleInstance();

        builder
            .Register(_ =>
            {
                var conn = new SqliteConnection(DbInfo.SqliteConnectionString);
                conn.Open();
                return conn;
            })
            .As<IDbConnection>()
            .SingleInstance();

        builder
            .RegisterType<EntityFrameworkRemover>()
            .As<IDataRemover>();

        builder
            .RegisterType<DapperReader>()
            .As<IDataReader>();

        builder
            .RegisterType<EntityFrameworkSaver>()
            .As<IDataSaver>();

        builder
            .RegisterType<EntityFrameworkUpdater>()
            .As<IDataUpdater>();

        builder
            .RegisterType<AudioRepository>()
            .As<IRepository<AudioModel>>();
        #endregion

        builder
            .RegisterType<WindowsMusicPlayer>()
            .As<IMediaController>()
            .As<IPlaylistReference>()
            .SingleInstance();

        builder
            .RegisterType<GmaGlobalHookHandler>()
            .As<IGlobalHook>()
            .SingleInstance();

        #region MediatR
        builder
            .RegisterType<Mediator>()
            .As<IMediator>()
            .InstancePerLifetimeScope();

        builder.Register<ServiceFactory>(context =>
        {
            var c = context.Resolve<IComponentContext>();
            return t => c.Resolve(t);
        });
        #endregion

        builder
            .RegisterType<FolderBrowser>()
            .As<IFolderBrowser>();

        builder
            .RegisterType<DatabaseSettingsManager<AppSettings>>()
            .As<ISettingsManager<AppSettings>>();

        builder
            .RegisterType<SoundFileAnalyzer>()
            .As<IFileAnalyzer<AudioModel>>();

        builder
            .RegisterType<FileEnumerator>()
            .As<IFileEnumerator>();

        builder
            .Register(_ => new TimedTask())
            .InstancePerDependency();

        builder
            .Register(_ => new FontFamilies(Fonts.SystemFontFamilies.Select(f => f.Source)))
            .SingleInstance();

        return _container = builder.Build();
    }

    private static Serilog.Core.Logger CreateLogger()
    {
        var config = new LoggerConfiguration();

        config
            .WriteTo.Async(conf => conf.Seq(SeqConnection));
        config
            .MinimumLevel.Debug();

        return config.CreateLogger();
    }
}