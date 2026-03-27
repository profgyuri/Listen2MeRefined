using System.Reflection;
using Serilog;

namespace Listen2MeRefined.Application.Modules;

/// <summary>
/// Stores discovered modules, and provides discovery helpers.
/// </summary>
public sealed class ModuleCatalog : IModuleCatalog
{
    private readonly IReadOnlyList<IModule> _modules;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleCatalog"/> class.
    /// </summary>
    /// <param name="modules">The discovered module list.</param>
    public ModuleCatalog(IReadOnlyList<IModule> modules)
    {
        _modules = modules ?? throw new ArgumentNullException(nameof(modules));
        EnsureUniqueNames(_modules);
    }

    /// <inheritdoc />
    public IReadOnlyList<IModule> LoadModules() => _modules;

    /// <summary>
    /// Discovers modules through assembly scanning.
    /// </summary>
    /// <param name="options">Module discovery options.</param>
    /// <param name="logger">An optional logger.</param>
    /// <returns>A deterministic discovered module list.</returns>
    public static IReadOnlyList<IModule> DiscoverModules(
        ModuleCatalogOptions options,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var discovered = new List<IModule>();
        var seenModuleTypes = new HashSet<Type>();

        if (options.EnableAssemblyScan)
        {
            var prefixes = options.ScanAssemblyPrefixes
                .Where(prefix => !string.IsNullOrWhiteSpace(prefix))
                .Select(prefix => prefix.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var candidateAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic)
                .Where(assembly =>
                {
                    if (prefixes.Length == 0)
                    {
                        return true;
                    }

                    var assemblyName = assembly.GetName().Name ?? string.Empty;
                    return prefixes.Any(prefix => assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                })
                .OrderBy(assembly => assembly.GetName().Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var assembly in candidateAssemblies)
            {
                foreach (var moduleType in GetLoadableModuleTypes(assembly))
                {
                    if (!seenModuleTypes.Add(moduleType))
                    {
                        continue;
                    }

                    if (Activator.CreateInstance(moduleType) is not IModule module)
                    {
                        continue;
                    }

                    discovered.Add(module);
                    logger?.Information("Discovered module {ModuleName} from assembly {AssemblyName}.", module.Name,
                        assembly.GetName().Name);
                }
            }
        }

        EnsureUniqueNames(discovered);
        return discovered;
    }

    private static IEnumerable<Type> GetLoadableModuleTypes(Assembly assembly)
    {
        try
        {
            return assembly
                .GetTypes()
                .Where(type =>
                    typeof(IModule).IsAssignableFrom(type) &&
                    type is { IsClass: true, IsAbstract: false } &&
                    (type.IsPublic || type.IsNestedPublic) &&
                    type.GetConstructor(Type.EmptyTypes) is not null)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types
                .Where(type => type is not null)
                .Cast<Type>()
                .Where(type =>
                    typeof(IModule).IsAssignableFrom(type) &&
                    type is { IsClass: true, IsAbstract: false } &&
                    (type.IsPublic || type.IsNestedPublic) &&
                    type.GetConstructor(Type.EmptyTypes) is not null)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();
        }
    }

    private static void EnsureUniqueNames(IEnumerable<IModule> modules)
    {
        var duplicate = modules
            .GroupBy(module => module.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is null)
        {
            return;
        }

        throw new InvalidOperationException($"Duplicate module name detected: '{duplicate.Key}'.");
    }
}
