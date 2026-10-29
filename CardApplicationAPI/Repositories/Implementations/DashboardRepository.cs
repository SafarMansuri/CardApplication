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
            var totalStockItems = await _context.StockItems.CountAsync(cancellationToken);
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

            var bankAccounts = await _context.BankAccounts
                .AsNoTracking()
                .OrderBy(x => x.AccountName)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);

            var perAccountBalances = await _context.BankTransactions
                .AsNoTracking()
                .GroupBy(x => x.AccountId)
                .Select(group => new
                {
                    AccountId = group.Key,
                    Balance = group.Sum(x =>
                        x.TransactionType == "OPENING" || x.TransactionType == "IN" || x.TransactionType == "TRANSFER_IN"
                            ? x.Amount
                            : -x.Amount)
                })
                .ToDictionaryAsync(x => x.AccountId, x => x.Balance, cancellationToken);

            var accounts = bankAccounts
                .Select(account => new DashboardAccountBalance(
                    account.Id,
                    account.BankName,
                    account.AccountName,
                    perAccountBalances.TryGetValue(account.Id, out var bal) ? bal : 0m))
                .ToList();

            var totalBankBalance = bankTransactions == null
                ? 0m
                : bankTransactions.Opening + bankTransactions.TotalIn + bankTransactions.TransferIn
                    - bankTransactions.TotalOut - bankTransactions.TransferOut;
            var totalAvailableCash = cashTransactions == null
                ? 0m
                : cashTransactions.Opening + cashTransactions.TotalIn - cashTransactions.TotalOut;

            var totalCapital = totalBankBalance + totalAvailableCash;
            var totalOwedToYou = pendingStockReceivable;
            var totalYouOwe = pendingStockPayable + totalPendingPartyPayment;
            var netPosition = totalOwedToYou - totalYouOwe;
            var stockAvailablePct = totalStockItems == 0
                ? 0m
                : Math.Round((decimal)availableStockCount / totalStockItems * 100m, 1);

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
                pendingStockReceivable,
                totalCapital,
                totalOwedToYou,
                totalYouOwe,
                netPosition,
                stockAvailablePct,
                accounts);
        }

        public async Task<IReadOnlyList<MonthlyFlowPoint>> GetMonthlyFlowAsync(int months = 12, CancellationToken cancellationToken = default)
        {
            var istToday = ToIst(DateTime.UtcNow).Date;
            var currentMonthStart = new DateTime(istToday.Year, istToday.Month, 1);
            var firstMonthStart = currentMonthStart.AddMonths(-(months - 1));
            var cutoffUtc = firstMonthStart.AddDays(-2);

            var bankRows = await _context.BankTransactions
                .AsNoTracking()
                .Where(x => (x.TransactionType == "IN" || x.TransactionType == "OUT") && x.TransactionDate >= cutoffUtc)
                .Select(x => new { x.TransactionDate, x.TransactionType, x.Amount })
                .ToListAsync(cancellationToken);

            var cashRows = await _context.CashLedger
                .AsNoTracking()
                .Where(x => (x.TransactionType == "IN" || x.TransactionType == "OUT") && x.TransactionDate >= cutoffUtc)
                .Select(x => new { x.TransactionDate, x.TransactionType, x.Amount })
                .ToListAsync(cancellationToken);

            var bankByMonth = bankRows
                .GroupBy(x => ToIst(x.TransactionDate).ToString("yyyy-MM"))
                .ToDictionary(
                    g => g.Key,
                    g => (
                        BankIn: g.Where(r => r.TransactionType == "IN").Sum(r => r.Amount),
                        BankOut: g.Where(r => r.TransactionType == "OUT").Sum(r => r.Amount)));

            var cashByMonth = cashRows
                .GroupBy(x => ToIst(x.TransactionDate).ToString("yyyy-MM"))
                .ToDictionary(
                    g => g.Key,
                    g => (
                        CashIn: g.Where(r => r.TransactionType == "IN").Sum(r => r.Amount),
                        CashOut: g.Where(r => r.TransactionType == "OUT").Sum(r => r.Amount)));

            var points = new List<MonthlyFlowPoint>(months);
            for (var i = 0; i < months; i++)
            {
                var key = firstMonthStart.AddMonths(i).ToString("yyyy-MM");
                bankByMonth.TryGetValue(key, out var bank);
                cashByMonth.TryGetValue(key, out var cash);
                points.Add(new MonthlyFlowPoint(key, bank.BankIn, bank.BankOut, cash.CashIn, cash.CashOut));
            }

            return points;
        }

        public async Task<IReadOnlyList<DailySwipePoint>> GetSwipesDailyAsync(int days = 30, CancellationToken cancellationToken = default)
        {
            var istToday = ToIst(DateTime.UtcNow).Date;
            var startDate = istToday.AddDays(-(days - 1));
            var cutoffUtc = startDate.AddDays(-2);

            var swipes = await _context.CardInfo
                .AsNoTracking()
                .Where(x => (x.Date ?? x.Createdat) >= cutoffUtc)
                .Select(x => new { EffectiveDate = x.Date ?? x.Createdat, x.Deduction, x.Profit })
                .ToListAsync(cancellationToken);

            var swipesByDay = swipes
                .GroupBy(x => ToIst(x.EffectiveDate).ToString("yyyy-MM-dd"))
                .ToDictionary(
                    g => g.Key,
                    g => (
                        Count: g.Count(),
                        Amount: g.Sum(r => r.Deduction ?? 0m),
                        Profit: g.Sum(r => r.Profit ?? 0m)));

            var points = new List<DailySwipePoint>(days);
            for (var i = 0; i < days; i++)
            {
                var key = startDate.AddDays(i).ToString("yyyy-MM-dd");
                swipesByDay.TryGetValue(key, out var day);
                points.Add(new DailySwipePoint(key, day.Count, day.Amount, day.Profit));
            }

            return points;
        }

        public async Task<IReadOnlyList<TopPartyOwedItem>> GetTopPartiesAsync(int limit = 5, CancellationToken cancellationToken = default)
        {
            var cardParties = await _context.PaymentInfo
                .AsNoTracking()
                .Where(x => x.PendingAmount > 0m)
                .Select(x => new TopPartyOwedItem(x.PartyName, "CardParty", x.PendingAmount))
                .ToListAsync(cancellationToken);

            var suppliers = await _context.StockPaymentInfo
                .AsNoTracking()
                .Where(x => x.PaymentType == "Payable" && x.OutstandingAmount > 0m && x.PartyName != null && x.PartyName != "")
                .GroupBy(x => x.PartyName!)
                .Select(g => new TopPartyOwedItem(g.Key, "Supplier", g.Sum(x => x.OutstandingAmount)))
                .ToListAsync(cancellationToken);

            return cardParties
                .Concat(suppliers)
                .OrderByDescending(x => x.Pending)
                .ThenBy(x => x.PartyName)
                .Take(limit)
                .ToList();
        }

        private static DateTime ToIst(DateTime value)
        {
            var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
            return utc.AddMinutes(330);
        }
    }
}