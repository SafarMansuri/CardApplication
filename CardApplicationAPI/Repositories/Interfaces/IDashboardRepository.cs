using CardApplicationAPI.Models;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface IDashboardRepository
    {
        Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
    }
}