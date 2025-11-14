using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardApplicationAPI.Data.Entities
{

    [Table("CardInfo")]
    public class CardInfoEntity
    {
    
      
            [Key]
            public int Id { get; set; }

            [Required]
            public string CardName { get; set; } = string.Empty;

            [Required]
            public string CardNumber { get; set; } = string.Empty;
        [Required, MaxLength(150)]

        public string? PartyName { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Deduction { get; set; }
        public DateTime? Date { get; set; }
            public string? POS { get; set; }

            public string? Remarks { get; set; }

            [Required]
            public bool limitUsed { get; set; }
            public string? AdditionalInfo { get; set; }
            
            public DateTime Createdat { get; set; } = DateTime.UtcNow;

            public DateTime Updatedat { get; set; } = DateTime.UtcNow;

       

    }
}
