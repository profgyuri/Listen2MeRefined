using Listen2MeRefined.WPF.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Listen2MeRefined.WPF.Dependency;

public static class ViewsModule
{
    internal static IHostBuilder ConfigureViews(this IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<MainWindow>();
            services.AddSingleton<SettingsWindow>();
            services.AddSingleton<CornerWindow>();
            services.AddTransient<FolderBrowserWindow>();
            services.AddTransient<AdvancedSearchWindow>();
        });
        
        return builder;
    }
}