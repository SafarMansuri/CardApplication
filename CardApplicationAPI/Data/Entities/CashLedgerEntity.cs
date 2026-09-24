using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardApplicationAPI.Data.Entities
{
    [Table("CashLedger")]
    public class CashLedgerEntity
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        [MaxLength(10)]
        public string TransactionType { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Name { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? CreatedBy { get; set; }
    }
}
