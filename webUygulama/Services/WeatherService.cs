using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using webUygulama.Models;
using Microsoft.Extensions.Logging;

namespace webUygulama.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://api.openweathermap.org/data/2.5/weather";
        private readonly string _forecastUrl = "https://api.openweathermap.org/data/2.5/forecast";
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(HttpClient httpClient, IConfiguration configuration, ILogger<WeatherService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["OpenWeatherMap:ApiKey"] ?? throw new ArgumentNullException("OpenWeatherMap:ApiKey");

            // HttpClient'ı yapılandır
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
        }

        public async Task<WeatherInfo> GetWeatherInfoAsync(string city)
        {
            try
            {
                _logger.LogInformation($"Hava durumu bilgisi alınıyor: {city}");
                
                var requestUrl = $"{_baseUrl}?q={city}&appid={_apiKey}&units=metric&lang=tr";
                _logger.LogInformation($"İstek URL: {requestUrl}");

                using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API hatası: {response.StatusCode}, İçerik: {errorContent}");
                    throw new Exception($"API yanıt vermedi: {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"API yanıtı alındı: {content}");
                
                var weatherData = JsonSerializer.Deserialize<JsonElement>(content);

                var weatherInfo = new WeatherInfo
                {
                    City = city,
                    Temperature = weatherData.GetProperty("main").GetProperty("temp").GetDouble(),
                    Description = weatherData.GetProperty("weather")[0].GetProperty("description").GetString() ?? "",
                    Humidity = weatherData.GetProperty("main").GetProperty("humidity").GetInt32(),
                    WindSpeed = weatherData.GetProperty("wind").GetProperty("speed").GetDouble(),
                    LastUpdated = DateTime.Now
                };

                // Etkinlik için uygunluk kontrolü
                weatherInfo.IsSuitableForEvent = IsSuitableWeatherForEvent(weatherInfo);

                _logger.LogInformation($"Hava durumu bilgisi işlendi: {weatherInfo.Temperature}°C, {weatherInfo.Description}");
                return weatherInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Hava durumu bilgisi alınırken hata: {ex.Message}");
                throw;
            }
        }

        public async Task<EventWeatherInfo> GetEventWeatherForecastAsync(string city, DateTime eventDate)
        {
            try
            {
                _logger.LogInformation($"Etkinlik için hava durumu tahmini alınıyor: {city}, Tarih: {eventDate}");
                
                var requestUrl = $"{_forecastUrl}?q={city}&appid={_apiKey}&units=metric&lang=tr";
                _logger.LogInformation($"Tahmin isteği URL: {requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API hatası: {response.StatusCode}, İçerik: {errorContent}");
                    throw new Exception($"API yanıt vermedi: {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();
                var forecastData = JsonSerializer.Deserialize<JsonElement>(content);
                var forecasts = forecastData.GetProperty("list");

                // En yakın tahmin zamanını bul
                var closestForecast = GetClosestForecast(forecasts, eventDate);

                var eventWeatherInfo = new EventWeatherInfo
                {
                    Temperature = closestForecast.GetProperty("main").GetProperty("temp").GetDouble(),
                    Description = closestForecast.GetProperty("weather")[0].GetProperty("description").GetString() ?? "",
                    WindSpeed = closestForecast.GetProperty("wind").GetProperty("speed").GetDouble()
                };

                // Etkinlik için uygunluk kontrolü
                eventWeatherInfo.IsSuitableForEvent = IsSuitableWeatherForEvent(eventWeatherInfo);

                _logger.LogInformation($"Etkinlik hava durumu tahmini: {eventWeatherInfo.Temperature}°C, {eventWeatherInfo.Description}");
                return eventWeatherInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Hava durumu tahmini alınırken hata: {ex.Message}");
                throw;
            }
        }

        private JsonElement GetClosestForecast(JsonElement forecasts, DateTime targetDate)
        {
            var closestDiff = double.MaxValue;
            var closestForecast = forecasts[0];

            foreach (var forecast in forecasts.EnumerateArray())
            {
                var forecastTime = DateTime.Parse(forecast.GetProperty("dt_txt").GetString() ?? "");
                var diff = Math.Abs((forecastTime - targetDate).TotalHours);

                if (diff < closestDiff)
                {
                    closestDiff = diff;
                    closestForecast = forecast;
                }
            }

            return closestForecast;
        }

        private bool IsSuitableWeatherForEvent(WeatherInfo weather)
        {
            // Sadece şiddetli yağmur ve fırtına durumlarında uygunsuz olarak işaretle
            if (weather.Description.Contains("şiddetli yağmur") || 
                weather.Description.Contains("fırtına") ||
                weather.WindSpeed > 15.0 || // 15 m/s üzeri rüzgar (daha toleranslı)
                weather.Temperature < 0 || // 0°C altı
                weather.Temperature > 40) // 40°C üstü
            {
                return false;
            }
            return true;
        }

        private bool IsSuitableWeatherForEvent(EventWeatherInfo weather)
        {
            // Sadece şiddetli yağmur ve fırtına durumlarında uygunsuz olarak işaretle
            if (weather.Description.Contains("şiddetli yağmur") || 
                weather.Description.Contains("fırtına") ||
                weather.WindSpeed > 15.0 || // 15 m/s üzeri rüzgar (daha toleranslı)
                weather.Temperature < 0 || // 0°C altı
                weather.Temperature > 40) // 40°C üstü
            {
                return false;
            }
            return true;
        }
    }
} 