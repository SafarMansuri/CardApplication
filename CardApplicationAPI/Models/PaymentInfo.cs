namespace CardApplicationAPI.Models
{
    public class PaymentInfo
    {
        public int Id { get; set; }
        public DateTime? Date { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public int SwipeCount { get; set; }
        public decimal? SwipeAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? Profit { get; set; }
        public decimal? TotalProfit { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public string PaymentStatus { get; set; } = "Pending";
        public DateTime? LastPaidAt { get; set; }
        public int? LastPaidByUserId { get; set; }
        public string? LastPaidByUsername { get; set; }
        public bool LimitUsed { get; set; }
        public string CardName { get; set; } = string.Empty;
        public string? Merchant { get; set; }
        public string? SwipePerson { get; set; }
        public string? Remarks { get; set; }
        public DateTime Createdat { get; set; }
        public DateTime Updatedat { get; set; }
    }
}
