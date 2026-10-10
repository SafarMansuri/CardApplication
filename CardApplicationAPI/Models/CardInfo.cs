using System.ComponentModel.DataAnnotations;

namespace CardApplicationAPI.Models
{
    public class CardInfo
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string CardName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string CardNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? IMEI { get; set; }

        [Required, StringLength(150)]
        public string PartyName { get; set; } = string.Empty;
        [Range(0, double.MaxValue)]
        public Decimal? Deduction { get; set; }
        [Range(0, double.MaxValue)]
        public Decimal? Profit { get; set; }
        public DateTime? Date { get; set; }
        public string? POS { get; set; }

        [MaxLength(200)]
        public string? Model { get; set; }

        [MaxLength(150)]
        public string? SwipePerson { get; set; }

        [MaxLength(200)]
        public string? Merchant { get; set; }

        public string? Remarks { get; set; }

        [Required]
        public bool limitUsed { get; set; }
        public string? AdditionalInfo { get; set; }

        public string? BankName { get; set; }

        public bool IsSettled { get; set; }

        public DateTime? SettledAt { get; set; }

        public DateTime Createdat { get; set; } = DateTime.UtcNow;

        public DateTime Updatedat { get; set; } = DateTime.UtcNow;
    }
}
