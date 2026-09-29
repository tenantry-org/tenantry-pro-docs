using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Tenantry.Core;

namespace SchemaPerTenant.SqlServer;

/// <summary>
/// Application DbContext that switches schema based on the current tenant.
/// </summary>
public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ITenantContext<string> tenantContext) : DbContext(options)
{
    // The schema name is the tenant id. Real apps may look it up from a store.
    private string Schema => tenantContext.CurrentTenantId ?? "dbo";

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<Product>().HasKey(p => p.Id);
    }
}

public class Product
{
    public int Id { get; set; }
    
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;
}
