using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webUygulama.Models
{
    public class Event
    {
        public Event()
        {
            Name = string.Empty;
            Location = string.Empty;
            Tickets = new List<Ticket>();
            CreatedAt = DateTime.Now;
        }

        public int Id { get; set; }

        [Required(ErrorMessage = "Etkinlik adı zorunludur")]
        [StringLength(200, ErrorMessage = "Etkinlik adı en fazla 200 karakter olabilir")]
        [Display(Name = "Etkinlik Adı")]
        public string Name { get; set; }

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Tarih zorunludur")]
        [Display(Name = "Tarih ve Saat")]
        [Column(TypeName = "datetime2")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Konum zorunludur")]
        [StringLength(200, ErrorMessage = "Konum en fazla 200 karakter olabilir")]
        [Display(Name = "Konum")]
        public string Location { get; set; }

        [Display(Name = "Fiyat")]
        [Range(0, 100000, ErrorMessage = "Geçerli bir fiyat giriniz")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // Bilet türlerine göre fiyatlar
        [Display(Name = "Normal Bilet Fiyatı")]
        [Range(0, 100000, ErrorMessage = "Geçerli bir fiyat giriniz")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal NormalTicketPrice { get; set; }

        [Display(Name = "VIP Bilet Fiyatı")]
        [Range(0, 100000, ErrorMessage = "Geçerli bir fiyat giriniz")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal VIPTicketPrice { get; set; }

        [Display(Name = "Öğrenci Bilet Fiyatı")]
        [Range(0, 100000, ErrorMessage = "Geçerli bir fiyat giriniz")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal StudentTicketPrice { get; set; }

        [Display(Name = "65 Yaş Üstü Bilet Fiyatı")]
        [Range(0, 100000, ErrorMessage = "Geçerli bir fiyat giriniz")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SeniorTicketPrice { get; set; }

        // Her bilet türü için maksimum satılabilecek bilet sayısı
        [Display(Name = "Normal Bilet Kontenjanı")]
        [Range(0, 1000, ErrorMessage = "Kontenjan 0 ile 1000 arasında olmalıdır")]
        public int NormalTicketCount { get; set; }

        [Display(Name = "VIP Bilet Kontenjanı")]
        [Range(0, 1000, ErrorMessage = "Kontenjan 0 ile 1000 arasında olmalıdır")]
        public int VIPTicketCount { get; set; }

        [Display(Name = "Öğrenci Bilet Kontenjanı")]
        [Range(0, 1000, ErrorMessage = "Kontenjan 0 ile 1000 arasında olmalıdır")]
        public int StudentTicketCount { get; set; }

        [Display(Name = "65 Yaş Üstü Bilet Kontenjanı")]
        [Range(0, 1000, ErrorMessage = "Kontenjan 0 ile 1000 arasında olmalıdır")]
        public int SeniorTicketCount { get; set; }

        [Display(Name = "Görsel URL")]
        [Url(ErrorMessage = "Geçerli bir URL giriniz")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Kategori")]
        public string? Category { get; set; }

        [Display(Name = "Yayında")]
        public bool IsApproved { get; set; }

        public string? ExternalId { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }
    }
} 