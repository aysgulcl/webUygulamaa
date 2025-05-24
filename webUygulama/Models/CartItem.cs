using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace webUygulama.Models
{
    public class CartItem
    {
        public CartItem()
        {
            TicketType = "Normal";
            AddedAt = DateTime.Now;
            UserId = string.Empty;
        }

        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        public Event Event { get; set; } = null!;
        
        [Required]
        public string UserId { get; set; }
        
        public virtual User? User { get; set; }
        
        [Required]
        [Range(1, 10, ErrorMessage = "Bilet adedi 1 ile 10 arasında olmalıdır")]
        public int Quantity { get; set; }
        
        [Required]
        public string TicketType { get; set; } // Normal, VIP, Student, Senior

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100000, ErrorMessage = "Geçerli bir fiyat giriniz")]
        public decimal UnitPrice { get; set; }

        public DateTime AddedAt { get; set; }

        [NotMapped]
        public decimal TotalPrice => Quantity * UnitPrice;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
} 