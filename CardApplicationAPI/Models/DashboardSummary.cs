namespace CardApplicationAPI.Models
{
    public sealed record DashboardSummary(
        decimal TotalBankBalance,
        decimal TotalAvailableCash,
        int TotalSwipes,
        decimal TotalPartyProfit,
        int TotalStockItems,
        decimal TotalPendingPartyPayment,
        int AvailableStockCount,
        decimal AvailableStockValue,
        decimal PendingStockPayable,
        decimal PendingStockReceivable);
}