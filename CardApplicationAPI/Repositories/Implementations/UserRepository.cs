using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Data.Entities;
using CardApplicationAPI.Helpers;
using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
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

        public async Task<PaginatedResult<Users>> GetAllAsync(
            PaginationParams pagination,
            string? search = null,
            string? role = null,
            string? sortBy = null,
            string? sortDir = null)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(x =>
                    x.Username.ToLower().Contains(term) ||
                    (x.FullName != null && x.FullName.ToLower().Contains(term)) ||
                    (x.NickName != null && x.NickName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(x => x.Role == role);

            var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
            query = (sortBy?.ToLowerInvariant()) switch
            {
                "id" => asc ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id),
                "username" => asc ? query.OrderBy(x => x.Username).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Username).ThenByDescending(x => x.Id),
                "fullname" => asc ? query.OrderBy(x => x.FullName).ThenBy(x => x.Id) : query.OrderByDescending(x => x.FullName).ThenByDescending(x => x.Id),
                "nickname" => asc ? query.OrderBy(x => x.NickName).ThenBy(x => x.Id) : query.OrderByDescending(x => x.NickName).ThenByDescending(x => x.Id),
                "phonenumber" => asc ? query.OrderBy(x => x.PhoneNumber).ThenBy(x => x.Id) : query.OrderByDescending(x => x.PhoneNumber).ThenByDescending(x => x.Id),
                "role" => asc ? query.OrderBy(x => x.Role).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Role).ThenByDescending(x => x.Id),
                "createdat" => asc ? query.OrderBy(x => x.Createdat).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Createdat).ThenByDescending(x => x.Id),
                "updatedat" => asc ? query.OrderBy(x => x.Updatedat).ThenBy(x => x.Id) : query.OrderByDescending(x => x.Updatedat).ThenByDescending(x => x.Id),
                _ => sortDir == "desc" ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id)
            };

            var result = await query.ToPaginatedResultAsync(pagination);
            return new PaginatedResult<Users> { Items = result.Items.Select(MapToModel), Pagination = result.Pagination };
        }

        public async Task<UserRoleSummary> GetRoleSummaryAsync()
        {
            var counts = await _context.Users
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Masters = g.Count(x => x.Role == "Master"),
                    Admins = g.Count(x => x.Role == "Admin"),
                    Users = g.Count(x => x.Role == "User")
                })
                .FirstOrDefaultAsync();

            return counts == null
                ? new UserRoleSummary(0, 0, 0, 0)
                : new UserRoleSummary(counts.Total, counts.Masters, counts.Admins, counts.Users);
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
