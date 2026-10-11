using System.ComponentModel.DataAnnotations;

namespace CardApplicationAPI.Models
{
    public class StockPaymentSettlementRequest
    {
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        [Required, MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? ReferenceNo { get; set; }
        [MaxLength(500)]
        public string? Remarks { get; set; }
    }

    public class StockPaymentResetRequest
    {
        [Required, MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class StockPaymentHistory
    {
        public int Id { get; set; }
        public int StockPaymentInfoId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Remarks { get; set; }
        public int PerformedByUserId { get; set; }
        public string PerformedByUsername { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class StockPartyPaymentSummary
    {
        public string PartyName { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CompletedAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
    }

    public sealed record StockPaymentTotals(decimal PendingPayable, decimal PendingReceivable);
}
