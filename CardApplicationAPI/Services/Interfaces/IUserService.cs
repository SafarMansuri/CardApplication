using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface IUserService
    {
        Task<InternalActionResult<IEnumerable<Users>>> GetAllAsync();
        Task<InternalActionResult<Users?>> GetByIdAsync(int id);
        Task<InternalActionResult<Users>> AddAsync(Users user);
        Task<InternalActionResult<bool>> UpdateAsync(Users user);
        Task<InternalActionResult<bool>> DeleteAsync(int id);
    }
}
