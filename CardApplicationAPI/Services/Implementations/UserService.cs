using CardApplicationAPI.Services.Interfaces;
using CardApplicationAPI.Repositories.Interfaces;
using CardApplicationAPI.Models;
namespace CardApplicationAPI.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<Users>> GetAllAsync() => _repository.GetAllAsync();
        public Task<Users?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);
       // public Task<Users?> GetByUsersnameAsync(string Usersname) => _repository.GetByUsernameAsync(Usersname);
        public Task<Users> AddAsync(Users Users) => _repository.CreateAsync(Users);
        public Task UpdateAsync(Users Users) => _repository.UpdateAsync(Users);
        public Task DeleteAsync(int id) => _repository.DeleteAsync(id);
       // public Task<IEnumerable<Users>> GetByRoleAsync(string role) => _repository.GetByRoleAsync(role);
    }
}
