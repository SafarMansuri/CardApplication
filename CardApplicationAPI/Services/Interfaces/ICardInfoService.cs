using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface ICardInfoService
    {
        Task<InternalActionResult<PaginatedResult<CardInfo>>> GetAllAsync(PaginationParams pagination);
        Task<InternalActionResult<CardInfo?>> GetByIdAsync(int id);
        Task<InternalActionResult<CardInfo>> AddAsync(CardInfo user);
        Task<InternalActionResult<bool>> UpdateAsync(CardInfo user);
        Task<InternalActionResult<bool>> DeleteAsync(int id);
    }
}
