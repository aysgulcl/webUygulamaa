using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using webUygulama.Services;
using webUygulama.Repositories;
using webUygulama.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using webUygulama.Data;
using System.Security.Claims;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace webUygulama.Controllers
{
    public class HomeController : Controller
    {
        private readonly TicketmasterService _ticketmasterService;
        private readonly EventRepository _eventRepository;
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly WeatherService _weatherService;
        private readonly UserManager<User> _userManager;

        public HomeController(
            TicketmasterService ticketmasterService, 
            EventRepository eventRepository, 
            ILogger<HomeController> logger, 
            ApplicationDbContext context,
            WeatherService weatherService,
            UserManager<User> userManager)
        {
            _ticketmasterService = ticketmasterService;
            _eventRepository = eventRepository;
            _logger = logger;
            _context = context;
            _weatherService = weatherService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeViewModel();
            
            try
            {
                _logger.LogInformation("Ana sayfa yükleniyor");

                // Hava durumu bilgisini al
                try
                {
                    _logger.LogInformation("Hava durumu bilgisi alınıyor");
                    var weatherInfo = await _weatherService.GetWeatherInfoAsync("Ankara");
                    viewModel.WeatherInfo = weatherInfo;
                    _logger.LogInformation($"Hava durumu bilgisi alındı: {weatherInfo.Temperature}°C, {weatherInfo.Description}");
                }
                catch (Exception weatherEx)
                {
                    _logger.LogError($"Hava durumu bilgisi alınırken hata: {weatherEx.Message}");
                }

                // Aktif duyuruları getir
                try
                {
                    var activeAnnouncements = await _context.Announcements
                        .Where(a => a.IsActive && 
                                  a.PublishDate <= DateTime.Now &&
                                  (!a.EndDate.HasValue || a.EndDate.Value > DateTime.Now))
                        .OrderByDescending(a => a.IsImportant)
                        .ThenByDescending(a => a.PublishDate)
                        .ToListAsync();

                    viewModel.Announcements = activeAnnouncements;
                    _logger.LogInformation($"Aktif duyuru sayısı: {activeAnnouncements.Count}");
                }
                catch (Exception announcementEx)
                {
                    _logger.LogError($"Duyurular alınırken hata: {announcementEx.Message}");
                    viewModel.Announcements = new List<Announcement>();
                }

                // Onaylanmış etkinlikleri getir
                try
                {
                    var approvedEvents = await _eventRepository.GetApprovedEventsAsync();
                    viewModel.Events = approvedEvents;
                    _logger.LogInformation($"Onaylanmış etkinlik sayısı: {approvedEvents.Count}");

                    // Her etkinlik için hava durumu tahminini al
                    foreach (var evt in approvedEvents)
                    {
                        try
                        {
                            var eventWeather = await _weatherService.GetEventWeatherForecastAsync("Ankara", evt.Date);
                            viewModel.EventWeatherInfo[evt.Id] = eventWeather;
                        }
                        catch (Exception eventWeatherEx)
                        {
                            _logger.LogError($"Etkinlik {evt.Id} için hava durumu tahmini alınırken hata: {eventWeatherEx.Message}");
                        }
                    }

                    // Kullanıcı giriş yapmışsa önerilen etkinlikleri getir
                    if (User.Identity.IsAuthenticated)
                    {
                        try
                        {
                            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                            var user = await _userManager.FindByIdAsync(userId);

                            if (user != null)
                            {
                                _logger.LogInformation($"Kullanıcı ilgi alanları: {user.InterestsJson}");
                                var userInterests = user.Interests;

                                if (userInterests.Any())
                                {
                                    _logger.LogInformation($"Onaylanmış etkinlik sayısı: {approvedEvents.Count}");
                                    foreach (var evt in approvedEvents)
                                    {
                                        _logger.LogInformation($"Etkinlik: {evt.Name}, Kategori: {evt.Category}, Tarih: {evt.Date}");
                                    }

                                    // Önerilen etkinlikleri filtrele
                                    var recommendedEvents = approvedEvents
                                        .Where(e => e.Date > DateTime.Now)
                                        .Where(e => !string.IsNullOrEmpty(e.Category) && userInterests.Contains(e.Category))
                                        .OrderBy(e => e.Date)
                                        .Take(3)
                                        .ToList();

                                    viewModel.RecommendedEvents = recommendedEvents;

                                    _logger.LogInformation($"Önerilen etkinlik sayısı: {viewModel.RecommendedEvents.Count}");
                                    foreach (var evt in viewModel.RecommendedEvents)
                                    {
                                        _logger.LogInformation($"Önerilen etkinlik: {evt.Name}, Kategori: {evt.Category}");
                                    }
                                }
                                else
                                {
                                    _logger.LogInformation("Kullanıcının ilgi alanı bulunamadı");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Önerilen etkinlikler alınırken hata: {Message}", ex.Message);
                        }
                    }
                }
                catch (Exception eventEx)
                {
                    _logger.LogError($"Etkinlikler alınırken hata: {eventEx.Message}");
                    viewModel.Events = new List<Event>();
                    viewModel.RecommendedEvents = new List<Event>();
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ana sayfa yüklenirken hata: {ex.Message}");
                return View("Error");
            }
        }

        // Admin paneli için Ticketmaster'dan çekilen veriler (örneğin onaylanmamışlar)
        public async Task<IActionResult> PendingEvents()
        {
            var apiEvents = await _ticketmasterService.GetAnkaraEventsAsync();
            ViewData["Events"] = apiEvents;
            return View();
        }
    }
}


