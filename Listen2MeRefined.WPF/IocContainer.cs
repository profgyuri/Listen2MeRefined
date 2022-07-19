namespace Listen2MeRefined.WPF;

using Autofac;
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

        #region Logger
        builder.Register(_ => CreateLogger())
            .As<ILogger>().SingleInstance();
        #endregion

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
