using CardApplicationAPI.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CardApplicationAPI.Data.DBContext
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Register all your entities here
        public DbSet<UserEntity> Users { get; set; }
        // Add DbSet<Product> if you have one
        // public DbSet<Product> Products { get; set; }
    }
}
