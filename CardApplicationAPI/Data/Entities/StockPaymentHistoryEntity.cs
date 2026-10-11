using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardApplicationAPI.Data.Entities
{
    [Table("StockPaymentHistory")]
    public class StockPaymentHistoryEntity
    {
        [Key]
        public int Id { get; set; }
        public int StockPaymentInfoId { get; set; }
        [Required, MaxLength(20)]
        public string EventType { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        [MaxLength(50)]
        public string? PaymentMethod { get; set; }
        [MaxLength(100)]
        public string? ReferenceNo { get; set; }
        [MaxLength(500)]
        public string? Remarks { get; set; }
        public int PerformedByUserId { get; set; }
        [Required, MaxLength(100)]
        public string PerformedByUsername { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [ForeignKey(nameof(StockPaymentInfoId))]
        public StockPaymentInfoEntity? StockPaymentInfo { get; set; }
    }
}
