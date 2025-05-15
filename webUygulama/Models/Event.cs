using System;

namespace webUygulama.Models
{
    public class Event
    {
        public int Id { get; set; } // Veritabanı kimliği

        public string TicketmasterId { get; set; } // API'den gelen ID

        public string Name { get; set; }

        public string Url { get; set; }

        public string ImageUrl { get; set; }

        public DateTime StartDateTime { get; set; }

        public string Genre { get; set; }

        public bool IsApproved { get; set; } = false;
    }
}
