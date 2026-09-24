using System.ComponentModel.DataAnnotations;

namespace CardApplicationAPI.Models
{
    public class StockItem
    {
        public int Id { get; set; }

        [Required]
        public string No { get; set; } = string.Empty;

        public DateTime? InDate { get; set; }

        public DateTime? OutDate { get; set; }

        public string? Imei { get; set; }

        public string? Colour { get; set; }

        public string? Model { get; set; }

        public decimal? GstMrp { get; set; }

        public decimal? Amount { get; set; }

        public string? PartyName { get; set; }

        public string? Payment { get; set; }

        public string? InOut { get; set; }

        public string? Status { get; set; }

        public string? SoldTo { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
