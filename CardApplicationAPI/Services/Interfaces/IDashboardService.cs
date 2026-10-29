using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<InternalActionResult<DashboardSummary>> GetSummaryAsync(CancellationToken cancellationToken = default);
        Task<InternalActionResult<IReadOnlyList<MonthlyFlowPoint>>> GetMonthlyFlowAsync(int months = 12, CancellationToken cancellationToken = default);
        Task<InternalActionResult<IReadOnlyList<DailySwipePoint>>> GetSwipesDailyAsync(int days = 30, CancellationToken cancellationToken = default);
        Task<InternalActionResult<IReadOnlyList<TopPartyOwedItem>>> GetTopPartiesAsync(int limit = 5, CancellationToken cancellationToken = default);
    }
}