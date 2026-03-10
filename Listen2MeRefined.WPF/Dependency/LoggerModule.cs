using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;

namespace Listen2MeRefined.WPF.Dependency;

public static class LoggerModule
{
    internal static IHostBuilder ConfigureLogger(this IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ILogger>(_ => CreateLogger());
        });

        return builder;
    }
    
    private static Logger CreateLogger()
    {
        var config = new LoggerConfiguration();
        const string seqConnection = "http://192.168.0.22:5341";

        config
            .WriteTo.Async(conf => conf.Seq(seqConnection));
        config
            .WriteTo.Async(conf => conf.File(
                "log.txt", 
                retainedFileCountLimit: 3,
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 1024 * 1024 * 10));
        config
            .MinimumLevel.Verbose();

        return config.CreateLogger();
    }
}