using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace webUygulama.Models
{
    public class Ticket
    {
        public Ticket()
        {
            CustomerName = string.Empty;
            CustomerEmail = string.Empty;
            TicketCode = Guid.NewGuid().ToString("N");
            Status = "Pending";
            PurchaseDate = DateTime.Now;
        }

        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }
        
        [Required]
        public Event Event { get; set; } = null!;

        public string? UserId { get; set; }
        public virtual User? User { get; set; }

        [Required(ErrorMessage = "Müşteri adı zorunludur")]
        [StringLength(100, ErrorMessage = "Müşteri adı en fazla 100 karakter olabilir")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "E-posta adresi zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [StringLength(100, ErrorMessage = "E-posta adresi en fazla 100 karakter olabilir")]
        public string CustomerEmail { get; set; }

        [Required(ErrorMessage = "Bilet adedi zorunludur")]
        [Range(1, 10, ErrorMessage = "Bilet adedi 1 ile 10 arasında olmalıdır")]
        public int Quantity { get; set; }

        [Required]
        [Range(0, 100000, ErrorMessage = "Geçerli bir fiyat giriniz")]
        public decimal TotalPrice { get; set; }

        [Required]
        public string TicketCode { get; set; }

        public DateTime PurchaseDate { get; set; }

        public bool IsCancelled { get; set; }

        [Required]
        public string Status { get; set; }
    }
} 