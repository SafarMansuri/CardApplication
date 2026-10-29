using System.ComponentModel.DataAnnotations;

namespace CardApplicationAPI.Models
{
    public class BankAccount
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string AccountName { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string BankName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? AccountNo { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public sealed record BankAccountStatusSummary(int Total, int Active, int Inactive);
}
