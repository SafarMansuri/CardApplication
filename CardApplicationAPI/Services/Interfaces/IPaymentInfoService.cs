using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface IPaymentInfoService
    {
        Task<InternalActionResult<PaginatedResult<PaymentInfo>>> GetAllAsync(PaginationParams pagination, string? partyName);
    }
}
