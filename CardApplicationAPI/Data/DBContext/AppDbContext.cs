using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Models;
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

        public DbSet<CardInfoEntity> CardInfo { get; set; }
        public DbSet<PaymentInfoEntity> PaymentInfo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
        }
    }
}
