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
        decimal PendingStockReceivable,
        decimal TotalCapital,
        decimal TotalOwedToYou,
        decimal TotalYouOwe,
        decimal NetPosition,
        decimal StockAvailablePct,
        IReadOnlyList<DashboardAccountBalance> Accounts);

    public sealed record DashboardAccountBalance(
        int AccountId,
        string BankName,
        string AccountName,
        decimal CurrentBalance);

    public sealed record MonthlyFlowPoint(
        string Month,
        decimal BankIn,
        decimal BankOut,
        decimal CashIn,
        decimal CashOut);

    public sealed record DailySwipePoint(
        string Date,
        int Count,
        decimal Amount,
        decimal Profit);

    public sealed record TopPartyOwedItem(
        string PartyName,
        string Type,
        decimal Pending);
}