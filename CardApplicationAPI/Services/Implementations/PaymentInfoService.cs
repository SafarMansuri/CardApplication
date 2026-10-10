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

        public async Task<InternalActionResult<PaginatedResult<PaymentInfo>>> GetAllAsync(PaginationParams pagination, string? partyName, string? status)
        {
            var normalizedStatus = status?.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedStatus))
            {
                normalizedStatus = char.ToUpperInvariant(normalizedStatus[0]) + normalizedStatus[1..].ToLowerInvariant();
                if (normalizedStatus != "Pending" && normalizedStatus != "Paid")
                    return FactoryResponse<PaginatedResult<PaymentInfo>>.CreateFail("Status must be Pending or Paid.", ApplicationActionStatusCodes.ValidationFailed);
            }

            var result = await _repository.GetAllAsync(pagination, partyName, normalizedStatus);
            return FactoryResponse<PaginatedResult<PaymentInfo>>.CreateSuccess(result, "Payment summaries retrieved successfully.");
        }

        public async Task<InternalActionResult<PaymentInfo>> MarkPaidAsync(int id, int paidByUserId, string paidByUsername)
        {
            if (id <= 0 || paidByUserId <= 0 || string.IsNullOrWhiteSpace(paidByUsername))
                return FactoryResponse<PaymentInfo>.CreateFail("Payment summary or authenticated user is invalid.", ApplicationActionStatusCodes.ValidationFailed);

            try
            {
                var result = await _repository.MarkPaidAsync(id, paidByUserId, paidByUsername);
                if (result == null)
                    return FactoryResponse<PaymentInfo>.CreateFail("Payment summary not found.", ApplicationActionStatusCodes.NotFound);
                return FactoryResponse<PaymentInfo>.CreateSuccess(result, "Party payment marked as paid successfully.");
            }
            catch (InvalidOperationException ex)
            {
                return FactoryResponse<PaymentInfo>.CreateFail(ex.Message, ApplicationActionStatusCodes.Conflict);
            }
        }
    }
}
