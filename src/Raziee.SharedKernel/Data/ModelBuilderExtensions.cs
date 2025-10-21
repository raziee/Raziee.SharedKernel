using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raziee.SharedKernel.Domain.Entities;

namespace Raziee.SharedKernel.Data;

/// <summary>
/// Extension methods for Entity Framework ModelBuilder.
/// Provides common configuration for entities.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configures audit fields for auditable entities.
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    public static void ConfigureAuditableEntities(this ModelBuilder modelBuilder)
    {
        var auditableEntityTypes = modelBuilder
            .Model.GetEntityTypes()
            .Where(e => typeof(AuditableEntity<object>).IsAssignableFrom(e.ClrType))
            .ToList();

        foreach (var entityType in auditableEntityTypes)
        {
            modelBuilder
                .Entity(entityType.ClrType)
                .Property(nameof(AuditableEntity<object>.CreatedAt))
                .IsRequired();

            modelBuilder
                .Entity(entityType.ClrType)
                .Property(nameof(AuditableEntity<object>.CreatedBy))
                .HasMaxLength(256);

            modelBuilder
                .Entity(entityType.ClrType)
                .Property(nameof(AuditableEntity<object>.UpdatedAt));

            modelBuilder
                .Entity(entityType.ClrType)
                .Property(nameof(AuditableEntity<object>.UpdatedBy))
                .HasMaxLength(256);
        }
    }

    /// <summary>
    /// Configures soft delete for entities that implement ISoftDelete.
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    public static void ConfigureSoftDelete(this ModelBuilder modelBuilder)
    {
        var softDeleteEntityTypes = modelBuilder
            .Model.GetEntityTypes()
            .Where(e => typeof(ISoftDelete).IsAssignableFrom(e.ClrType))
            .ToList();

        foreach (var entityType in softDeleteEntityTypes)
        {
            modelBuilder
                .Entity(entityType.ClrType)
                .Property(nameof(ISoftDelete.IsDeleted))
                .IsRequired();

            modelBuilder.Entity(entityType.ClrType).Property(nameof(ISoftDelete.DeletedAt));

            modelBuilder
                .Entity(entityType.ClrType)
                .Property(nameof(ISoftDelete.DeletedBy))
                .HasMaxLength(256);

            // Add global query filter for soft delete
            // Note: Query filter implementation may need to be customized based on specific requirements
            // var entityTypeBuilder = modelBuilder.Entity(entityType.ClrType);
            // entityTypeBuilder.HasQueryFilter(e => !((ISoftDelete)e).IsDeleted);
        }
    }

    /// <summary>
    /// Configures concurrency tokens for entities.
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    public static void ConfigureConcurrencyTokens(this ModelBuilder modelBuilder)
    {
        var entityTypes = modelBuilder
            .Model.GetEntityTypes()
            .Where(e => typeof(Entity<object>).IsAssignableFrom(e.ClrType))
            .ToList();

        foreach (var entityType in entityTypes)
        {
            modelBuilder.Entity(entityType.ClrType).Property("RowVersion").IsRowVersion();
        }
    }

    /// <summary>
    /// Configures decimal precision for decimal properties.
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    /// <param name="precision">The precision</param>
    /// <param name="scale">The scale</param>
    public static void ConfigureDecimalPrecision(
        this ModelBuilder modelBuilder,
        int precision = 18,
        int scale = 2
    )
    {
        var decimalProperties = modelBuilder
            .Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
            .ToList();

        foreach (var property in decimalProperties)
        {
            property.SetPrecision(precision);
            property.SetScale(scale);
        }
    }

    /// <summary>
    /// Configures string properties with default max length.
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    /// <param name="maxLength">The maximum length</param>
    public static void ConfigureStringMaxLength(this ModelBuilder modelBuilder, int maxLength = 256)
    {
        var stringProperties = modelBuilder
            .Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.ClrType == typeof(string))
            .ToList();

        foreach (var property in stringProperties)
        {
            if (property.GetMaxLength() == null)
            {
                property.SetMaxLength(maxLength);
            }
        }
    }
}
