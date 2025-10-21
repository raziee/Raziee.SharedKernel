using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Raziee.SharedKernel.CQRS;
using Raziee.SharedKernel.Data;
using Raziee.SharedKernel.Domain.Entities;
using Raziee.SharedKernel.Domain.Events;
using Raziee.SharedKernel.DependencyInjection;
using Raziee.SharedKernel.Modules;
using Raziee.SharedKernel.Modules.Events;
using Raziee.SharedKernel.MultiTenancy;
using Raziee.SharedKernel.Repositories;
using Raziee.SharedKernel.ServiceCommunication;
using Xunit;

namespace Raziee.SharedKernel.Tests.DependencyInjection;

public class TestEntity : Entity<Guid>
{
    public TestEntity(Guid id) : base(id) { }
}

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSharedKernel_ShouldRegisterAllServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSharedKernel();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Domain Events
        serviceProvider.GetService<IDomainEventDispatcher>().Should().NotBeNull();
        
        // CQRS
        serviceProvider.GetService<IPipelineBehavior<MediatR.IRequest<object>, object>>().Should().NotBeNull();
        
        // Note: Repository and UnitOfWork services require DbContext registration
        // which is typically done in the application startup, not in the core library
        
        // Multi-tenancy
        serviceProvider.GetService<ITenantProvider>().Should().NotBeNull();
        
        // Modules
        serviceProvider.GetService<ModuleRegistry>().Should().NotBeNull();
        serviceProvider.GetService<IModuleCommunication>().Should().NotBeNull();
        
        // Integration Events
        serviceProvider.GetService<IIntegrationEventDispatcher>().Should().NotBeNull();
        serviceProvider.GetService<IEventBus>().Should().NotBeNull();
        
        // Service Communication
        serviceProvider.GetService<IServiceDiscovery>().Should().NotBeNull();
        serviceProvider.GetService<ICircuitBreaker>().Should().NotBeNull();
        serviceProvider.GetService<IRetryPolicy>().Should().NotBeNull();
    }

    [Fact]
    public void AddSharedKernel_WithConfiguration_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurationApplied = false;

        // Act
        services.AddSharedKernel(builder =>
        {
            builder.Configure(services =>
            {
                configurationApplied = true;
            });
        });

        // Assert
        configurationApplied.Should().BeTrue();
    }
}
