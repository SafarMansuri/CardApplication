using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Repositories.Interfaces
{
    public interface IUserRepository
    {
        // 🔹 Basic CRUD
        Task<PaginatedResult<Users>> GetAllAsync(
            PaginationParams pagination,
            string? search = null,
            string? role = null,
            string? sortBy = null,
            string? sortDir = null);
        Task<UserRoleSummary> GetRoleSummaryAsync();
        Task<Users?> GetByIdAsync(int id);
        Task<Users> CreateAsync(Users model);
        Task<Users> UpdateAsync(Users model);
        Task<bool> DeleteAsync(int id);
        // 🔹 Utility Methods
        Task<bool> ExistsAsync(int id);
        Task<bool> IsUsernameTakenAsync(string username);
        Task<Users?> GetByUsernameAsync(string username);
    }
}
