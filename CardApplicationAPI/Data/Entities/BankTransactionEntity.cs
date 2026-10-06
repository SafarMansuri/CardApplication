using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardApplicationAPI.Data.Entities
{
    [Table("BankTransactions")]
    public class BankTransactionEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AccountId { get; set; }

        public int? RelatedAccountId { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string TransactionType { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(100)]
        public string? ReferenceNo { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(AccountId))]
        public BankAccountEntity? Account { get; set; }

        [ForeignKey(nameof(RelatedAccountId))]
        public BankAccountEntity? RelatedAccount { get; set; }
    }
}
