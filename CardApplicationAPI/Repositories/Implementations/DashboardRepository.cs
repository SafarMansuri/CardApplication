using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Models;
using CardApplicationAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CardApplicationAPI.Repositories.Implementations
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;

        public DashboardRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            var bankTransactions = await _context.BankTransactions
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Opening = group.Where(x => x.TransactionType == "OPENING").Sum(x => (decimal?)x.Amount) ?? 0m,
                    TotalIn = group.Where(x => x.TransactionType == "IN").Sum(x => (decimal?)x.Amount) ?? 0m,
                    TotalOut = group.Where(x => x.TransactionType == "OUT").Sum(x => (decimal?)x.Amount) ?? 0m,
                    TransferIn = group.Where(x => x.TransactionType == "TRANSFER_IN").Sum(x => (decimal?)x.Amount) ?? 0m,
                    TransferOut = group.Where(x => x.TransactionType == "TRANSFER_OUT").Sum(x => (decimal?)x.Amount) ?? 0m
                })
                .FirstOrDefaultAsync(cancellationToken);

            var cashTransactions = await _context.CashLedger
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Opening = group.Where(x => x.TransactionType == "OPENING").Sum(x => (decimal?)x.Amount) ?? 0m,
                    TotalIn = group.Where(x => x.TransactionType == "IN").Sum(x => (decimal?)x.Amount) ?? 0m,
                    TotalOut = group.Where(x => x.TransactionType == "OUT").Sum(x => (decimal?)x.Amount) ?? 0m
                })
                .FirstOrDefaultAsync(cancellationToken);

            var totalSwipes = await _context.CardInfo.CountAsync(cancellationToken);
            var totalPartyProfit = await _context.CardInfo
                .SumAsync(x => (decimal?)x.Profit, cancellationToken) ?? 0m;
            var totalStockItems = await _context.StockItems
                .CountAsync(x => x.InOut != null && x.InOut.ToUpper() == "IN", cancellationToken);
            var totalPendingPartyPayment = await _context.PaymentInfo
                .SumAsync(x => (decimal?)x.PendingAmount, cancellationToken) ?? 0m;
            var availableStockCount = await _context.StockItems
                .CountAsync(x => x.Status != null && x.Status.ToUpper() == "AVAILABLE", cancellationToken);
            var availableStockValue = await _context.StockItems
                .Where(x => x.Status != null && x.Status.ToUpper() == "AVAILABLE")
                .SumAsync(x => x.Amount ?? 0m, cancellationToken);
            var pendingStockPayable = await _context.StockPaymentInfo
                .Where(x => x.PaymentType == "Payable")
                .SumAsync(x => (decimal?)x.OutstandingAmount, cancellationToken) ?? 0m;
            var pendingStockReceivable = await _context.StockPaymentInfo
                .Where(x => x.PaymentType == "Receivable")
                .SumAsync(x => (decimal?)x.OutstandingAmount, cancellationToken) ?? 0m;

            var totalBankBalance = bankTransactions == null
                ? 0m
                : bankTransactions.Opening + bankTransactions.TotalIn + bankTransactions.TransferIn
                    - bankTransactions.TotalOut - bankTransactions.TransferOut;
            var totalAvailableCash = cashTransactions == null
                ? 0m
                : cashTransactions.Opening + cashTransactions.TotalIn - cashTransactions.TotalOut;

            return new DashboardSummary(
                totalBankBalance,
                totalAvailableCash,
                totalSwipes,
                totalPartyProfit,
                totalStockItems,
                totalPendingPartyPayment,
                availableStockCount,
                availableStockValue,
                pendingStockPayable,
                pendingStockReceivable);
        }
    }
}