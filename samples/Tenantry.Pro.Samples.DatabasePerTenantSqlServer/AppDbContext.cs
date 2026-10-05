using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tenantry.Pro.Samples.DatabasePerTenantSqlServer;

/// <summary>
/// Application DbContext. With a database per tenant, the connection string already
/// names the tenant's database, so the context sets no schema.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Order>().HasKey(o => o.Id);
}

public class Order
{
    public int Id { get; set; }

    [MaxLength(256)]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Lets `dotnet ef migrations add` build the context. Every tenant database has the same schema, so the
/// connection string here is only a placeholder; at runtime each tenant's comes from the resolver.
/// </summary>
public sealed class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=myapp_design;Integrated Security=true;TrustServerCertificate=true")
            .Options);
}
