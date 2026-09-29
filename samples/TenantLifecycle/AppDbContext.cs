using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TenantLifecycle;

/// <summary>
/// With database-per-tenant the connection string already points at the tenant's own database,
/// so no schema switching is needed here.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Setting> Settings => Set<Setting>();

    // One row per key: a seeder that is re-run after a failure can never insert a duplicate.
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Setting>().HasIndex(setting => setting.Key).IsUnique();
}

/// <summary>
/// Lets `dotnet ef migrations add` build the context. Every tenant database has the same schema, so the
/// connection string here is only a placeholder; at runtime each tenant's comes from the resolver.
/// </summary>
public sealed class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=app_design;Integrated Security=true;TrustServerCertificate=true")
            .Options);
}

public class Setting
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Value { get; set; } = string.Empty;
}
