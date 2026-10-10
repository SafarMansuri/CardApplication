using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardApplicationAPI.Data.Entities
{
    [Table("BankAccounts")]
    public class BankAccountEntity
    {
        [Key]
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

        public ICollection<BankTransactionEntity> Transactions { get; set; } = new List<BankTransactionEntity>();
    }
}
