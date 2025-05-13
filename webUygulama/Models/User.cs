using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webUygulama.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        public bool IsApproved { get; set; } = false;

        public bool IsAdmin { get; set; } = false;

        public bool PasswordChanged { get; set; } = false;

    }
}
