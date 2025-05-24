using System.Collections.Generic;

namespace webUygulama.Models
{
    public class AdminViewModel
    {
        public IList<User> PendingUsers { get; set; } = new List<User>();
        public IList<Event> Events { get; set; } = new List<Event>();

        public Announcement NewAnnouncement { get; set; } = new Announcement();
        public IList<Announcement> AllAnnouncements { get; set; } = new List<Announcement>();
    }
}
