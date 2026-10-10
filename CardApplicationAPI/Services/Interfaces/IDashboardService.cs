using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<InternalActionResult<DashboardSummary>> GetSummaryAsync(CancellationToken cancellationToken = default);
    }
}