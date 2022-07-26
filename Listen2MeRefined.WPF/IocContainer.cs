namespace Listen2MeRefined.WPF;

using Autofac;
using Listen2MeRefined.Infrastructure.LowLevel;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Mvvm;
using Serilog;
using System.Collections.Generic;
using System;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Listen2MeRefined.Core.Interfaces.DataHandlers;

internal static class IocContainer
{
    const string seqConnection = "http://localhost:5341";
    static readonly HashSet<ConsoleKey> lowLevelKeys = 
        new(){
                    ConsoleKey.MediaPlay,
                    ConsoleKey.MediaStop,
                    ConsoleKey.MediaPrevious,
                    ConsoleKey.MediaNext
                };

    internal static IContainer RegisterDependencies()
    {
        var builder = new ContainerBuilder();

        #region Windows and Pages
        builder.RegisterType<MainWindow>().SingleInstance();
        #endregion

        #region ViewModels
        builder.RegisterType<MainWindowViewModel>();
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
            .RegisterType<EntityFrameworkRemover>()
            .As<IDataRemover>();
        
        builder
            .RegisterType<EntityFrameworkReader>()
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

        return builder.Build();
    }

    private static Serilog.Core.Logger CreateLogger()
    {
        var config = new LoggerConfiguration();

        config
            .WriteTo.Async(conf => conf.Seq(seqConnection));

        return config.CreateLogger();
    }
}
