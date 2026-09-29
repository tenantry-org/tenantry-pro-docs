using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DatabasePerTenant.MySql;

/// <summary>
/// Application DbContext. With database-per-tenant the connection string already
/// points at the correct database — no schema switching required here.
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
            .UseMySQL("Server=localhost;Database=myapp_design;User Id=root;Password=placeholder")
            .Options);
}
