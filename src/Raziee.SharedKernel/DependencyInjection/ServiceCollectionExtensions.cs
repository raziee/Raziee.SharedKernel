using Microsoft.Extensions.DependencyInjection;
using Raziee.SharedKernel.CQRS;
using Raziee.SharedKernel.Data;
using Raziee.SharedKernel.Domain.Events;
using Raziee.SharedKernel.Modules;
using Raziee.SharedKernel.Modules.Events;
using Raziee.SharedKernel.MultiTenancy;
using Raziee.SharedKernel.Repositories;

namespace Raziee.SharedKernel.DependencyInjection;

/// <summary>
/// Extension methods for configuring SharedKernel services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SharedKernel services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">The configuration action</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSharedKernel(
        this IServiceCollection services,
        Action<SharedKernelBuilder>? configure = null
    )
    {
        var builder = new SharedKernelBuilder(services);
        configure?.Invoke(builder);
        return builder.Build();
    }

    /// <summary>
    /// Adds SharedKernel services with default configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSharedKernel(this IServiceCollection services)
    {
        return services.AddSharedKernel(builder =>
        {
            builder
                .AddDomainEvents()
                .AddCQRS()
                .AddRepositories()
                .AddUnitOfWork()
                .AddMultiTenancy()
                .AddModules()
                .AddIntegrationEvents()
                .AddMessaging()
                .AddDistributedTransactions()
                .AddServiceCommunication()
                .AddCaching()
                .AddLogging()
                .AddCurrentUserService();
        });
    }
}
