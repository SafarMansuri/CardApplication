using System.ComponentModel.DataAnnotations;

namespace CardApplicationAPI.Models
{
    public class Users
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? FullName { get; set; }
        public string? NickName { get; set; }
        public string? AdditionalInfo { get; set; }

        // Role: Master, Admin, or User
        [Required]
        public string Role { get; set; } = "User";


    }
}
