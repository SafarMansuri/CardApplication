using System.ComponentModel.DataAnnotations;

namespace CardApplicationAPI.Models
{
    public class BankTransferRequest
    {
        [Range(1, int.MaxValue)]
        public int FromAccountId { get; set; }

        [Range(1, int.MaxValue)]
        public int ToAccountId { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [MaxLength(100)]
        public string? ReferenceNo { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }
    }
}