using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Updating;
using Listen2MeRefined.Infrastructure.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Modules;

public sealed class VersioningModule : IModule
{
    public string Name { get; } = "Versioning";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<IVersionChecker, VersionChecker>();
        services.AddTransient<IAppUpdateChecker, AppUpdateChecker>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
    }
}
