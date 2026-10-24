using System.ComponentModel.DataAnnotations;

namespace CardApplicationAPI.Models
{
    public class StockPaymentInfo
    {
        public int Id { get; set; }

        [Required]
        public string No { get; set; } = string.Empty;

        public string? AccountName { get; set; }

        public string? AccountNo { get; set; }

        public string? Ifsc { get; set; }

        public decimal? Amount { get; set; }

        public DateTime? Date { get; set; }

        public string? PartyName { get; set; }

        public string? Bank { get; set; }

        public string? InOut { get; set; }

        public string PaymentType { get; set; } = "Payable";

        public string PaymentStatus { get; set; } = "Pending";

        public decimal OutstandingAmount { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int? CompletedByUserId { get; set; }

        public string? CompletedByUsername { get; set; }

        public string? Remarks { get; set; }

        public int StockItemId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
