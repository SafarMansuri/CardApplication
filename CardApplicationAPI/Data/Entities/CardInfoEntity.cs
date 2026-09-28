using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardApplicationAPI.Data.Entities
{

    [Table("CardInfo")]
    public class CardInfoEntity
    {
    
      
            [Key]
            public int Id { get; set; }

            [Required, MaxLength(100)]
            public string CardName { get; set; } = string.Empty;

            [Required, MaxLength(100)]
            public string CardNumber { get; set; } = string.Empty;

            [MaxLength(100)]
            public string? IMEI { get; set; }
        [Required, MaxLength(150)]
        public string PartyName { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Deduction { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Profit { get; set; }
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
            [Required, MaxLength(50)]

        public string? BankName { get; set; }


        public DateTime Createdat { get; set; } = DateTime.UtcNow;

            public DateTime Updatedat { get; set; } = DateTime.UtcNow;

       

    }
}
