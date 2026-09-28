using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface IPaymentInfoRepository
    {
        Task<PaginatedResult<PaymentInfo>> GetAllAsync(PaginationParams pagination, string? partyName);
    }
}
