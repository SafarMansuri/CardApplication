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
        public DbSet<BankAccountEntity> BankAccounts { get; set; }
        public DbSet<BankTransactionEntity> BankTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<StockPaymentInfoEntity>()
                .HasIndex(x => x.StockItemId)
                .IsUnique();

            modelBuilder.Entity<StockPaymentInfoEntity>()
                .HasOne(x => x.StockItem)
                .WithMany(x => x.PaymentInfo)
                .HasForeignKey(x => x.StockItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaymentInfoEntity>()
                .HasIndex(x => x.PartyName)
                .IsUnique();

            modelBuilder.Entity<PaymentInfoEntity>()
                .HasCheckConstraint("CK_PaymentInfo_PaymentStatus", "[PaymentStatus] IN ('Pending', 'Paid')");

            modelBuilder.Entity<PaymentInfoEntity>()
                .HasCheckConstraint("CK_PaymentInfo_Amounts", "[PaidAmount] >= 0 AND [PendingAmount] >= 0");

            modelBuilder.Entity<UserEntity>()
                .HasIndex(x => x.Username)
                .IsUnique();

            modelBuilder.Entity<CashLedgerEntity>()
                .HasCheckConstraint("CK_CashLedger_TransactionType", "[TransactionType] IN ('OPENING', 'IN', 'OUT')");

            modelBuilder.Entity<CashLedgerEntity>()
                .HasCheckConstraint("CK_CashLedger_Amount", "[Amount] > 0");

            modelBuilder.Entity<BankAccountEntity>()
                .HasIndex(x => x.AccountName)
                .IsUnique();

            modelBuilder.Entity<BankTransactionEntity>()
                .HasOne(x => x.Account)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BankTransactionEntity>()
                .HasOne(x => x.RelatedAccount)
                .WithMany()
                .HasForeignKey(x => x.RelatedAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BankTransactionEntity>()
                .HasCheckConstraint("CK_BankTransaction_TransactionType", "[TransactionType] IN ('OPENING', 'IN', 'OUT', 'TRANSFER_IN', 'TRANSFER_OUT')");

            modelBuilder.Entity<BankTransactionEntity>()
                .HasCheckConstraint("CK_BankTransaction_Amount", "[Amount] > 0");
        }
    }
}
