using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tenantry;

namespace Tenantry.Pro.Samples.TenantLifecycle;

/// <summary>
/// The catalog: one database that lists every tenant. It is not a tenant's database, so it does not use Tenantry's
/// isolation, and it is read before any tenant is current.
/// </summary>
public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<CatalogTenant> Tenants => Set<CatalogTenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var tenant = modelBuilder.Entity<CatalogTenant>();
        tenant.HasKey(t => t.TenantId);
        tenant.Property(t => t.Status).HasConversion<string>().HasMaxLength(16);
    }
}

/// <summary>A tenant, as the catalog records it. Tenantry reads its id and name; the status is the application's.</summary>
public class CatalogTenant : ITenantDescriptor<string>
{
    [MaxLength(64)]
    public required string TenantId { get; init; }

    [MaxLength(256)]
    public required string Name { get; set; }

    public TenantStatus Status { get; set; }
}

public enum TenantStatus
{
    /// <summary>In the catalog, and its database is not ready yet.</summary>
    Provisioning,

    Active,
    Suspended,
}

/// <summary>
/// The tenant store, over the catalog. Every tenant is listed, whatever its status: migrations and health checks
/// find tenants through the store, and an access validator decides which tenants requests may use.
/// </summary>
public sealed class CatalogTenantStore(CatalogDbContext catalog) : ITenantStore<string>
{
    public async ValueTask<ITenantDescriptor<string>?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default) =>
        await catalog.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.TenantId == tenantId, cancellationToken);

    public async ValueTask<IReadOnlyList<ITenantDescriptor<string>>> GetAllTenantsAsync(CancellationToken cancellationToken = default) =>
        await catalog.Tenants.AsNoTracking().OrderBy(t => t.TenantId).ToListAsync<ITenantDescriptor<string>>(cancellationToken);
}

/// <summary>Lets `dotnet ef migrations add --context CatalogDbContext` build the catalog's context.</summary>
public sealed class DesignTimeCatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlServer("Server=localhost;Database=catalog;Integrated Security=true;TrustServerCertificate=true")
            .Options);
}
