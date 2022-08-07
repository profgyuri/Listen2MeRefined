namespace Listen2MeRefined.WPF;

using Autofac;
using Listen2MeRefined.Infrastructure.LowLevel;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Mvvm;
using Serilog;
using System.Collections.Generic;
using System;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.Dapper;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Listen2MeRefined.Infrastructure.SystemOperations;
using Listen2MeRefined.Core.Interfaces.DataHandlers;
using System.Data;
using IDataReader = IDataReader;
using Microsoft.Data.SqlClient;
using MediatR.Extensions.Autofac.DependencyInjection;
using Listen2MeRefined.WPF.Views;
using MediatR;
using Listen2MeRefined.Infrastructure.Notifications;

internal static class IocContainer
{
    private static IContainer? _container;

    const string seqConnection = "http://localhost:5341";
    static readonly HashSet<ConsoleKey> lowLevelKeys = 
        new(){
                    ConsoleKey.MediaPlay,
                    ConsoleKey.MediaStop,
                    ConsoleKey.MediaPrevious,
                    ConsoleKey.MediaNext
                };

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
        #endregion

        #region ViewModels
        builder.RegisterType<MainWindowViewModel>();
        builder.RegisterType<FolderBrowserViewModel>();
        builder.RegisterType<AdvancedSearchViewModel>();
        builder
            .RegisterType<SettingsWindowViewModel>()
            .AsSelf()
            .As<INotificationHandler<FolderBrowserNotification>>()
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
            .Register(_ => new SqlConnection(DbInfo.SqliteConnectionString))
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
            .RegisterType<MusicPlayer>()
            .As<IMediaController>()
            .SingleInstance();

        builder
            .Register(_ => new KeyboardHook(lowLevelKeys))
            .SingleInstance();

        builder
            .RegisterType<Mediator>()
            .As<IMediator>()
            .InstancePerLifetimeScope();

        builder.Register<ServiceFactory>(context =>
        {
            var c = context.Resolve<IComponentContext>();
            return t => c.Resolve(t);
        });

        builder
            .RegisterType<FolderBrowser>()
            .As<IFolderBrowser>();

        return _container = builder.Build();
    }

    private static Serilog.Core.Logger CreateLogger()
    {
        var config = new LoggerConfiguration();

        config
            .WriteTo.Async(conf => conf.Seq(seqConnection));
        config
            .MinimumLevel.Debug();

        return config.CreateLogger();
    }
}
