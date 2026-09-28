using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CardApplicationAPI.Models
{
    public class Users
    {
        public int Id { get; set; }

        [Required, StringLength(100, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(128, MinimumLength = 8)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
        public string Password { get; set; } = string.Empty;

        [Phone, StringLength(30)]
        public string? PhoneNumber { get; set; }
        [StringLength(150)]
        public string? FullName { get; set; }
        [StringLength(100)]
        public string? NickName { get; set; }
        [StringLength(1000)]
        public string? AdditionalInfo { get; set; }

        // Role: Master, Admin, or User
        [Required, RegularExpression("^(Master|Admin|User)$", ErrorMessage = "Role must be Master, Admin, or User.")]
        public string Role { get; set; } = "User";

        public DateTime Createdat { get; set; } = DateTime.UtcNow;

        public DateTime Updatedat { get; set; } = DateTime.UtcNow;


    }
}
