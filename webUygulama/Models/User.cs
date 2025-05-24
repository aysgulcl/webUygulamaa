using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text.Json;

namespace webUygulama.Models
{
    public class User : IdentityUser
    {
        [PersonalData]
        public bool IsAdmin { get; set; }

        [PersonalData]
        public bool IsApproved { get; set; }

        [PersonalData]
        public bool PasswordChanged { get; set; }

        private string _interestsJson = "[]";

        [Column("Interests")]
        public string InterestsJson
        {
            get => _interestsJson;
            set => _interestsJson = value ?? "[]";
        }

        [NotMapped]
        public List<string> Interests
        {
            get => JsonSerializer.Deserialize<List<string>>(InterestsJson) ?? new List<string>();
            set => InterestsJson = JsonSerializer.Serialize(value ?? new List<string>());
        }

        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
