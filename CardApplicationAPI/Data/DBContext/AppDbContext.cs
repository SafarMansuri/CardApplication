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
        public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }

        public DbSet<CardInfoEntity> CardInfo { get; set; }
        public DbSet<PaymentInfoEntity> PaymentInfo { get; set; }
        public DbSet<StockItemEntity> StockItems { get; set; }
        public DbSet<StockPaymentInfoEntity> StockPaymentInfo { get; set; }
        public DbSet<CashLedgerEntity> CashLedger { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<StockPaymentInfoEntity>()
                .HasOne(x => x.StockItem)
                .WithMany(x => x.PaymentInfo)
                .HasForeignKey(x => x.StockItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaymentInfoEntity>()
                .HasIndex(x => x.PartyName)
                .IsUnique();

            modelBuilder.Entity<UserEntity>()
                .HasIndex(x => x.Username)
                .IsUnique();

            modelBuilder.Entity<CashLedgerEntity>()
                .HasCheckConstraint("CK_CashLedger_TransactionType", "[TransactionType] IN ('OPENING', 'IN', 'OUT')");

            modelBuilder.Entity<CashLedgerEntity>()
                .HasCheckConstraint("CK_CashLedger_Amount", "[Amount] > 0");
        }
    }
}
