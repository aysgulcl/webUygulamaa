using System.ComponentModel.DataAnnotations;
using static System.Net.Mime.MediaTypeNames;

namespace webUygulama.Models
{
    public class Event
    {
        [Key]
        public int Id { get; set; }  // Auto-increment integer ID

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        public bool IsApproved { get; set; }  // Onay durumu

        [Required]
        public string ApiEventId { get; set; } = string.Empty; // API'den gelen Etkinlik ID'si

        [Required]
        public string Url { get; set; } = string.Empty; // Etkinlik Linki (URL)

        public string ImageUrl { get; set; } = string.Empty; // Etkinlik görseli

        [Required]
        public string City { get; set; } = string.Empty;
    }
}
