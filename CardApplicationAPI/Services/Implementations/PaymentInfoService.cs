using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using CardApplicationAPI.Services.Interfaces;

namespace CardApplicationAPI.Services.Implementations
{
    public class PaymentInfoService : IPaymentInfoService
    {
        private readonly IPaymentInfoRepository _repository;

        public PaymentInfoService(IPaymentInfoRepository repository)
        {
            _repository = repository;
        }

        public async Task<InternalActionResult<PaginatedResult<PaymentInfo>>> GetAllAsync(PaginationParams pagination, string? partyName)
        {
            var result = await _repository.GetAllAsync(pagination, partyName);
            return FactoryResponse<PaginatedResult<PaymentInfo>>.CreateSuccess(result, "Payment summaries retrieved successfully.");
        }
    }
}
