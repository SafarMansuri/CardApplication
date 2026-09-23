using CardApplicationAPI.Models;

namespace CardApplicationAPI.Services.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(Users user);
    }
}
