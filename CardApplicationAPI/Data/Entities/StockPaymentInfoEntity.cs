using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardApplicationAPI.Data.Entities
{
    [Table("StockPaymentInfo")]
    public class StockPaymentInfoEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("NO")]
        public string No { get; set; } = string.Empty;

        [Column("ACCOUNT NAME")]
        public string? AccountName { get; set; }

        [Column("ACCOUNT NO")]
        public string? AccountNo { get; set; }

        [Column("IFSC")]
        public string? Ifsc { get; set; }

        [Column("AMOUNT")]
        public decimal? Amount { get; set; }

        [Column("DATE")]
        public DateTime? Date { get; set; }

        [Column("PARTY NAME")]
        public string? PartyName { get; set; }

        [Column("BANK")]
        public string? Bank { get; set; }

        [Column("IN/OUT")]
        public string? InOut { get; set; }

        [Column("REMARKS")]
        public string? Remarks { get; set; }

        public int StockItemId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(StockItemId))]
        public StockItemEntity? StockItem { get; set; }
    }
}
