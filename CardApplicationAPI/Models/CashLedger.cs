using System.ComponentModel.DataAnnotations;

namespace CardApplicationAPI.Models
{
    public class CashLedger
    {
        public long Id { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        [RegularExpression("^(OPENING|IN|OUT)$", ErrorMessage = "TransactionType must be OPENING, IN, or OUT.")]
        public string TransactionType { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Name { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? CreatedBy { get; set; }
    }
}
