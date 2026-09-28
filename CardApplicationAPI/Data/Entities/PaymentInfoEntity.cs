using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardApplicationAPI.Data.Entities
{
    public class PaymentInfoEntity
    {
        [Key]
        public int Id { get; set; }

        public DateTime? Date { get; set; }

        [Required, MaxLength(150)]
        public string PartyName { get; set; } = string.Empty;

        public int SwipeCount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SwipeAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Profit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalProfit { get; set; }

        public bool LimitUsed { get; set; }

        [Required, MaxLength(100)]
        public string CardName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Merchant { get; set; }

        [MaxLength(150)]
        public string? SwipePerson { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime Createdat { get; set; } = DateTime.UtcNow;

        public DateTime Updatedat { get; set; } = DateTime.UtcNow;
    }
}
