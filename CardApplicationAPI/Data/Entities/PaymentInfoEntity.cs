using CardApplicationAPI.Models;
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
        public string Name { get; set; } = string.Empty; 

        [Required]
        [MaxLength(100)]
        public string Card { get; set; } = string.Empty;

        public int? SwipeCount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CreditAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Profit { get; set; }

        [NotMapped]
        public decimal TotalAmount => (Amount ?? 0) + (Profit ?? 0) - (CreditAmount ?? 0);

        public string? OutletName { get; set; }
        public string? PaymentStatus { get; set; }

        public DateTime Createdat { get; set; } = DateTime.UtcNow;

        public DateTime Updatedat { get; set; } = DateTime.UtcNow;


     }
}
