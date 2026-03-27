using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.Tests.Modules;

public sealed class ModuleCatalogDuplicateNameTests
{
    [Fact]
    public void Constructor_DuplicateNames_ThrowsInvalidOperationException()
    {
        var duplicates = new IModule[]
        {
            new ExplicitDuplicateModule("Duplicate"),
            new ExplicitDuplicateModule("duplicate")
        };

        var exception = Assert.Throws<InvalidOperationException>(() => _ = new ModuleCatalog(duplicates));

        Assert.Contains("Duplicate module name", exception.Message);
    }

    [Fact]
    public void DiscoverModules_DuplicateNames_ThrowsInvalidOperationException()
    {
        var options = new ModuleCatalogOptions
        {
            EnableAssemblyScan = true,
            ScanAssemblyPrefixes = ["Listen2MeRefined.Tests"]
        };

        var exception = Assert.Throws<InvalidOperationException>(() => _ = ModuleCatalog.DiscoverModules(options));

        Assert.Contains("Duplicate module name", exception.Message);
    }

    private sealed class ExplicitDuplicateModule(string name) : IModule
    {
        public string Name { get; } = name;

        public void RegisterServices(IServiceCollection services)
        {
        }

        public void RegisterNavigation(INavigationRegistry registry)
        {
        }
    }

    public sealed class DiscoveryDuplicateModuleA : IModule
    {
        public string Name { get; } = "DiscoveryDuplicate";

        public void RegisterServices(IServiceCollection services)
        {
        }

        public void RegisterNavigation(INavigationRegistry registry)
        {
        }
    }

    public sealed class DiscoveryDuplicateModuleB : IModule
    {
        public string Name { get; } = "discoveryduplicate";

        public void RegisterServices(IServiceCollection services)
        {
        }

        public void RegisterNavigation(INavigationRegistry registry)
        {
        }
    }
}
