using Microsoft.AspNetCore.Mvc;
using webUygulama.Services;
using System.Threading.Tasks;

namespace webUygulama.Controllers
{
    public class WeatherController : Controller
    {
        private readonly WeatherService _weatherService;

        public WeatherController(WeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        public async Task<IActionResult> GetAnkaraWeather()
        {
            try
            {
                var weatherInfo = await _weatherService.GetWeatherInfoAsync("Ankara");
                return Json(new { 
                    temperature = weatherInfo.Temperature,
                    description = weatherInfo.Description
                });
            }
            catch
            {
                return Json(new { error = "Hava durumu bilgisi alınamadı" });
            }
        }
    }
} 