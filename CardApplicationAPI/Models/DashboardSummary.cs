namespace CardApplicationAPI.Models
{
    public sealed record DashboardSummary(
        decimal TotalBankBalance,
        decimal TotalAvailableCash,
        int TotalSwipes,
        decimal TotalPartyProfit,
        int TotalStockItems);
}