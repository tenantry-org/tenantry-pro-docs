using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Tenantry.Core;

namespace SchemaPerTenant.Npgsql;

/// <summary>
/// Application DbContext that switches PostgreSQL schema based on the current tenant.
/// The tenant context is nullable so the migration-orchestration factory can construct the
/// context with only options (it falls back to the "public" schema in that case).
/// </summary>
public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ITenantContext<string>? tenantContext = null) : DbContext(options)
{
    // Schema name == tenant id. "public" is PostgreSQL's default schema when no tenant is active.
    private string Schema => tenantContext?.CurrentTenantId ?? "public";

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
