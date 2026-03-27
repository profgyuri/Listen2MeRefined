using Listen2MeRefined.WPF.ErrorHandling;
using Serilog;
using Serilog.Core;

namespace Listen2MeRefined.WPF.Dependency;

public static class LoggerConfiguration
{
    public static Logger CreateLogger(ILogLocationService logLocationService)
    {
        var config = new Serilog.LoggerConfiguration();
        const string seqConnection = "http://192.168.0.22:5341";

        config
            .WriteTo.Async(conf => conf.Seq(seqConnection));
        config
            .WriteTo.Async(conf => conf.File(
                logLocationService.LogFilePath,
                retainedFileCountLimit: 3,
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 1024 * 1024 * 10,
                shared: true));
        config
            .MinimumLevel.Verbose();

        return config.CreateLogger();
    }
}
