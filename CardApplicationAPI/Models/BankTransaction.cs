using System.ComponentModel.DataAnnotations;

namespace CardApplicationAPI.Models
{
    public class BankTransaction
    {
        public int Id { get; set; }

        [Required]
        public int AccountId { get; set; }

        public int? RelatedAccountId { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        [RegularExpression("^(OPENING|IN|OUT|TRANSFER_IN|TRANSFER_OUT)$", ErrorMessage = "TransactionType must be OPENING, IN, OUT, TRANSFER_IN, or TRANSFER_OUT.")]
        public string TransactionType { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [MaxLength(100)]
        public string? ReferenceNo { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public BankAccount? Account { get; set; }
        public BankAccount? RelatedAccount { get; set; }
    }
}
