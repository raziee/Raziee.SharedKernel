# Vertical Slice Architecture Guide

This comprehensive guide explains how to use Raziee.SharedKernel's IFeature interface to implement Vertical Slice Architecture in your applications.

## Table of Contents

- [Introduction](#introduction)
- [What is Vertical Slice Architecture?](#what-is-vertical-slice-architecture)
- [IFeature Interface](#ifeature-interface)
- [FeatureBase Class](#featurebase-class)
- [Implementing Features](#implementing-features)
- [Feature Management](#feature-management)
- [Feature Configuration](#feature-configuration)
- [Best Practices](#best-practices)
- [Complete Example](#complete-example)
- [Migration Strategies](#migration-strategies)

## Introduction

Vertical Slice Architecture is a software architecture pattern that organizes code around business capabilities rather than technical layers. Each "slice" contains all the code needed to implement a specific business feature, from the database to the user interface.

Raziee.SharedKernel provides the `IFeature` interface and `FeatureBase` class to support this architecture pattern, making it easy to organize and manage business features in your application.

## What is Vertical Slice Architecture?

Traditional layered architecture organizes code by technical concerns:

```
Traditional Layered Architecture:
├── Controllers/
├── Services/
├── Repositories/
├── Models/
└── Data/
```

Vertical Slice Architecture organizes code by business features:

```
Vertical Slice Architecture:
├── Features/
│   ├── UserManagement/
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Repositories/
│   │   └── Models/
│   ├── OrderProcessing/
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Repositories/
│   │   └── Models/
│   └── PaymentProcessing/
│       ├── Controllers/
│       ├── Services/
│       ├── Repositories/
│       └── Models/
```

### Benefits of Vertical Slice Architecture

1. **Business Focus**: Code is organized around business capabilities
2. **Reduced Coupling**: Features are loosely coupled
3. **Easier Testing**: Each feature can be tested independently
4. **Better Maintainability**: Changes to a feature are contained within its slice
5. **Team Scalability**: Different teams can work on different features
6. **Feature Toggle**: Features can be enabled/disabled independently

## IFeature Interface

The `IFeature` interface is a marker interface that represents a self-contained business capability.

### Interface Definition

```csharp
public interface IFeature
{
    /// <summary>
    /// Gets the name of the feature.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the feature.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the description of the feature.
    /// </summary>
    string Description { get; }
}
```

### Key Characteristics

- **Self-contained**: Each feature contains all necessary components
- **Versioned**: Features can be versioned independently
- **Described**: Features have clear descriptions for documentation
- **Identifiable**: Each feature has a unique name

## FeatureBase Class

The `FeatureBase` class provides a base implementation of `IFeature` with additional functionality for feature lifecycle management.

### Class Definition

```csharp
public abstract class FeatureBase : IFeature
{
    // Core properties
    public string Name { get; }
    public string Version { get; }
    public string Description { get; }

    // Feature state
    public virtual bool IsEnabled => true;
    public virtual IEnumerable<string> Dependencies => Enumerable.Empty<string>();

    // Lifecycle methods
    public virtual Task InitializeAsync(CancellationToken cancellationToken = default);
    public virtual Task ShutdownAsync(CancellationToken cancellationToken = default);
}
```

### Key Features

- **Lifecycle Management**: Initialize and shutdown features
- **Dependency Tracking**: Define feature dependencies
- **Enable/Disable**: Control feature availability
- **String Representation**: Human-readable feature information

## Implementing Features

### Basic Feature Implementation

```csharp
public class UserManagementFeature : FeatureBase
{
    public UserManagementFeature() 
        : base("UserManagement", "1.0.0", "User management and authentication")
    {
    }

    public override bool IsEnabled => true;
    
    public override IEnumerable<string> Dependencies => new[] 
    { 
        "SharedKernel", 
        "IdentityModule" 
    };

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Initialize user management services
        // Register repositories, services, etc.
        await Task.CompletedTask;
    }

    public override async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        // Cleanup resources
        await Task.CompletedTask;
    }
}
```

### Advanced Feature Implementation

```csharp
public class OrderProcessingFeature : FeatureBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderProcessingFeature> _logger;

    public OrderProcessingFeature(
        IServiceProvider serviceProvider,
        ILogger<OrderProcessingFeature> logger) 
        : base("OrderProcessing", "2.1.0", "Order creation and processing")
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override bool IsEnabled => true;
    
    public override IEnumerable<string> Dependencies => new[] 
    { 
        "UserManagement", 
        "ProductCatalog",
        "PaymentProcessing"
    };

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing OrderProcessing feature");

        try
        {
            // Register services
            await RegisterServicesAsync();
            
            // Initialize repositories
            await InitializeRepositoriesAsync();
            
            // Setup event handlers
            await SetupEventHandlersAsync();
            
            _logger.LogInformation("OrderProcessing feature initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize OrderProcessing feature");
            throw;
        }
    }

    public override async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Shutting down OrderProcessing feature");

        try
        {
            // Cleanup resources
            await CleanupResourcesAsync();
            
            _logger.LogInformation("OrderProcessing feature shut down successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OrderProcessing feature shutdown");
        }
    }

    private async Task RegisterServicesAsync()
    {
        // Register feature-specific services
        await Task.CompletedTask;
    }

    private async Task InitializeRepositoriesAsync()
    {
        // Initialize repositories
        await Task.CompletedTask;
    }

    private async Task SetupEventHandlersAsync()
    {
        // Setup event handlers
        await Task.CompletedTask;
    }

    private async Task CleanupResourcesAsync()
    {
        // Cleanup resources
        await Task.CompletedTask;
    }
}
```

## Feature Management

### Feature Registration

```csharp
// Program.cs or Startup.cs
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register features
        builder.Services.AddFeature<UserManagementFeature>();
        builder.Services.AddFeature<OrderProcessingFeature>();
        builder.Services.AddFeature<PaymentProcessingFeature>();

        var app = builder.Build();

        // Initialize features
        await app.Services.InitializeFeaturesAsync();

        app.Run();
    }
}

// Extension methods for feature registration
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFeature<TFeature>(this IServiceCollection services)
        where TFeature : class, IFeature
    {
        services.AddScoped<IFeature, TFeature>();
        return services;
    }

    public static IServiceCollection AddFeatures(this IServiceCollection services, params Type[] featureTypes)
    {
        foreach (var featureType in featureTypes)
        {
            if (typeof(IFeature).IsAssignableFrom(featureType))
            {
                services.AddScoped(typeof(IFeature), featureType);
            }
        }
        return services;
    }

    public static async Task InitializeFeaturesAsync(this IServiceProvider serviceProvider)
    {
        var featureManager = serviceProvider.GetRequiredService<IFeatureManager>();
        await featureManager.InitializeAllFeaturesAsync();
    }
}
```

### Feature Manager

```csharp
public interface IFeatureManager
{
    Task InitializeAllFeaturesAsync(CancellationToken cancellationToken = default);
    Task ShutdownAllFeaturesAsync(CancellationToken cancellationToken = default);
    IEnumerable<IFeature> GetEnabledFeatures();
    IEnumerable<IFeature> GetFeaturesByDependency(string dependency);
    IFeature? GetFeature(string name);
}

public class FeatureManager : IFeatureManager
{
    private readonly IEnumerable<IFeature> _features;
    private readonly ILogger<FeatureManager> _logger;

    public FeatureManager(IEnumerable<IFeature> features, ILogger<FeatureManager> logger)
    {
        _features = features;
        _logger = logger;
    }

    public async Task InitializeAllFeaturesAsync(CancellationToken cancellationToken = default)
    {
        var enabledFeatures = _features.Where(f => f.IsEnabled).ToList();
        
        _logger.LogInformation("Initializing {Count} features", enabledFeatures.Count);
        
        // Sort features by dependencies
        var sortedFeatures = SortFeaturesByDependencies(enabledFeatures);
        
        foreach (var feature in sortedFeatures)
        {
            try
            {
                _logger.LogInformation("Initializing feature: {FeatureName}", feature.Name);
                await feature.InitializeAsync(cancellationToken);
                _logger.LogInformation("Successfully initialized feature: {FeatureName}", feature.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize feature: {FeatureName}", feature.Name);
                throw;
            }
        }
    }

    public async Task ShutdownAllFeaturesAsync(CancellationToken cancellationToken = default)
    {
        var enabledFeatures = _features.Where(f => f.IsEnabled).ToList();
        
        _logger.LogInformation("Shutting down {Count} features", enabledFeatures.Count);
        
        // Shutdown in reverse order
        var sortedFeatures = SortFeaturesByDependencies(enabledFeatures).Reverse();
        
        foreach (var feature in sortedFeatures)
        {
            try
            {
                _logger.LogInformation("Shutting down feature: {FeatureName}", feature.Name);
                await feature.ShutdownAsync(cancellationToken);
                _logger.LogInformation("Successfully shut down feature: {FeatureName}", feature.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to shut down feature: {FeatureName}", feature.Name);
            }
        }
    }

    public IEnumerable<IFeature> GetEnabledFeatures()
    {
        return _features.Where(f => f.IsEnabled);
    }

    public IEnumerable<IFeature> GetFeaturesByDependency(string dependency)
    {
        return _features.Where(f => f.Dependencies.Contains(dependency));
    }

    public IFeature? GetFeature(string name)
    {
        return _features.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private IEnumerable<IFeature> SortFeaturesByDependencies(IEnumerable<IFeature> features)
    {
        var sorted = new List<IFeature>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        foreach (var feature in features)
        {
            if (!visited.Contains(feature.Name))
            {
                VisitFeature(feature, features, sorted, visited, visiting);
            }
        }

        return sorted;
    }

    private void VisitFeature(
        IFeature feature,
        IEnumerable<IFeature> allFeatures,
        List<IFeature> sorted,
        HashSet<string> visited,
        HashSet<string> visiting)
    {
        if (visiting.Contains(feature.Name))
        {
            throw new InvalidOperationException($"Circular dependency detected involving feature: {feature.Name}");
        }

        if (visited.Contains(feature.Name))
        {
            return;
        }

        visiting.Add(feature.Name);

        foreach (var dependency in feature.Dependencies)
        {
            var dependencyFeature = allFeatures.FirstOrDefault(f => f.Name == dependency);
            if (dependencyFeature != null)
            {
                VisitFeature(dependencyFeature, allFeatures, sorted, visited, visiting);
            }
        }

        visiting.Remove(feature.Name);
        visited.Add(feature.Name);
        sorted.Add(feature);
    }
}
```

## Feature Configuration

### Configuration Model

```csharp
public class FeatureConfiguration
{
    public bool Enabled { get; set; } = true;
    public string Version { get; set; } = "1.0.0";
    public Dictionary<string, object> Settings { get; set; } = new();
}

public class FeatureConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FeatureConfigurationService> _logger;

    public FeatureConfigurationService(IConfiguration configuration, ILogger<FeatureConfigurationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsFeatureEnabled(string featureName)
    {
        var featureConfig = _configuration.GetSection($"Features:{featureName}").Get<FeatureConfiguration>();
        return featureConfig?.Enabled ?? true;
    }

    public string GetFeatureVersion(string featureName)
    {
        var featureConfig = _configuration.GetSection($"Features:{featureName}").Get<FeatureConfiguration>();
        return featureConfig?.Version ?? "1.0.0";
    }

    public T GetFeatureSetting<T>(string featureName, string settingName, T defaultValue = default)
    {
        var featureConfig = _configuration.GetSection($"Features:{featureName}").Get<FeatureConfiguration>();
        if (featureConfig?.Settings.TryGetValue(settingName, out var value) == true)
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        return defaultValue;
    }
}
```

### Configuration File

```json
{
  "Features": {
    "UserManagement": {
      "Enabled": true,
      "Version": "1.0.0",
      "Settings": {
        "MaxUsers": 1000,
        "EnableTwoFactorAuth": true
      }
    },
    "OrderProcessing": {
      "Enabled": true,
      "Version": "2.1.0",
      "Settings": {
        "MaxOrderItems": 50,
        "EnableOrderTracking": true
      }
    },
    "PaymentProcessing": {
      "Enabled": false,
      "Version": "1.5.0",
      "Settings": {
        "PaymentProvider": "Stripe",
        "EnableRefunds": true
      }
    }
  }
}
```

## Best Practices

### 1. Feature Design

- **Single Responsibility**: Each feature should have a single, well-defined responsibility
- **Clear Boundaries**: Features should have clear boundaries and minimal coupling
- **Self-contained**: Features should contain all necessary components
- **Versioned**: Features should be versioned independently

### 2. Dependency Management

```csharp
// Good: Clear dependencies
public class OrderProcessingFeature : FeatureBase
{
    public override IEnumerable<string> Dependencies => new[] 
    { 
        "UserManagement", 
        "ProductCatalog" 
    };
}

// Bad: Circular dependencies
public class FeatureA : FeatureBase
{
    public override IEnumerable<string> Dependencies => new[] { "FeatureB" };
}

public class FeatureB : FeatureBase
{
    public override IEnumerable<string> Dependencies => new[] { "FeatureA" };
}
```

### 3. Error Handling

```csharp
public class RobustFeature : FeatureBase
{
    private readonly ILogger<RobustFeature> _logger;

    public RobustFeature(ILogger<RobustFeature> logger) 
        : base("RobustFeature", "1.0.0", "A robust feature implementation")
    {
        _logger = logger;
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing RobustFeature");
            
            // Feature initialization logic
            await InitializeFeatureAsync();
            
            _logger.LogInformation("RobustFeature initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RobustFeature");
            throw;
        }
    }

    public override async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Shutting down RobustFeature");
            
            // Feature shutdown logic
            await ShutdownFeatureAsync();
            
            _logger.LogInformation("RobustFeature shut down successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during RobustFeature shutdown");
            // Don't rethrow during shutdown to avoid cascading failures
        }
    }

    private async Task InitializeFeatureAsync()
    {
        // Feature-specific initialization
        await Task.CompletedTask;
    }

    private async Task ShutdownFeatureAsync()
    {
        // Feature-specific shutdown
        await Task.CompletedTask;
    }
}
```

### 4. Testing Features

```csharp
[Test]
public async Task UserManagementFeature_ShouldInitializeSuccessfully()
{
    // Arrange
    var feature = new UserManagementFeature();
    var cancellationToken = CancellationToken.None;

    // Act
    await feature.InitializeAsync(cancellationToken);

    // Assert
    Assert.That(feature.IsEnabled, Is.True);
    Assert.That(feature.Name, Is.EqualTo("UserManagement"));
}

[Test]
public async Task FeatureManager_ShouldInitializeFeaturesInCorrectOrder()
{
    // Arrange
    var features = new List<IFeature>
    {
        new OrderProcessingFeature(), // Depends on UserManagement
        new UserManagementFeature()   // No dependencies
    };
    
    var featureManager = new FeatureManager(features, Mock.Of<ILogger<FeatureManager>>());

    // Act
    await featureManager.InitializeAllFeaturesAsync();

    // Assert
    // Features should be initialized in dependency order
    // UserManagement should be initialized before OrderProcessing
}
```

## Complete Example

### E-Commerce Application with Vertical Slices

```csharp
// User Management Feature
public class UserManagementFeature : FeatureBase
{
    public UserManagementFeature() 
        : base("UserManagement", "1.0.0", "User management and authentication")
    {
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Register user management services
        // Setup authentication
        // Configure user repositories
        await Task.CompletedTask;
    }
}

// Product Catalog Feature
public class ProductCatalogFeature : FeatureBase
{
    public ProductCatalogFeature() 
        : base("ProductCatalog", "2.0.0", "Product catalog and inventory management")
    {
    }

    public override IEnumerable<string> Dependencies => new[] { "UserManagement" };

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Register product services
        // Setup inventory management
        // Configure product repositories
        await Task.CompletedTask;
    }
}

// Order Processing Feature
public class OrderProcessingFeature : FeatureBase
{
    public OrderProcessingFeature() 
        : base("OrderProcessing", "3.0.0", "Order creation and processing")
    {
    }

    public override IEnumerable<string> Dependencies => new[] 
    { 
        "UserManagement", 
        "ProductCatalog" 
    };

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Register order services
        // Setup order processing workflows
        // Configure order repositories
        await Task.CompletedTask;
    }
}

// Payment Processing Feature
public class PaymentProcessingFeature : FeatureBase
{
    public PaymentProcessingFeature() 
        : base("PaymentProcessing", "1.5.0", "Payment processing and billing")
    {
    }

    public override IEnumerable<string> Dependencies => new[] { "UserManagement" };

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Register payment services
        // Setup payment gateways
        // Configure billing repositories
        await Task.CompletedTask;
    }
}

// Application Startup
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register features
        builder.Services.AddFeature<UserManagementFeature>();
        builder.Services.AddFeature<ProductCatalogFeature>();
        builder.Services.AddFeature<OrderProcessingFeature>();
        builder.Services.AddFeature<PaymentProcessingFeature>();

        // Register feature manager
        builder.Services.AddScoped<IFeatureManager, FeatureManager>();

        var app = builder.Build();

        // Initialize features
        app.Services.InitializeFeaturesAsync().Wait();

        app.Run();
    }
}
```

## Migration Strategies

### From Monolith to Vertical Slices

1. **Identify Business Capabilities**: Identify distinct business capabilities
2. **Create Feature Boundaries**: Define clear boundaries between features
3. **Extract Features**: Move related code into feature slices
4. **Implement IFeature**: Convert modules to features
5. **Add Dependencies**: Define feature dependencies
6. **Test Features**: Ensure features work independently

### From Vertical Slices to Microservices

1. **Identify Service Boundaries**: Identify features that can become services
2. **Extract APIs**: Create APIs for cross-feature communication
3. **Implement Service Discovery**: Add service discovery mechanisms
4. **Database Separation**: Separate databases for each service
5. **Deploy Independently**: Deploy features as independent services

## Conclusion

Vertical Slice Architecture with `IFeature` provides a powerful way to organize code around business capabilities. By using Raziee.SharedKernel's feature system, you can:

- **Organize code** around business features
- **Manage dependencies** between features
- **Control feature lifecycle** with initialization and shutdown
- **Enable/disable features** dynamically
- **Version features** independently
- **Test features** in isolation

This approach leads to more maintainable, testable, and scalable applications that can evolve from monoliths to microservices as needed.
