using CardApplicationAPI.Models;
using CardApplicationAPI.Models.ResponseModel;
using CardApplicationAPI.Repositories.Interfaces;
using CardApplicationAPI.Services.Interfaces;
namespace CardApplicationAPI.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        private static readonly HashSet<string> AllowedSortFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "id", "username", "fullName", "nickName", "phoneNumber", "role", "createdat", "updatedat"
        };

        public async Task<InternalActionResult<PaginatedResult<Users>>> GetAllAsync(
            PaginationParams pagination,
            string? search = null,
            string? role = null,
            string? sortBy = null,
            string? sortDir = null)
        {
            string? normalizedRole = null;
            if (!string.IsNullOrWhiteSpace(role))
            {
                var trimmed = role.Trim();
                normalizedRole = char.ToUpperInvariant(trimmed[0]) + trimmed[1..].ToLowerInvariant();
                if (normalizedRole is not ("Master" or "Admin" or "User"))
                    return FactoryResponse<PaginatedResult<Users>>.CreateFail("Role must be Master, Admin, or User.", ApplicationActionStatusCodes.ValidationFailed);
            }

            sortBy = string.IsNullOrWhiteSpace(sortBy) ? null : sortBy.Trim();
            if (sortBy != null && !AllowedSortFields.Contains(sortBy))
                return FactoryResponse<PaginatedResult<Users>>.CreateFail("Invalid sortBy field.", ApplicationActionStatusCodes.ValidationFailed);

            sortDir = string.IsNullOrWhiteSpace(sortDir) ? null : sortDir.Trim().ToLowerInvariant();
            if (sortDir != null && sortDir != "asc" && sortDir != "desc")
                return FactoryResponse<PaginatedResult<Users>>.CreateFail("SortDir must be asc or desc.", ApplicationActionStatusCodes.ValidationFailed);

            var result = await _repository.GetAllAsync(pagination, search, normalizedRole, sortBy, sortDir);
            result.Items = result.Items.ToList();
            foreach (var user in result.Items)
                user.Password = string.Empty;
            return FactoryResponse<PaginatedResult<Users>>.CreateSuccess(result, "Users retrieved successfully.");
        }

        public async Task<InternalActionResult<UserRoleSummary>> GetSummaryAsync()
        {
            var summary = await _repository.GetRoleSummaryAsync();
            return FactoryResponse<UserRoleSummary>.CreateSuccess(summary, "User summary retrieved successfully.");
        }

        public async Task<InternalActionResult<Users?>> GetByIdAsync(int id)
        {
            var user = await _repository.GetByIdAsync(id);
            if (user == null)
                return FactoryResponse<Users?>.CreateFail("User not found.", ApplicationActionStatusCodes.NotFound);

            user.Password = string.Empty;

            return FactoryResponse<Users?>.CreateSuccess(user, "User retrieved successfully.");
        }

        public async Task<InternalActionResult<Users>> AddAsync(Users user)
        {
            var validation = ValidateUser(user);
            if (validation != null)
                return FactoryResponse<Users>.CreateFail(validation, ApplicationActionStatusCodes.ValidationFailed);
            if (await _repository.IsUsernameTakenAsync(user.Username.Trim()))
                return FactoryResponse<Users>.CreateFail("Username is already in use.", ApplicationActionStatusCodes.Conflict);

            user.Username = user.Username.Trim();
            var created = await _repository.CreateAsync(user);
            created.Password = string.Empty;
            return FactoryResponse<Users>.CreateSuccess(created, "User added successfully.");
        }

        public async Task<InternalActionResult<bool>> UpdateAsync(Users user)
        {
            var validation = ValidateUser(user);
            if (validation != null)
                return FactoryResponse<bool>.CreateFail(validation, ApplicationActionStatusCodes.ValidationFailed);
            if (!await _repository.ExistsAsync(user.Id))
                return FactoryResponse<bool>.CreateFail("User not found.", ApplicationActionStatusCodes.NotFound);
            if (await _repository.IsUsernameTakenAsync(user.Username.Trim()) &&
                !string.Equals((await _repository.GetByIdAsync(user.Id))?.Username, user.Username.Trim(), StringComparison.OrdinalIgnoreCase))
                return FactoryResponse<bool>.CreateFail("Username is already in use.", ApplicationActionStatusCodes.Conflict);

            user.Username = user.Username.Trim();
            await _repository.UpdateAsync(user);
            return FactoryResponse<bool>.CreateSuccess(true, "User updated successfully.");
        }

        public async Task<InternalActionResult<bool>> DeleteAsync(int id)
        {
            if (!await _repository.DeleteAsync(id))
                return FactoryResponse<bool>.CreateFail("User not found.", ApplicationActionStatusCodes.NotFound);
            return FactoryResponse<bool>.CreateSuccess(true, "User deleted successfully.");
        }

        private static string? ValidateUser(Users user)
        {
            if (string.IsNullOrWhiteSpace(user.Username) || user.Username.Trim().Length < 3 || user.Username.Trim().Length > 100)
                return "Username is required and must be between 3 and 100 characters.";
            if (string.IsNullOrWhiteSpace(user.Password) || user.Password.Length < 8 || user.Password.Length > 128)
                return "Password is required and must be between 8 and 128 characters.";
            if (user.Role is not ("Master" or "Admin" or "User"))
                return "Role must be Master, Admin, or User.";
            if (!string.IsNullOrWhiteSpace(user.PhoneNumber) && !System.ComponentModel.DataAnnotations.Validator.TryValidateProperty(
                    user.PhoneNumber,
                    new System.ComponentModel.DataAnnotations.ValidationContext(user) { MemberName = nameof(user.PhoneNumber) },
                    new List<System.ComponentModel.DataAnnotations.ValidationResult>()))
                return "Phone number is invalid.";
            return null;
        }
    }
}
