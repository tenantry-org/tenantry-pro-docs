using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tenantry.Pro.Samples.SchemaPerTenantSqlServer;

/// <summary>
/// The application's context. It names no schema: with UseTenantry() and pro.UseSchemaPerTenant(...), its default
/// schema is the current tenant's.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Product>().HasKey(p => p.Id);
}

public class Product
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Lets `dotnet ef migrations add` build the context. It has no tenant, so the model, and the migrations generated from
/// it, name no schema: at run time each tenant's schema gets them. The connection string is only a placeholder.
/// </summary>
public sealed class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=app_design;Integrated Security=true;TrustServerCertificate=true")
            .UseTenantry()
            .Options);
}
