using System.Collections.Generic;

namespace webUygulama.Models
{
    public class HomeViewModel
    {
        public List<Announcement> Announcements { get; set; } = new List<Announcement>();
        public List<Event> Events { get; set; } = new List<Event>();
        public List<Event> RecommendedEvents { get; set; } = new List<Event>();
        public WeatherInfo WeatherInfo { get; set; } = new WeatherInfo();
        public Dictionary<int, EventWeatherInfo> EventWeatherInfo { get; set; } = new Dictionary<int, EventWeatherInfo>();
    }
} 