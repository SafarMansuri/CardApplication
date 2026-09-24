using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardApplicationAPI.Data.Entities
{
    [Table("StockItems")]
    public class StockItemEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("NO")]
        public string No { get; set; } = string.Empty;

        [Column("IN DATE")]
        public DateTime? InDate { get; set; }

        [Column("OUT DATE")]
        public DateTime? OutDate { get; set; }

        [Column("IMEI")]
        public string? Imei { get; set; }

        [Column("COLOUR")]
        public string? Colour { get; set; }

        [Column("MODEL")]
        public string? Model { get; set; }

        [Column("GST/MRP")]
        public decimal? GstMrp { get; set; }

        [Column("AMOUNT")]
        public decimal? Amount { get; set; }

        [Column("PARTY NAME")]
        public string? PartyName { get; set; }

        [Column("PAYMENT")]
        public string? Payment { get; set; }

        [Column("IN/OUT")]
        public string? InOut { get; set; }

        [Column("STATUS")]
        public string? Status { get; set; }

        [Column("SOLD TO")]
        public string? SoldTo { get; set; }

        [Column("REMARKS")]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<StockPaymentInfoEntity> PaymentInfo { get; set; } = new List<StockPaymentInfoEntity>();
    }
}
