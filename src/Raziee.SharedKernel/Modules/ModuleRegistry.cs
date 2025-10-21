using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Raziee.SharedKernel.Modules;

/// <summary>
/// Registry for managing modules in modular monolith architecture.
/// Handles module discovery, initialization, and communication.
/// </summary>
public class ModuleRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ModuleRegistry> _logger;
    private readonly Dictionary<string, IModule> _modules = new();
    private readonly Dictionary<string, List<string>> _dependencies = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleRegistry"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="logger">The logger</param>
    public ModuleRegistry(IServiceProvider serviceProvider, ILogger<ModuleRegistry> logger)
    {
        _serviceProvider =
            serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all registered modules.
    /// </summary>
    public IReadOnlyDictionary<string, IModule> Modules => _modules.AsReadOnly();

    /// <summary>
    /// Registers a module.
    /// </summary>
    /// <param name="module">The module to register</param>
    public void RegisterModule(IModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        _logger.LogDebug(
            "Registering module {ModuleName} v{ModuleVersion}",
            module.Name,
            module.Version
        );

        if (_modules.ContainsKey(module.Name))
        {
            throw new InvalidOperationException($"Module '{module.Name}' is already registered");
        }

        _modules[module.Name] = module;
        _dependencies[module.Name] = module.Dependencies.ToList();

        _logger.LogInformation(
            "Successfully registered module {ModuleName} v{ModuleVersion}",
            module.Name,
            module.Version
        );
    }

    /// <summary>
    /// Gets a module by name.
    /// </summary>
    /// <param name="name">The name of the module</param>
    /// <returns>The module if found; otherwise, null</returns>
    public IModule? GetModule(string name)
    {
        return _modules.TryGetValue(name, out var module) ? module : null;
    }

    /// <summary>
    /// Gets all modules that depend on the specified module.
    /// </summary>
    /// <param name="moduleName">The name of the module</param>
    /// <returns>A collection of dependent modules</returns>
    public IEnumerable<IModule> GetDependentModules(string moduleName)
    {
        return _dependencies
            .Where(kvp => kvp.Value.Contains(moduleName))
            .Select(kvp => _modules[kvp.Key])
            .ToList();
    }

    /// <summary>
    /// Initializes all modules in dependency order.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task InitializeAllModulesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing all modules");

        var sortedModules = SortModulesByDependencies();
        var initializedModules = new HashSet<string>();

        foreach (var module in sortedModules)
        {
            await InitializeModuleAsync(module, initializedModules, cancellationToken);
        }

        _logger.LogInformation("Successfully initialized all modules");
    }

    /// <summary>
    /// Shuts down all modules in reverse dependency order.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task ShutdownAllModulesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Shutting down all modules");

        var sortedModules = SortModulesByDependencies();
        sortedModules.Reverse();

        foreach (var module in sortedModules)
        {
            try
            {
                await module.ShutdownAsync(cancellationToken);
                _logger.LogDebug("Successfully shut down module {ModuleName}", module.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shutting down module {ModuleName}", module.Name);
            }
        }

        _logger.LogInformation("Successfully shut down all modules");
    }

    /// <summary>
    /// Initializes a single module and its dependencies.
    /// </summary>
    /// <param name="module">The module to initialize</param>
    /// <param name="initializedModules">The set of already initialized modules</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private async Task InitializeModuleAsync(
        IModule module,
        HashSet<string> initializedModules,
        CancellationToken cancellationToken
    )
    {
        if (initializedModules.Contains(module.Name))
        {
            return;
        }

        // Initialize dependencies first
        foreach (var dependency in module.Dependencies)
        {
            var dependencyModule = GetModule(dependency);
            if (dependencyModule != null)
            {
                await InitializeModuleAsync(
                    dependencyModule,
                    initializedModules,
                    cancellationToken
                );
            }
        }

        // Initialize the module
        await module.InitializeAsync(cancellationToken);
        initializedModules.Add(module.Name);

        _logger.LogDebug("Successfully initialized module {ModuleName}", module.Name);
    }

    /// <summary>
    /// Sorts modules by their dependencies using topological sort.
    /// </summary>
    /// <returns>A list of modules sorted by dependencies</returns>
    private List<IModule> SortModulesByDependencies()
    {
        var result = new List<IModule>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        foreach (var module in _modules.Values)
        {
            VisitModule(module, visited, visiting, result);
        }

        return result;
    }

    /// <summary>
    /// Visits a module and its dependencies for topological sorting.
    /// </summary>
    /// <param name="module">The module to visit</param>
    /// <param name="visited">The set of visited modules</param>
    /// <param name="visiting">The set of modules currently being visited</param>
    /// <param name="result">The result list</param>
    private void VisitModule(
        IModule module,
        HashSet<string> visited,
        HashSet<string> visiting,
        List<IModule> result
    )
    {
        if (visited.Contains(module.Name))
        {
            return;
        }

        if (visiting.Contains(module.Name))
        {
            throw new InvalidOperationException(
                $"Circular dependency detected involving module '{module.Name}'"
            );
        }

        visiting.Add(module.Name);

        foreach (var dependency in module.Dependencies)
        {
            var dependencyModule = GetModule(dependency);
            if (dependencyModule != null)
            {
                VisitModule(dependencyModule, visited, visiting, result);
            }
        }

        visiting.Remove(module.Name);
        visited.Add(module.Name);
        result.Add(module);
    }
}
