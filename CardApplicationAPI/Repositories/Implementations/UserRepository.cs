using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Helpers;
using CardApplicationAPI.Models;
using CardApplicationAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace CardApplicationAPI.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasherHelper _passwordHasher;

        public UserRepository(AppDbContext context, PasswordHasherHelper passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        private string HashIfPlain(string password)
        {
            if (string.IsNullOrEmpty(password))
                return password;

            // ASP.NET Identity V3 hashes start with "AQAAAA". Avoid re-hashing an existing hash.
            if (password.StartsWith("AQAAAA"))
                return password;

            return _passwordHasher.HashPassword(password);
        }

        public async Task<IEnumerable<Users>> GetAllAsync()
        {
            var entities = await _context.Users.ToListAsync();
            return entities.Select(MapToModel);
        }

        public async Task<Users?> GetByIdAsync(int id)
        {
            var entity = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            return entity != null ? MapToModel(entity) : null;
        }

        public async Task<Users> CreateAsync(Users model)
        {
            var entity = MapToEntity(model);
            entity.Password = HashIfPlain(model.Password);
            entity.Createdat = DateTime.UtcNow;
            entity.Updatedat = DateTime.UtcNow;

            _context.Users.Add(entity);
            await _context.SaveChangesAsync();

            return MapToModel(entity);
        }

        public async Task<Users> UpdateAsync(Users model)
        {
            var existing = await _context.Users.FirstOrDefaultAsync(u => u.Id == model.Id);
            if (existing == null)
                throw new InvalidOperationException($"Users with ID {model.Id} not found");

            existing.Username = model.Username;
            existing.Password = HashIfPlain(model.Password);
            existing.PhoneNumber = model.PhoneNumber;
            existing.FullName = model.FullName;
            existing.NickName = model.NickName;
            existing.AdditionalInfo = model.AdditionalInfo;
            existing.Role = model.Role;
            existing.Updatedat = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToModel(existing);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (entity == null) return false;

            _context.Users.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Users.AnyAsync(u => u.Id == id);
        }

        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<Users?> GetByUsernameAsync(string username)
        {
            var entity = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            return entity != null ? MapToModel(entity) : null;
        }

        private static Users MapToModel(UserEntity entity)
        {
            return new Users
            {
                Id = entity.Id,
                Username = entity.Username,
                Password = entity.Password,
                PhoneNumber = entity.PhoneNumber,
                FullName = entity.FullName,
                NickName = entity.NickName,
                AdditionalInfo = entity.AdditionalInfo,
                Role = entity.Role,
                Createdat = entity.Createdat,
                Updatedat = entity.Updatedat
            };
        }

        private static UserEntity MapToEntity(Users model)
        {
            return new UserEntity
            {
                Id = model.Id,
                Username = model.Username,
                Password = model.Password,
                PhoneNumber = model.PhoneNumber,
                FullName = model.FullName,
                NickName = model.NickName,
                AdditionalInfo = model.AdditionalInfo,
                Role = model.Role,
                Createdat = model.Createdat,
                Updatedat = model.Updatedat
            };

        }
    }
}
