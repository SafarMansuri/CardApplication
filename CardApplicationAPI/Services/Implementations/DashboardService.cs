using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using CardApplicationAPI.Services.Interfaces;

namespace CardApplicationAPI.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _repository;

        public DashboardService(IDashboardRepository repository)
        {
            _repository = repository;
        }

        public async Task<InternalActionResult<DashboardSummary>> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            var result = await _repository.GetSummaryAsync(cancellationToken);
            return FactoryResponse<DashboardSummary>.CreateSuccess(result, "Dashboard summary retrieved successfully.");
        }
    }
}