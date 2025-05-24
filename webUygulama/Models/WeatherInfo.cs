using System;
using System.ComponentModel.DataAnnotations;

namespace webUygulama.Models
{
    public class WeatherInfo
    {
        public int Id { get; set; }
        public string City { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
        public bool IsSuitableForEvent { get; set; }
        public DateTime LastUpdated { get; set; }
    }
} 