# راهنمای ارتباط سرویس‌ها

این راهنمای جامع نحوه استفاده از الگوهای ارتباط سرویس‌ها در Raziee.SharedKernel برای ساخت معماری‌های میکروسرویس مقاوم را نشان می‌دهد.

## فهرست مطالب

- [مقدمه](#مقدمه)
- [الگوی Circuit Breaker](#الگوی-circuit-breaker)
- [سیاست‌های Retry](#سیاست‌های-retry)
- [Service Discovery](#service-discovery)
- [مثال کامل: ارتباط سرویس مقاوم](#مثال-کامل-ارتباط-سرویس-مقاوم)
- [بهترین شیوه‌ها](#بهترین-شیوه‌ها)

## مقدمه

الگوهای ارتباط سرویس‌ها مقاومت و قابلیت اطمینان برای سیستم‌های توزیع شده فراهم می‌کنند. Raziee.SharedKernel الگوهای Circuit Breaker، سیاست‌های Retry، و Service Discovery را پیاده‌سازی می‌کند تا خطاها را به‌طور مناسب مدیریت کند و پایداری سیستم را حفظ کند.

## الگوی Circuit Breaker

### 1. رابط Circuit Breaker

```csharp
using Raziee.SharedKernel.ServiceCommunication;

public interface ICircuitBreaker
{
    string Name { get; }
    CircuitBreakerState State { get; }
    
    Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default
    );
    
    Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}

public enum CircuitBreakerState
{
    Closed,    // عملیات عادی
    Open,      // Circuit باز است، فراخوانی‌ها سریع شکست می‌خورند
    HalfOpen   // تست اینکه آیا سرویس برگشته است
}
```

### 2. پیاده‌سازی Circuit Breaker

```csharp
public class CircuitBreaker : ICircuitBreaker
{
    private readonly ILogger<CircuitBreaker> _logger;
    private readonly int _failureThreshold;
    private readonly TimeSpan _timeout;
    private readonly TimeSpan _retryTimeout;
    
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private DateTimeOffset _lastFailureTime = DateTimeOffset.MinValue;

    public CircuitBreaker(
        ILogger<CircuitBreaker> logger,
        int failureThreshold = 5,
        TimeSpan timeout = default,
        TimeSpan retryTimeout = default)
    {
        _logger = logger;
        _failureThreshold = failureThreshold;
        _timeout = timeout == default ? TimeSpan.FromMinutes(1) : timeout;
        _retryTimeout = retryTimeout == default ? TimeSpan.FromMinutes(5) : retryTimeout;
    }

    public string Name => "DefaultCircuitBreaker";
    public CircuitBreakerState State => _state;

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        if (_state == CircuitBreakerState.Open)
        {
            if (DateTimeOffset.UtcNow - _lastFailureTime < _retryTimeout)
            {
                _logger.LogWarning("Circuit breaker is open, failing fast");
                throw new CircuitBreakerOpenException("Circuit breaker is open");
            }
            
            _state = CircuitBreakerState.HalfOpen;
            _logger.LogInformation("Circuit breaker moved to half-open state");
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_timeout);
            
            var result = await operation();
            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure();
            _logger.LogError(ex, "Operation failed, circuit breaker state: {State}", _state);
            throw;
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation();
            return true;
        }, cancellationToken);
    }

    private void OnSuccess()
    {
        _failureCount = 0;
        _state = CircuitBreakerState.Closed;
        _logger.LogDebug("Operation succeeded, circuit breaker closed");
    }

    private void OnFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTimeOffset.UtcNow;

        if (_failureCount >= _failureThreshold)
        {
            _state = CircuitBreakerState.Open;
            _logger.LogWarning("Circuit breaker opened after {FailureCount} failures", _failureCount);
        }
    }
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message)
    {
    }
}
```

### 3. Circuit Breaker پیشرفته با Metrics

```csharp
public class AdvancedCircuitBreaker : ICircuitBreaker
{
    private readonly ILogger<AdvancedCircuitBreaker> _logger;
    private readonly CircuitBreakerOptions _options;
    private readonly IMetricsCollector _metricsCollector;
    
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private int _successCount = 0;
    private DateTimeOffset _lastFailureTime = DateTimeOffset.MinValue;
    private DateTimeOffset _lastSuccessTime = DateTimeOffset.MinValue;

    public AdvancedCircuitBreaker(
        ILogger<AdvancedCircuitBreaker> logger,
        CircuitBreakerOptions options,
        IMetricsCollector metricsCollector)
    {
        _logger = logger;
        _options = options;
        _metricsCollector = metricsCollector;
    }

    public string Name => _options.Name;
    public CircuitBreakerState State => _state;

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        
        try
        {
            if (!CanExecute())
            {
                _metricsCollector.IncrementCounter("circuit_breaker_rejected", new Dictionary<string, string>
                {
                    ["circuit_breaker"] = _options.Name,
                    ["state"] = _state.ToString()
                });
                
                throw new CircuitBreakerOpenException($"Circuit breaker {_options.Name} is open");
            }

            var result = await operation();
            OnSuccess();
            
            _metricsCollector.RecordDuration("circuit_breaker_execution_time", 
                DateTimeOffset.UtcNow - startTime, new Dictionary<string, string>
                {
                    ["circuit_breaker"] = _options.Name,
                    ["result"] = "success"
                });
            
            return result;
        }
        catch (Exception ex)
        {
            OnFailure(ex);
            
            _metricsCollector.RecordDuration("circuit_breaker_execution_time", 
                DateTimeOffset.UtcNow - startTime, new Dictionary<string, string>
                {
                    ["circuit_breaker"] = _options.Name,
                    ["result"] = "failure"
                });
            
            throw;
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation();
            return true;
        }, cancellationToken);
    }

    private bool CanExecute()
    {
        return _state switch
        {
            CircuitBreakerState.Closed => true,
            CircuitBreakerState.Open => DateTimeOffset.UtcNow - _lastFailureTime >= _options.RetryTimeout,
            CircuitBreakerState.HalfOpen => true,
            _ => false
        };
    }

    private void OnSuccess()
    {
        _successCount++;
        _lastSuccessTime = DateTimeOffset.UtcNow;
        
        if (_state == CircuitBreakerState.HalfOpen)
        {
            _state = CircuitBreakerState.Closed;
            _failureCount = 0;
            _logger.LogInformation("Circuit breaker {Name} closed after successful operation", _options.Name);
        }
        
        _metricsCollector.IncrementCounter("circuit_breaker_success", new Dictionary<string, string>
        {
            ["circuit_breaker"] = _options.Name
        });
    }

    private void OnFailure(Exception exception)
    {
        _failureCount++;
        _lastFailureTime = DateTimeOffset.UtcNow;
        
        if (_state == CircuitBreakerState.Closed && _failureCount >= _options.FailureThreshold)
        {
            _state = CircuitBreakerState.Open;
            _logger.LogWarning("Circuit breaker {Name} opened after {FailureCount} failures", 
                _options.Name, _failureCount);
        }
        else if (_state == CircuitBreakerState.HalfOpen)
        {
            _state = CircuitBreakerState.Open;
            _logger.LogWarning("Circuit breaker {Name} opened after failure in half-open state", _options.Name);
        }
        
        _metricsCollector.IncrementCounter("circuit_breaker_failure", new Dictionary<string, string>
        {
            ["circuit_breaker"] = _options.Name,
            ["exception_type"] = exception.GetType().Name
        });
    }
}

public class CircuitBreakerOptions
{
    public string Name { get; set; } = "DefaultCircuitBreaker";
    public int FailureThreshold { get; set; } = 5;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan RetryTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public double SuccessThreshold { get; set; } = 0.8;
}

public interface IMetricsCollector
{
    void IncrementCounter(string name, Dictionary<string, string> tags);
    void RecordDuration(string name, TimeSpan duration, Dictionary<string, string> tags);
}
```

## سیاست‌های Retry

### 1. رابط سیاست Retry

```csharp
using Raziee.SharedKernel.ServiceCommunication;

public interface IRetryPolicy
{
    int MaxRetries { get; }
    TimeSpan BaseDelay { get; }
    TimeSpan MaxDelay { get; }
    double BackoffMultiplier { get; }
    
    Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default
    );
    
    Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default);
    bool ShouldRetry(Exception exception);
}
```

### 2. پیاده‌سازی سیاست Retry

```csharp
public class RetryPolicy : IRetryPolicy
{
    private readonly ILogger<RetryPolicy> _logger;
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;
    private readonly double _backoffMultiplier;
    private readonly TimeSpan _maxDelay;

    public RetryPolicy(
        ILogger<RetryPolicy> logger,
        int maxRetries = 3,
        TimeSpan baseDelay = default,
        double backoffMultiplier = 2.0,
        TimeSpan maxDelay = default)
    {
        _logger = logger;
        _maxRetries = maxRetries;
        _baseDelay = baseDelay == default ? TimeSpan.FromSeconds(1) : baseDelay;
        _backoffMultiplier = backoffMultiplier;
        _maxDelay = maxDelay == default ? TimeSpan.FromMinutes(5) : maxDelay;
    }

    public int MaxRetries => _maxRetries;
    public TimeSpan BaseDelay => _baseDelay;
    public TimeSpan MaxDelay => _maxDelay;
    public double BackoffMultiplier => _backoffMultiplier;

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt == _maxRetries)
                {
                    _logger.LogError(ex, "Operation failed after {MaxRetries} retries", _maxRetries);
                    throw;
                }

                if (!ShouldRetry(ex))
                {
                    _logger.LogWarning(ex, "Operation failed with non-retryable exception");
                    throw;
                }

                var delay = CalculateDelay(attempt);
                _logger.LogWarning(ex, "Operation failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}ms", 
                    attempt + 1, _maxRetries + 1, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("Operation failed");
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation();
            return true;
        }, cancellationToken);
    }

    public bool ShouldRetry(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => true,
            TaskCanceledException => true,
            TimeoutException => true,
            InvalidOperationException ex when ex.Message.Contains("timeout") => true,
            _ => false
        };
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, attempt));
        return delay > _maxDelay ? _maxDelay : delay;
    }
}
```

### 3. سیاست Retry پیشرفته با Jitter

```csharp
public class AdvancedRetryPolicy : IRetryPolicy
{
    private readonly ILogger<AdvancedRetryPolicy> _logger;
    private readonly RetryPolicyOptions _options;
    private readonly Random _random = new();

    public AdvancedRetryPolicy(ILogger<AdvancedRetryPolicy> logger, RetryPolicyOptions options)
    {
        _logger = logger;
        _options = options;
    }

    public int MaxRetries => _options.MaxRetries;
    public TimeSpan BaseDelay => _options.BaseDelay;
    public TimeSpan MaxDelay => _options.MaxDelay;
    public double BackoffMultiplier => _options.BackoffMultiplier;

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt == _maxRetries)
                {
                    _logger.LogError(ex, "Operation failed after {MaxRetries} retries", _maxRetries);
                    throw;
                }

                if (!ShouldRetry(ex))
                {
                    _logger.LogWarning(ex, "Operation failed with non-retryable exception");
                    throw;
                }

                var delay = CalculateDelayWithJitter(attempt);
                _logger.LogWarning(ex, "Operation failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}ms", 
                    attempt + 1, _maxRetries + 1, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("Operation failed");
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation();
            return true;
        }, cancellationToken);
    }

    public bool ShouldRetry(Exception exception)
    {
        if (_options.RetryableExceptions?.Any(type => type.IsInstanceOfType(exception)) == true)
            return true;

        return exception switch
        {
            HttpRequestException => true,
            TaskCanceledException => true,
            TimeoutException => true,
            InvalidOperationException ex when ex.Message.Contains("timeout") => true,
            _ => false
        };
    }

    private TimeSpan CalculateDelayWithJitter(int attempt)
    {
        var baseDelay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, attempt));
        var jitter = TimeSpan.FromMilliseconds(_random.NextDouble() * baseDelay.TotalMilliseconds * _options.JitterFactor);
        var totalDelay = baseDelay + jitter;
        
        return totalDelay > _maxDelay ? _maxDelay : totalDelay;
    }
}

public class RetryPolicyOptions
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(5);
    public double BackoffMultiplier { get; set; } = 2.0;
    public double JitterFactor { get; set; } = 0.1;
    public Type[]? RetryableExceptions { get; set; }
}
```

## Service Discovery

### 1. رابط Service Discovery

```csharp
using Raziee.SharedKernel.ServiceCommunication;

public interface IServiceDiscovery
{
    Task<IEnumerable<ServiceEndpoint>> DiscoverServicesAsync(
        string serviceName,
        CancellationToken cancellationToken = default
    );
    
    Task<ServiceEndpoint?> DiscoverServiceAsync(
        string serviceName,
        CancellationToken cancellationToken = default
    );
    
    Task RegisterServiceAsync(
        string serviceName,
        ServiceEndpoint endpoint,
        CancellationToken cancellationToken = default
    );
    
    Task UnregisterServiceAsync(
        string serviceName,
        ServiceEndpoint endpoint,
        CancellationToken cancellationToken = default
    );
}

public class ServiceEndpoint
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public ServiceHealth Health { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public enum ServiceHealth
{
    Healthy,
    Unhealthy,
    Unknown
}
```

### 2. پیاده‌سازی Service Discovery با Consul

```csharp
public class ConsulServiceDiscovery : IServiceDiscovery
{
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulServiceDiscovery> _logger;

    public ConsulServiceDiscovery(IConsulClient consulClient, ILogger<ConsulServiceDiscovery> logger)
    {
        _consulClient = consulClient;
        _logger = logger;
    }

    public async Task<IEnumerable<ServiceEndpoint>> DiscoverServicesAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Discovering services for {ServiceName}", serviceName);

        var services = await _consulClient.Health.Service(serviceName, string.Empty, true, cancellationToken);
        
        var endpoints = services.Response.Select(service => new ServiceEndpoint
        {
            Id = service.Service.ID,
            Name = service.Service.Service,
            Url = $"http://{service.Service.Address}:{service.Service.Port}",
            Health = service.Checks.All(c => c.Status == HealthStatus.Passing) ? ServiceHealth.Healthy : ServiceHealth.Unhealthy,
            Metadata = service.Service.Meta ?? new Dictionary<string, string>()
        });

        _logger.LogDebug("Found {Count} services for {ServiceName}", endpoints.Count(), serviceName);
        return endpoints;
    }

    public async Task<ServiceEndpoint?> DiscoverServiceAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        var services = await DiscoverServicesAsync(serviceName, cancellationToken);
        return services.FirstOrDefault(s => s.Health == ServiceHealth.Healthy);
    }

    public async Task RegisterServiceAsync(
        string serviceName,
        ServiceEndpoint endpoint,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Registering service {ServiceName} at {Endpoint}", serviceName, endpoint.Url);

        var registration = new AgentServiceRegistration
        {
            ID = endpoint.Id,
            Name = serviceName,
            Address = GetHostFromUrl(endpoint.Url),
            Port = GetPortFromUrl(endpoint.Url),
            Check = new AgentServiceCheck
            {
                HTTP = $"{endpoint.Url}/health",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5)
            },
            Meta = endpoint.Metadata
        };

        await _consulClient.Agent.ServiceRegister(registration, cancellationToken);
    }

    public async Task UnregisterServiceAsync(
        string serviceName,
        ServiceEndpoint endpoint,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Unregistering service {ServiceName} at {Endpoint}", serviceName, endpoint.Url);
        await _consulClient.Agent.ServiceDeregister(endpoint.Id, cancellationToken);
    }

    private string GetHostFromUrl(string url)
    {
        var uri = new Uri(url);
        return uri.Host;
    }

    private int GetPortFromUrl(string url)
    {
        var uri = new Uri(url);
        return uri.Port;
    }
}
```

### 3. Service Discovery با Load Balancing

```csharp
public class LoadBalancingServiceDiscovery : IServiceDiscovery
{
    private readonly IServiceDiscovery _baseServiceDiscovery;
    private readonly ILoadBalancer _loadBalancer;
    private readonly ILogger<LoadBalancingServiceDiscovery> _logger;

    public LoadBalancingServiceDiscovery(
        IServiceDiscovery baseServiceDiscovery,
        ILoadBalancer loadBalancer,
        ILogger<LoadBalancingServiceDiscovery> logger)
    {
        _baseServiceDiscovery = baseServiceDiscovery;
        _loadBalancer = loadBalancer;
        _logger = logger;
    }

    public async Task<IEnumerable<ServiceEndpoint>> DiscoverServicesAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        return await _baseServiceDiscovery.DiscoverServicesAsync(serviceName, cancellationToken);
    }

    public async Task<ServiceEndpoint?> DiscoverServiceAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        var services = await DiscoverServicesAsync(serviceName, cancellationToken);
        var healthyServices = services.Where(s => s.Health == ServiceHealth.Healthy).ToList();
        
        if (!healthyServices.Any())
        {
            _logger.LogWarning("No healthy services found for {ServiceName}", serviceName);
            return null;
        }

        var selectedService = _loadBalancer.SelectService(healthyServices);
        _logger.LogDebug("Selected service {ServiceId} for {ServiceName}", selectedService.Id, serviceName);
        
        return selectedService;
    }

    public async Task RegisterServiceAsync(
        string serviceName,
        ServiceEndpoint endpoint,
        CancellationToken cancellationToken = default)
    {
        await _baseServiceDiscovery.RegisterServiceAsync(serviceName, endpoint, cancellationToken);
    }

    public async Task UnregisterServiceAsync(
        string serviceName,
        ServiceEndpoint endpoint,
        CancellationToken cancellationToken = default)
    {
        await _baseServiceDiscovery.UnregisterServiceAsync(serviceName, endpoint, cancellationToken);
    }
}

public interface ILoadBalancer
{
    ServiceEndpoint SelectService(IEnumerable<ServiceEndpoint> services);
}

public class RoundRobinLoadBalancer : ILoadBalancer
{
    private readonly Dictionary<string, int> _serviceCounters = new();
    private readonly object _lock = new();

    public ServiceEndpoint SelectService(IEnumerable<ServiceEndpoint> services)
    {
        var serviceList = services.ToList();
        if (!serviceList.Any())
            throw new InvalidOperationException("No services available");

        var serviceName = serviceList.First().Name;
        
        lock (_lock)
        {
            if (!_serviceCounters.ContainsKey(serviceName))
                _serviceCounters[serviceName] = 0;

            var selectedIndex = _serviceCounters[serviceName] % serviceList.Count;
            _serviceCounters[serviceName]++;
            
            return serviceList[selectedIndex];
        }
    }
}
```

## مثال کامل: ارتباط سرویس مقاوم

### 1. کلاینت سرویس با مقاومت

```csharp
public class ResilientServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly IRetryPolicy _retryPolicy;
    private readonly ILogger<ResilientServiceClient> _logger;

    public ResilientServiceClient(
        HttpClient httpClient,
        IServiceDiscovery serviceDiscovery,
        ICircuitBreaker circuitBreaker,
        IRetryPolicy retryPolicy,
        ILogger<ResilientServiceClient> logger)
    {
        _httpClient = httpClient;
        _serviceDiscovery = serviceDiscovery;
        _circuitBreaker = circuitBreaker;
        _retryPolicy = retryPolicy;
        _logger = logger;
    }

    public async Task<TResponse> GetAsync<TResponse>(
        string serviceName,
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var serviceEndpoint = await _serviceDiscovery.DiscoverServiceAsync(serviceName, cancellationToken);
                if (serviceEndpoint == null)
                    throw new ServiceUnavailableException($"Service {serviceName} is not available");

                var url = $"{serviceEndpoint.Url}{endpoint}";
                _logger.LogDebug("Making request to {Url}", url);

                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<TResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("Failed to deserialize response");
            });
        });
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        string serviceName,
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var serviceEndpoint = await _serviceDiscovery.DiscoverServiceAsync(serviceName, cancellationToken);
                if (serviceEndpoint == null)
                    throw new ServiceUnavailableException($"Service {serviceName} is not available");

                var url = $"{serviceEndpoint.Url}{endpoint}";
                _logger.LogDebug("Making POST request to {Url}", url);

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<TResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("Failed to deserialize response");
            });
        });
    }
}

public class ServiceUnavailableException : Exception
{
    public ServiceUnavailableException(string message) : base(message)
    {
    }
}
```

### 2. کلاینت سرویس کاربر

```csharp
public class UserServiceClient
{
    private readonly ResilientServiceClient _serviceClient;
    private readonly ILogger<UserServiceClient> _logger;

    public UserServiceClient(ResilientServiceClient serviceClient, ILogger<UserServiceClient> logger)
    {
        _serviceClient = serviceClient;
        _logger = logger;
    }

    public async Task<UserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting user {UserId}", userId);
        
        return await _serviceClient.GetAsync<UserDto>(
            "UserService",
            $"/api/users/{userId}",
            cancellationToken);
    }

    public async Task<bool> IsUserActiveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await GetUserAsync(userId, cancellationToken);
            return user.IsActive;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check user status for {UserId}", userId);
            return false; // پیش‌فرض false برای امنیت
        }
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating user with email {Email}", request.Email);
        
        return await _serviceClient.PostAsync<CreateUserRequest, UserDto>(
            "UserService",
            "/api/users",
            request,
            cancellationToken);
    }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CreateUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

### 3. سرویس سفارش با مقاومت

```csharp
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly UserServiceClient _userServiceClient;
    private readonly ProductServiceClient _productServiceClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        UserServiceClient userServiceClient,
        ProductServiceClient productServiceClient,
        IUnitOfWork unitOfWork,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _userServiceClient = userServiceClient;
        _productServiceClient = productServiceClient;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // اعتبارسنجی مشتری با مقاومت
            if (!await _userServiceClient.IsUserActiveAsync(request.CustomerId, cancellationToken))
                throw new InvalidOperationException("Customer is not active");

            // اعتبارسنجی محصولات با مقاومت
            foreach (var item in request.Items)
            {
                var product = await _productServiceClient.GetProductAsync(item.ProductId, cancellationToken);
                if (product == null)
                    throw new InvalidOperationException($"Product {item.ProductId} not found");

                if (!product.IsAvailable || product.StockQuantity < item.Quantity)
                    throw new InvalidOperationException($"Product {item.ProductId} is not available in requested quantity");
            }

            // ایجاد سفارش
            var order = new Order(Guid.NewGuid(), request.CustomerId);
            foreach (var item in request.Items)
            {
                order.AddItem(item.ProductId, item.Quantity, item.UnitPrice);
            }

            await _orderRepository.AddAsync(order, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} created successfully", order.Id);
            return order.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order for customer {CustomerId}", request.CustomerId);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
```

### 4. ثبت سرویس‌ها

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // افزودن Raziee.SharedKernel
        builder.Services.AddSharedKernel();

        // افزودن ارتباط سرویس‌ها
        builder.Services.AddScoped<ICircuitBreaker, AdvancedCircuitBreaker>();
        builder.Services.AddScoped<IRetryPolicy, AdvancedRetryPolicy>();
        builder.Services.AddScoped<IServiceDiscovery, LoadBalancingServiceDiscovery>();
        builder.Services.AddScoped<ILoadBalancer, RoundRobinLoadBalancer>();

        // تنظیم circuit breaker
        builder.Services.Configure<CircuitBreakerOptions>(options =>
        {
            options.Name = "OrderServiceCircuitBreaker";
            options.FailureThreshold = 5;
            options.Timeout = TimeSpan.FromSeconds(30);
            options.RetryTimeout = TimeSpan.FromMinutes(2);
        });

        // تنظیم سیاست retry
        builder.Services.Configure<RetryPolicyOptions>(options =>
        {
            options.MaxRetries = 3;
            options.BaseDelay = TimeSpan.FromSeconds(1);
            options.MaxDelay = TimeSpan.FromMinutes(2);
            options.BackoffMultiplier = 2.0;
            options.JitterFactor = 0.1;
        });

        // افزودن HTTP client با مقاومت
        builder.Services.AddHttpClient<ResilientServiceClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // افزودن کلاینت‌های سرویس
        builder.Services.AddScoped<UserServiceClient>();
        builder.Services.AddScoped<ProductServiceClient>();
        builder.Services.AddScoped<OrderService>();

        var app = builder.Build();

        app.Run();
    }
}
```

## بهترین شیوه‌ها

### 1. طراحی Circuit Breaker
- آستانه‌های خطای مناسب تنظیم کنید
- از timeout های معنادار استفاده کنید
- انتقال state های مناسب پیاده‌سازی کنید
- metrics های circuit breaker را مانیتور کنید

### 2. طراحی سیاست Retry
- از exponential backoff با jitter استفاده کنید
- شرایط retry مناسب پیاده‌سازی کنید
- محدودیت‌های retry منطقی تنظیم کنید
- تلاش‌های retry را لاگ کنید

### 3. Service Discovery
- health check ها را پیاده‌سازی کنید
- از استراتژی‌های load balancing استفاده کنید
- خطاهای سرویس را به‌طور مناسب مدیریت کنید
- دسترسی سرویس را مانیتور کنید

### 4. الگوهای مقاومت
- چندین الگوی مقاومت را ترکیب کنید
- مدیریت خطای مناسب پیاده‌سازی کنید
- از timeout های مناسب استفاده کنید
- سلامت سیستم را مانیتور کنید

### 5. تست
- سناریوهای خطا را تست کنید
- از chaos engineering استفاده کنید
- مانیتورینگ مناسب پیاده‌سازی کنید
- رفتار circuit breaker را تست کنید

این راهنما پایه جامعی برای پیاده‌سازی الگوهای ارتباط سرویس‌ها با Raziee.SharedKernel ارائه می‌دهد، شامل تمام الگوها و شیوه‌های لازم برای ساخت سیستم‌های توزیع شده مقاوم و قابل اعتماد.
