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

        public async Task<InternalActionResult<IReadOnlyList<MonthlyFlowPoint>>> GetMonthlyFlowAsync(int months = 12, CancellationToken cancellationToken = default)
        {
            if (months < 1 || months > 60)
                return FactoryResponse<IReadOnlyList<MonthlyFlowPoint>>.CreateFail("Months must be between 1 and 60.", ApplicationActionStatusCodes.ValidationFailed);

            var result = await _repository.GetMonthlyFlowAsync(months, cancellationToken);
            return FactoryResponse<IReadOnlyList<MonthlyFlowPoint>>.CreateSuccess(result, "Monthly flow retrieved successfully.");
        }

        public async Task<InternalActionResult<IReadOnlyList<DailySwipePoint>>> GetSwipesDailyAsync(int days = 30, CancellationToken cancellationToken = default)
        {
            if (days < 1 || days > 365)
                return FactoryResponse<IReadOnlyList<DailySwipePoint>>.CreateFail("Days must be between 1 and 365.", ApplicationActionStatusCodes.ValidationFailed);

            var result = await _repository.GetSwipesDailyAsync(days, cancellationToken);
            return FactoryResponse<IReadOnlyList<DailySwipePoint>>.CreateSuccess(result, "Daily swipes retrieved successfully.");
        }

        public async Task<InternalActionResult<IReadOnlyList<TopPartyOwedItem>>> GetTopPartiesAsync(int limit = 5, CancellationToken cancellationToken = default)
        {
            if (limit < 1 || limit > 50)
                return FactoryResponse<IReadOnlyList<TopPartyOwedItem>>.CreateFail("Limit must be between 1 and 50.", ApplicationActionStatusCodes.ValidationFailed);

            var result = await _repository.GetTopPartiesAsync(limit, cancellationToken);
            return FactoryResponse<IReadOnlyList<TopPartyOwedItem>>.CreateSuccess(result, "Top parties retrieved successfully.");
        }
    }
}