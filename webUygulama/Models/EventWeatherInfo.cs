namespace webUygulama.Models
{
    public class EventWeatherInfo
    {
        public double Temperature { get; set; }
        public string Description { get; set; } = string.Empty;
        public double WindSpeed { get; set; }
        public bool IsSuitableForEvent { get; set; }
    }
} 