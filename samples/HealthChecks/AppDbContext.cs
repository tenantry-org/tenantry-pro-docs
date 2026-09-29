using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace HealthChecks;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}

public class Order
{
    public int Id { get; set; }

    [MaxLength(256)]
    public string Description { get; set; } = string.Empty;
}
