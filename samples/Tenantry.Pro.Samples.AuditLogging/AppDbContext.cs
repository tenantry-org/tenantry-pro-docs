using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Tenantry.Pro.Samples.AuditLogging;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
}

// Tenant-owned: Tenantry stamps the current tenant on a new product, before audit logging records it.
public class Product : TenantEntity<string>
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }
}
