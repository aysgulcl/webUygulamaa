using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webUygulama.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Başlık alanı zorunludur.")]
        [Display(Name = "Başlık")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "İçerik alanı zorunludur.")]
        [Display(Name = "İçerik")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Yayın Tarihi")]
        public DateTime PublishDate { get; set; } = DateTime.Now;

        [Display(Name = "Bitiş Tarihi")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Önemli Duyuru")]
        public bool IsImportant { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
} 