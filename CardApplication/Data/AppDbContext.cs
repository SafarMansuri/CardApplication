using CardApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace CardApplication.Data
{
    public class AppDbContext : DbContext // Inherit from DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; } = null!;
        //public DbSet<Category> Categories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Additional config or seed data here if desired
        }
    }
}
