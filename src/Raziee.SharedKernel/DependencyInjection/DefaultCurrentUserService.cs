using Microsoft.Extensions.Logging;
using Raziee.SharedKernel.Data;

namespace Raziee.SharedKernel.DependencyInjection;

/// <summary>
/// Default implementation of current user service.
/// </summary>
public class DefaultCurrentUserService : ICurrentUserService
{
    private readonly ILogger<DefaultCurrentUserService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCurrentUserService"/> class.
    /// </summary>
    /// <param name="logger">The logger</param>
    public DefaultCurrentUserService(ILogger<DefaultCurrentUserService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current user identifier.
    /// This is a placeholder implementation that returns null.
    /// In a real application, this would get the user from the current context (e.g., HttpContext, ClaimsPrincipal).
    /// </summary>
    /// <returns>The current user identifier or null if not available</returns>
    public string? GetCurrentUser()
    {
        _logger.LogDebug("Getting current user (default implementation)");
        // This is a placeholder implementation
        // In a real application, this would get the user from the current context
        return null;
    }
}
