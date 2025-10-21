namespace Raziee.SharedKernel.Data;

/// <summary>
/// Service for getting the current user.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user identifier.
    /// </summary>
    /// <returns>The current user identifier</returns>
    string? GetCurrentUser();
}
