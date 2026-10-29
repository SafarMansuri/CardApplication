using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface ICardInfoRepository
    {
        Task<PaginatedResult<CardInfo>> GetAllAsync(
            PaginationParams pagination,
            string? search = null,
            bool? isSettled = null,
            bool? limitUsed = null,
            string? sortBy = null,
            string? sortDir = null);
        Task<CardInfo?> GetByIdAsync(int id);
        Task<CardInfo> CreateAsync(CardInfo model);
        Task<CardInfo> UpdateAsync(CardInfo model);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        
    }
}
