using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raziee.SharedKernel.CQRS;
using Raziee.SharedKernel.Data;
using Raziee.SharedKernel.DistributedTransactions;
using Raziee.SharedKernel.Domain.Events;
using Raziee.SharedKernel.Messaging;
using Raziee.SharedKernel.Modules;
using Raziee.SharedKernel.Modules.Events;
using Raziee.SharedKernel.MultiTenancy;
using Raziee.SharedKernel.Repositories;
using Raziee.SharedKernel.ServiceCommunication;

namespace Raziee.SharedKernel.DependencyInjection;

/// <summary>
/// Builder for configuring SharedKernel services.
/// Provides a fluent interface for configuring the SharedKernel library.
/// </summary>
public class SharedKernelBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<Action<IServiceCollection>> _configureActions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedKernelBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection</param>
    public SharedKernelBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Configures the SharedKernel services.
    /// </summary>
    /// <param name="configure">The configuration action</param>
    /// <returns>The builder instance</returns>
    public SharedKernelBuilder Configure(Action<IServiceCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configureActions.Add(configure);
        return this;
    }

    /// <summary>
    /// Adds domain event dispatching.
    /// </summary>
    /// <returns>The builder instance</returns>
    public SharedKernelBuilder AddDomainEvents()
    {
        _services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        return this;
    }

    /// <summary>
    /// Adds CQRS support with MediatR.
    /// </summary>
    /// <returns>The builder instance</returns>
    public SharedKernelBuilder AddCQRS()
    {
        _services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(SharedKernelBuilder).Assembly);
        });

        // Add pipeline behaviors
        _services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        _services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        _services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        _services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

        return this;
    }

    /// <summary>
    /// Adds repository pattern support.
    /// </summary>
    /// <returns>The builder instance</returns>
    public SharedKernelBuilder AddRepositories()
    {
        _services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        _services.AddScoped(typeof(IReadRepository<,>), typeof(EfRepository<,>));
        return this;
    }

    /// <summary>
    /// Adds unit of work support.
    /// </summary>
    /// <returns>The builder instance</returns>
    public SharedKernelBuilder AddUnitOfWork()
    {
        _services.AddScoped<IUnitOfWork, UnitOfWork>();
        return this;
    }

    /// <summary>
    /// Adds multi-tenancy support.
    /// </summary>
    /// <returns>The builder instance</returns>
    public SharedKernelBuilder AddMultiTenancy()
    {
        _services.AddScoped<ITenantProvider, DefaultTenantProvider>();
        return this;
    }

    /// <summary>
    /// Adds module support.
    /// </summary>
    /// <returns>The builder instance</returns>
    public SharedKernelBuilder AddModules()
    {
        _services.AddSingleton<ModuleRegistry>();
        _services.AddScoped<IModuleCommunication, DefaultModuleCommunication>();
        return this;
    }

    /// <summary>
    /// Adds integration events support.
    /// </summary>
    /// <returns>The builder instance</returns>
    public SharedKernelBuilder AddIntegrationEvents()
    {
        _services.AddScoped<IIntegrationEventDispatcher, IntegrationEventDispatcher>();
        _services.AddScoped<IEventBus, InMemoryEventBus>();
        return this;
    }

    /// <summary>
    /// Adds messaging abstractions.
    /// </summary>
    /// <returns>The builder instance</returns>
    public SharedKernelBuilder AddMessaging()
    {
        _services.AddScoped<IMessageBus, DefaultMessageBus>();
        _services.AddScoped<IMessagePublisher, DefaultMessagePublisher>();
        _services.AddScoped<IMessageConsumer, DefaultMessageConsumer>();
        return this;
    }

    /// <summary>
    /// Adds distributed transaction support.
    /// </summary>
    /// <returns>The builder instance</returns>
    public SharedKernelBuilder AddDistributedTransactions()
    {
        _services.AddScoped<ISagaOrchestrator, DefaultSagaOrchestrator>();
        return this;
    }

    /// <summary>
    /// Adds service communication support.
    /// </summary>
    /// <returns>The builder instance</returns>
    public SharedKernelBuilder AddServiceCommunication()
    {
        _services.AddScoped<IServiceDiscovery, DefaultServiceDiscovery>();
        _services.AddScoped<ICircuitBreaker, DefaultCircuitBreaker>();
        _services.AddScoped<IRetryPolicy, RetryPolicy>();
        return this;
    }

    /// <summary>
    /// Adds caching support.
    /// </summary>
    /// <returns>The builder instance</returns>
    public SharedKernelBuilder AddCaching()
    {
        _services.AddMemoryCache();
        return this;
    }

    /// <summary>
    /// Adds logging support.
    /// </summary>
    /// <returns>The builder instance</returns>
    public SharedKernelBuilder AddLogging()
    {
        _services.AddLogging();
        return this;
    }

    /// <summary>
    /// Adds current user service support.
    /// </summary>
    /// <returns>The builder instance</returns>
    public SharedKernelBuilder AddCurrentUserService()
    {
        _services.AddScoped<ICurrentUserService, DefaultCurrentUserService>();
        return this;
    }

    /// <summary>
    /// Builds the SharedKernel configuration.
    /// </summary>
    /// <returns>The service collection</returns>
    public IServiceCollection Build()
    {
        foreach (var configure in _configureActions)
        {
            configure(_services);
        }

        return _services;
    }
}
