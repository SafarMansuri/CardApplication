using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardApplicationAPI.Data.Entities
{
    [Table("Users")]
    public class UserEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? FullName { get; set; }
        public string? NickName { get; set; }
        public string? AdditionalInfo { get; set; }

        // Role: Master, Admin, or User
        [Required]
        public string Role { get; set; } = "User";

        public DateTime Createdat { get; set; } = DateTime.UtcNow;

        public DateTime Updatedat { get; set; } = DateTime.UtcNow;
    }
}
