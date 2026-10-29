using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface ICardInfoService
    {
        Task<InternalActionResult<PaginatedResult<CardInfo>>> GetAllAsync(
            PaginationParams pagination,
            string? search = null,
            bool? isSettled = null,
            bool? limitUsed = null,
            string? sortBy = null,
            string? sortDir = null);
        Task<InternalActionResult<CardInfo?>> GetByIdAsync(int id);
        Task<InternalActionResult<CardInfo>> AddAsync(CardInfo user);
        Task<InternalActionResult<bool>> UpdateAsync(CardInfo user);
        Task<InternalActionResult<bool>> DeleteAsync(int id);
    }
}
