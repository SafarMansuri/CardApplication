using CardApplicationAPI.Helpers;
using CardApplicationAPI.Models;
using CardApplicationAPI.Repositories.Interfaces;
using CardApplicationAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardApplicationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly PasswordHasherHelper _passwordHasher;

        public AuthController(
            IUserRepository userRepository,
            IJwtTokenService jwtTokenService,
            IRefreshTokenService refreshTokenService,
            PasswordHasherHelper passwordHasher)
        {
            _userRepository = userRepository;
            _jwtTokenService = jwtTokenService;
            _refreshTokenService = refreshTokenService;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null)
                return Unauthorized(new { message = "Invalid username or password" });

            var valid = _passwordHasher.VerifyPassword(user.Password, request.Password)
                        || user.Password == request.Password;

            if (!valid)
                return Unauthorized(new { message = "Invalid username or password" });

            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(user.Id);

            return Ok(new
            {
                accessToken,
                refreshToken = refreshToken.Token,
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Role,
                    user.FullName
                }
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var result = await _refreshTokenService.RotateAsync(request.RefreshToken);

            if (result.error != null)
                return Unauthorized(new { message = result.error });

            return Ok(new
            {
                accessToken = result.accessToken,
                refreshToken = result.refreshToken
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
        {
            var revoked = await _refreshTokenService.RevokeAsync(request.RefreshToken);

            if (!revoked)
                return BadRequest(new { message = "Invalid refresh token." });

            return Ok(new { message = "Logged out successfully." });
        }

        [HttpPost("seed")]
        public async Task<IActionResult> Seed()
        {
            var users = await _userRepository.GetAllAsync();
            if (users.Any())
                return BadRequest(new { message = "Users already exist. Seed is only allowed on an empty user table." });

            var masterUser = new Users
            {
                Username = "master",
                Password = _passwordHasher.HashPassword("Master@123"),
                Role = "Master",
                FullName = "System Master",
                PhoneNumber = "",
                NickName = "",
                AdditionalInfo = ""
            };

            await _userRepository.CreateAsync(masterUser);

            return Ok(new
            {
                message = "Master user created successfully.",
                username = masterUser.Username,
                defaultPassword = "Master@123"
            });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
