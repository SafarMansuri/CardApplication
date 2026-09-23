using CardApplicationAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace CardApplicationAPI.Helpers
{
    public class PasswordHasherHelper
    {
        private readonly PasswordHasher<Users> _passwordHasher = new PasswordHasher<Users>();

        public string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(new Users(), password);
        }

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(providedPassword))
                return false;

            var result = _passwordHasher.VerifyHashedPassword(new Users(), hashedPassword, providedPassword);
            return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
