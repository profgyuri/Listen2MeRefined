using Autofac;
using Serilog;

namespace Listen2MeRefined.WPF.Dependency.Modules;

internal sealed class LoggerModule : Module
{
    private const string SeqConnection = "http://localhost:5341";
    
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(_ =>
            {
                var config = new LoggerConfiguration();

                config
                    .WriteTo.Async(conf => conf.Seq(SeqConnection));
                config
                    .MinimumLevel.Debug();

                return config.CreateLogger();
            })
            .As<ILogger>().SingleInstance();
    }
}