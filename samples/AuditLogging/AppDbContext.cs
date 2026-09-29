using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace AuditLogging;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
}

public class Product
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }
}
