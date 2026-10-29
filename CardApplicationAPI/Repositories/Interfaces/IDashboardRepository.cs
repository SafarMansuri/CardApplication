using CardApplicationAPI.Models;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface IDashboardRepository
    {
        Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<MonthlyFlowPoint>> GetMonthlyFlowAsync(int months = 12, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DailySwipePoint>> GetSwipesDailyAsync(int days = 30, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TopPartyOwedItem>> GetTopPartiesAsync(int limit = 5, CancellationToken cancellationToken = default);
    }
}