namespace Listen2MeRefined.WPF;

using Autofac;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Mvvm;
using Serilog;

internal static class IocContainer
{
    const string seqConnection = "http://localhost:5341";

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

        builder
            .RegisterType<MusicPlayer>()
            .As<IMediaController>()
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
