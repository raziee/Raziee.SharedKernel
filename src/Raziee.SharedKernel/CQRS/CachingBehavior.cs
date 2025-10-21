using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Raziee.SharedKernel.CQRS;

/// <summary>
/// Pipeline behavior for caching responses.
/// This behavior automatically caches responses for queries.
/// </summary>
/// <typeparam name="TRequest">The type of request</typeparam>
/// <typeparam name="TResponse">The type of response</typeparam>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="cache">The memory cache</param>
    /// <param name="logger">The logger</param>
    public CachingBehavior(IMemoryCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }

    /// <summary>
    /// Handles the request by checking the cache first, then processing if not cached.
    /// </summary>
    /// <param name="request">The request to handle</param>
    /// <param name="next">The next handler in the pipeline</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The response from the cache or the next handler</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var cacheKey = GenerateCacheKey(request);
        var requestName = typeof(TRequest).Name;

        _logger.LogDebug(
            "Checking cache for {RequestName} with key {CacheKey}",
            requestName,
            cacheKey
        );

        if (_cache.TryGetValue(cacheKey, out TResponse? cachedResponse))
        {
            _logger.LogDebug(
                "Cache hit for {RequestName} with key {CacheKey}",
                requestName,
                cacheKey
            );
            return cachedResponse!;
        }

        _logger.LogDebug("Cache miss for {RequestName} with key {CacheKey}", requestName, cacheKey);
        var response = await next();

        // Cache the response
        var cacheOptions = GetCacheOptions(request);
        _cache.Set(cacheKey, response, cacheOptions);

        _logger.LogDebug(
            "Cached response for {RequestName} with key {CacheKey}",
            requestName,
            cacheKey
        );
        return response;
    }

    /// <summary>
    /// Generates a cache key for the request.
    /// </summary>
    /// <param name="request">The request</param>
    /// <returns>A cache key</returns>
    private string GenerateCacheKey(TRequest request)
    {
        try
        {
            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            var requestHash = requestJson.GetHashCode();
            return $"{typeof(TRequest).Name}_{requestHash}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to generate cache key for request of type {RequestType}",
                typeof(TRequest).Name
            );
            return $"{typeof(TRequest).Name}_{Guid.NewGuid()}";
        }
    }

    /// <summary>
    /// Gets cache options for the request.
    /// Override this method to customize cache behavior.
    /// </summary>
    /// <param name="request">The request</param>
    /// <returns>Cache options</returns>
    protected virtual MemoryCacheEntryOptions GetCacheOptions(TRequest request)
    {
        return new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(1),
        };
    }
}
