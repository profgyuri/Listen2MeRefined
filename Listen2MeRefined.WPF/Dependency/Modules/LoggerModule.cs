using Autofac;
using Serilog;
using Serilog.Core;

namespace Listen2MeRefined.WPF.Dependency.Modules;

public class LoggerModule : Module
{
    private const string SeqConnection = "http://localhost:5341";
    
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(_ => CreateLogger())
            .As<ILogger>().SingleInstance();
    }
    
    private static Logger CreateLogger()
    {
        var config = new LoggerConfiguration();

        config
            .WriteTo.Async(conf => conf.Seq(SeqConnection));
        config
            .MinimumLevel.Debug();

        return config.CreateLogger();
    }
}