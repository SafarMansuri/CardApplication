using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using CardApplicationAPI.Helpers;
using CardApplicationAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CardApplicationAPI.Authentication
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IUserRepository _userRepository;
        private readonly PasswordHasherHelper _passwordHasher;

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IUserRepository userRepository,
            PasswordHasherHelper passwordHasher) : base(options, logger, encoder, clock)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.Fail("Missing Authorization header");

            var authHeader = Request.Headers["Authorization"].ToString();
            if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                return AuthenticateResult.Fail("Invalid Authorization header");

            try
            {
                var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
                var decodedBytes = Convert.FromBase64String(encodedCredentials);
                var credentials = Encoding.UTF8.GetString(decodedBytes);
                var parts = credentials.Split(':', 2);

                if (parts.Length != 2)
                    return AuthenticateResult.Fail("Invalid credentials format");

                var username = parts[0];
                var password = parts[1];

                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                    return AuthenticateResult.Fail("Invalid username or password");

                // Accept hashed password first; fall back to plain text for legacy data during migration.
                var valid = _passwordHasher.VerifyPassword(user.Password, password)
                            || user.Password == password;

                if (!valid)
                    return AuthenticateResult.Fail("Invalid username or password");

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return AuthenticateResult.Success(ticket);
            }
            catch
            {
                return AuthenticateResult.Fail("Invalid Authorization header");
            }
        }
    }
}
