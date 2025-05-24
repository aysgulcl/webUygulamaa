using System.ComponentModel.DataAnnotations;

namespace webUygulama.Models
{
    public class CartItemDTO
    {
        public CartItemDTO()
        {
            TicketType = "Normal";
        }

        public int EventId { get; set; }
        
        [Required]
        public string TicketType { get; set; }
        
        [Range(1, int.MaxValue, ErrorMessage = "Lütfen en az 1 bilet seçin")]
        public int Quantity { get; set; }
    }
} 