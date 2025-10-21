# راهنمای معماری Vertical Slice

این راهنمای جامع نحوه استفاده از رابط `IFeature` در Raziee.SharedKernel برای پیاده‌سازی معماری Vertical Slice در برنامه‌های شما را توضیح می‌دهد.

## فهرست مطالب

- [مقدمه](#مقدمه)
- [معماری Vertical Slice چیست؟](#معماری-vertical-slice-چیست)
- [رابط IFeature](#رابط-ifeature)
- [کلاس FeatureBase](#کلاس-featurebase)
- [پیاده‌سازی Features](#پیاده‌سازی-features)
- [مدیریت Features](#مدیریت-features)
- [پیکربندی Features](#پیکربندی-features)
- [بهترین شیوه‌ها](#بهترین-شیوه‌ها)
- [مثال کامل](#مثال-کامل)
- [استراتژی‌های Migration](#استراتژی‌های-migration)

## مقدمه

معماری Vertical Slice یک الگوی معماری نرم‌افزار است که کد را حول قابلیت‌های کسب‌وکار به جای لایه‌های فنی سازماندهی می‌کند. هر "slice" شامل تمام کدهای لازم برای پیاده‌سازی یک ویژگی کسب‌وکار خاص، از دیتابیس تا رابط کاربری است.

Raziee.SharedKernel رابط `IFeature` و کلاس `FeatureBase` را برای پشتیبانی از این الگوی معماری فراهم می‌کند، که سازماندهی و مدیریت ویژگی‌های کسب‌وکار در برنامه شما را آسان می‌کند.

## معماری Vertical Slice چیست؟

معماری لایه‌ای سنتی کد را بر اساس نگرانی‌های فنی سازماندهی می‌کند:

```
معماری لایه‌ای سنتی:
├── Controllers/
├── Services/
├── Repositories/
├── Models/
└── Data/
```

معماری Vertical Slice کد را بر اساس ویژگی‌های کسب‌وکار سازماندهی می‌کند:

```
معماری Vertical Slice:
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

### مزایای معماری Vertical Slice

1. **تمرکز کسب‌وکار**: کد حول قابلیت‌های کسب‌وکار سازماندهی می‌شود
2. **کاهش Coupling**: ویژگی‌ها به‌طور شل coupled هستند
3. **تست آسان‌تر**: هر ویژگی می‌تواند مستقل تست شود
4. **نگهداری بهتر**: تغییرات یک ویژگی در slice خودش محدود می‌شود
5. **مقیاس‌پذیری تیم**: تیم‌های مختلف می‌توانند روی ویژگی‌های مختلف کار کنند
6. **Feature Toggle**: ویژگی‌ها می‌توانند مستقل فعال/غیرفعال شوند

## رابط IFeature

رابط `IFeature` یک رابط marker است که یک قابلیت کسب‌وکار خودکفا را نشان می‌دهد.

### تعریف رابط

```csharp
public interface IFeature
{
    /// <summary>
    /// نام ویژگی را دریافت می‌کند.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// نسخه ویژگی را دریافت می‌کند.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// توضیحات ویژگی را دریافت می‌کند.
    /// </summary>
    string Description { get; }
}
```

### ویژگی‌های کلیدی

- **خودکفا**: هر ویژگی شامل تمام اجزای لازم است
- **نسخه‌دار**: ویژگی‌ها می‌توانند مستقل نسخه‌دار شوند
- **توضیح‌دار**: ویژگی‌ها توضیحات واضح برای مستندات دارند
- **قابل شناسایی**: هر ویژگی نام منحصر به فرد دارد

## کلاس FeatureBase

کلاس `FeatureBase` پیاده‌سازی پایه `IFeature` با قابلیت‌های اضافی برای مدیریت lifecycle ویژگی فراهم می‌کند.

### تعریف کلاس

```csharp
public abstract class FeatureBase : IFeature
{
    // ویژگی‌های اصلی
    public string Name { get; }
    public string Version { get; }
    public string Description { get; }

    // وضعیت ویژگی
    public virtual bool IsEnabled => true;
    public virtual IEnumerable<string> Dependencies => Enumerable.Empty<string>();

    // متدهای lifecycle
    public virtual Task InitializeAsync(CancellationToken cancellationToken = default);
    public virtual Task ShutdownAsync(CancellationToken cancellationToken = default);
}
```

### ویژگی‌های کلیدی

- **مدیریت Lifecycle**: مقداردهی اولیه و خاموش کردن ویژگی‌ها
- **ردیابی وابستگی**: تعریف وابستگی‌های ویژگی
- **فعال/غیرفعال**: کنترل دسترسی ویژگی
- **نمایش رشته**: اطلاعات ویژگی قابل خواندن توسط انسان

## پیاده‌سازی Features

### پیاده‌سازی ویژگی پایه

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
        // مقداردهی اولیه سرویس‌های مدیریت کاربر
        // ثبت repositories، services، و غیره
        await Task.CompletedTask;
    }

    public override async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        // پاکسازی منابع
        await Task.CompletedTask;
    }
}
```

### پیاده‌سازی ویژگی پیشرفته

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
            // ثبت سرویس‌ها
            await RegisterServicesAsync();
            
            // مقداردهی اولیه repositories
            await InitializeRepositoriesAsync();
            
            // تنظیم event handlers
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
            // پاکسازی منابع
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
        // ثبت سرویس‌های خاص ویژگی
        await Task.CompletedTask;
    }

    private async Task InitializeRepositoriesAsync()
    {
        // مقداردهی اولیه repositories
        await Task.CompletedTask;
    }

    private async Task SetupEventHandlersAsync()
    {
        // تنظیم event handlers
        await Task.CompletedTask;
    }

    private async Task CleanupResourcesAsync()
    {
        // پاکسازی منابع
        await Task.CompletedTask;
    }
}
```

## مدیریت Features

### ثبت ویژگی

```csharp
// Program.cs یا Startup.cs
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ثبت ویژگی‌ها
        builder.Services.AddFeature<UserManagementFeature>();
        builder.Services.AddFeature<OrderProcessingFeature>();
        builder.Services.AddFeature<PaymentProcessingFeature>();

        var app = builder.Build();

        // مقداردهی اولیه ویژگی‌ها
        await app.Services.InitializeFeaturesAsync();

        app.Run();
    }
}

// متدهای extension برای ثبت ویژگی
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

### مدیر ویژگی

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
        
        // مرتب‌سازی ویژگی‌ها بر اساس وابستگی‌ها
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
        
        // خاموش کردن به ترتیب معکوس
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

## پیکربندی Features

### مدل پیکربندی

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

### فایل پیکربندی

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

## بهترین شیوه‌ها

### 1. طراحی ویژگی

- **مسئولیت واحد**: هر ویژگی باید یک مسئولیت واحد و تعریف شده داشته باشد
- **مرزهای واضح**: ویژگی‌ها باید مرزهای واضح و coupling کم داشته باشند
- **خودکفا**: ویژگی‌ها باید شامل تمام اجزای لازم باشند
- **نسخه‌دار**: ویژگی‌ها باید مستقل نسخه‌دار شوند

### 2. مدیریت وابستگی

```csharp
// خوب: وابستگی‌های واضح
public class OrderProcessingFeature : FeatureBase
{
    public override IEnumerable<string> Dependencies => new[] 
    { 
        "UserManagement", 
        "ProductCatalog" 
    };
}

// بد: وابستگی‌های دایره‌ای
public class FeatureA : FeatureBase
{
    public override IEnumerable<string> Dependencies => new[] { "FeatureB" };
}

public class FeatureB : FeatureBase
{
    public override IEnumerable<string> Dependencies => new[] { "FeatureA" };
}
```

### 3. مدیریت خطا

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
            
            // منطق مقداردهی اولیه ویژگی
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
            
            // منطق خاموش کردن ویژگی
            await ShutdownFeatureAsync();
            
            _logger.LogInformation("RobustFeature shut down successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during RobustFeature shutdown");
            // در طول shutdown دوباره throw نکنید تا از شکست‌های cascade جلوگیری کنید
        }
    }

    private async Task InitializeFeatureAsync()
    {
        // مقداردهی اولیه خاص ویژگی
        await Task.CompletedTask;
    }

    private async Task ShutdownFeatureAsync()
    {
        // خاموش کردن خاص ویژگی
        await Task.CompletedTask;
    }
}
```

### 4. تست ویژگی‌ها

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
        new OrderProcessingFeature(), // وابسته به UserManagement
        new UserManagementFeature()   // بدون وابستگی
    };
    
    var featureManager = new FeatureManager(features, Mock.Of<ILogger<FeatureManager>>());

    // Act
    await featureManager.InitializeAllFeaturesAsync();

    // Assert
    // ویژگی‌ها باید به ترتیب وابستگی مقداردهی اولیه شوند
    // UserManagement باید قبل از OrderProcessing مقداردهی اولیه شود
}
```

## مثال کامل

### برنامه تجارت الکترونیک با Vertical Slices

```csharp
// ویژگی مدیریت کاربر
public class UserManagementFeature : FeatureBase
{
    public UserManagementFeature() 
        : base("UserManagement", "1.0.0", "User management and authentication")
    {
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // ثبت سرویس‌های مدیریت کاربر
        // تنظیم authentication
        // پیکربندی user repositories
        await Task.CompletedTask;
    }
}

// ویژگی کاتالوگ محصول
public class ProductCatalogFeature : FeatureBase
{
    public ProductCatalogFeature() 
        : base("ProductCatalog", "2.0.0", "Product catalog and inventory management")
    {
    }

    public override IEnumerable<string> Dependencies => new[] { "UserManagement" };

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // ثبت سرویس‌های محصول
        // تنظیم مدیریت موجودی
        // پیکربندی product repositories
        await Task.CompletedTask;
    }
}

// ویژگی پردازش سفارش
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
        // ثبت سرویس‌های سفارش
        // تنظیم workflows پردازش سفارش
        // پیکربندی order repositories
        await Task.CompletedTask;
    }
}

// ویژگی پردازش پرداخت
public class PaymentProcessingFeature : FeatureBase
{
    public PaymentProcessingFeature() 
        : base("PaymentProcessing", "1.5.0", "Payment processing and billing")
    {
    }

    public override IEnumerable<string> Dependencies => new[] { "UserManagement" };

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // ثبت سرویس‌های پرداخت
        // تنظیم payment gateways
        // پیکربندی billing repositories
        await Task.CompletedTask;
    }
}

// راه‌اندازی برنامه
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ثبت ویژگی‌ها
        builder.Services.AddFeature<UserManagementFeature>();
        builder.Services.AddFeature<ProductCatalogFeature>();
        builder.Services.AddFeature<OrderProcessingFeature>();
        builder.Services.AddFeature<PaymentProcessingFeature>();

        // ثبت مدیر ویژگی
        builder.Services.AddScoped<IFeatureManager, FeatureManager>();

        var app = builder.Build();

        // مقداردهی اولیه ویژگی‌ها
        app.Services.InitializeFeaturesAsync().Wait();

        app.Run();
    }
}
```

## استراتژی‌های Migration

### از Monolith به Vertical Slices

1. **شناسایی قابلیت‌های کسب‌وکار**: شناسایی قابلیت‌های کسب‌وکار متمایز
2. **ایجاد مرزهای ویژگی**: تعریف مرزهای واضح بین ویژگی‌ها
3. **استخراج ویژگی‌ها**: انتقال کدهای مرتبط به slice های ویژگی
4. **پیاده‌سازی IFeature**: تبدیل modules به features
5. **افزودن وابستگی‌ها**: تعریف وابستگی‌های ویژگی
6. **تست ویژگی‌ها**: اطمینان از کارکرد مستقل ویژگی‌ها

### از Vertical Slices به Microservices

1. **شناسایی مرزهای سرویس**: شناسایی ویژگی‌هایی که می‌توانند سرویس شوند
2. **استخراج API ها**: ایجاد API ها برای ارتباط cross-feature
3. **پیاده‌سازی Service Discovery**: افزودن مکانیزم‌های service discovery
4. **جداسازی دیتابیس**: جداسازی دیتابیس‌ها برای هر سرویس
5. **Deploy مستقل**: deploy ویژگی‌ها به عنوان سرویس‌های مستقل

## نتیجه‌گیری

معماری Vertical Slice با `IFeature` راه قدرتمندی برای سازماندهی کد حول قابلیت‌های کسب‌وکار فراهم می‌کند. با استفاده از سیستم ویژگی Raziee.SharedKernel، می‌توانید:

- **کد را** حول ویژگی‌های کسب‌وکار سازماندهی کنید
- **وابستگی‌ها** بین ویژگی‌ها را مدیریت کنید
- **lifecycle ویژگی** را با مقداردهی اولیه و خاموش کردن کنترل کنید
- **ویژگی‌ها را** به‌طور پویا فعال/غیرفعال کنید
- **ویژگی‌ها را** مستقل نسخه‌دار کنید
- **ویژگی‌ها را** در انزوا تست کنید

این رویکرد منجر به برنامه‌های قابل نگهداری‌تر، قابل تست‌تر و مقیاس‌پذیرتر می‌شود که می‌توانند از monolith ها به microservices ها در صورت نیاز تکامل یابند.
