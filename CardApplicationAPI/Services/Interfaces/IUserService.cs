using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface IUserService
    {
        Task<InternalActionResult<PaginatedResult<Users>>> GetAllAsync(
            PaginationParams pagination,
            string? search = null,
            string? role = null,
            string? sortBy = null,
            string? sortDir = null);
        Task<InternalActionResult<UserRoleSummary>> GetSummaryAsync();
        Task<InternalActionResult<Users?>> GetByIdAsync(int id);
        Task<InternalActionResult<Users>> AddAsync(Users user);
        Task<InternalActionResult<bool>> UpdateAsync(Users user);
        Task<InternalActionResult<bool>> DeleteAsync(int id);
    }
}
