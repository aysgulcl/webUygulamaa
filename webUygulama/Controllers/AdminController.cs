using Microsoft.AspNetCore.Mvc;
using webUygulama.Models;
using webUygulama.Repositories;
using webUygulama.Services;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Filters;
using BCrypt.Net;
using webUygulama.Data;
using Microsoft.AspNetCore.Identity;

namespace webUygulama.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly UserRepository _userRepository;
        private readonly EventRepository _eventRepository;
        private readonly TicketmasterService _ticketmasterService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            UserRepository userRepository,
            EventRepository eventRepository,
            TicketmasterService ticketmasterService,
            ApplicationDbContext context,
            ILogger<AdminController> logger,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _ticketmasterService = ticketmasterService ?? throw new ArgumentNullException(nameof(ticketmasterService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userManager = userManager;
            _roleManager = roleManager;
        }

        private async Task<bool> IsAdmin()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return false;
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return false;
                }

                var isInRole = await _userManager.IsInRoleAsync(user, "Admin");
                return isInRole;
            }
            catch (Exception ex)
            {
                _logger.LogError($"IsAdmin kontrolü sırasında hata: {ex.Message}");
                return false;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!await IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            try
            {
                _logger.LogInformation("Yönetici paneli açıldı");
                
                if (!_context.Database.CanConnect())
                {
                    _logger.LogError("Veritabanına bağlanılamıyor");
                    TempData["Error"] = "Veritabanına bağlanılamıyor.";
                    return View(new AdminViewModel());
                }

                var pendingUsers = await _userRepository.GetPendingUsersAsync();
                var allEvents = await _eventRepository.GetAllEventsAsync();
                var announcements = await _context.Announcements
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation($"Onay bekleyen kullanıcı sayısı: {pendingUsers?.Count() ?? 0}");
                _logger.LogInformation($"Toplam etkinlik sayısı: {allEvents?.Count ?? 0}");
                _logger.LogInformation($"Toplam duyuru sayısı: {announcements?.Count ?? 0}");

                var viewModel = new AdminViewModel
                {
                    PendingUsers = pendingUsers ?? new List<User>(),
                    Events = allEvents ?? new List<Event>(),
                    AllAnnouncements = announcements ?? new List<Announcement>(),
                    NewAnnouncement = new Announcement { PublishDate = DateTime.Now, IsActive = true }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Yönetici paneli yüklenirken hata: {ex.Message}");
                TempData["Error"] = "Bir hata oluştu.";
                return View(new AdminViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> PendingEvents()
        {
            try
            {
                _logger.LogInformation("Accessing PendingEvents page");
                
                // API'den yeni etkinlikleri çek
                _logger.LogInformation("Fetching new events from API");
                var apiEvents = await _ticketmasterService.GetAnkaraEventsAsync();
                _logger.LogInformation($"Retrieved {apiEvents.Count} events from API");

                // Veritabanına kaydet
                await _eventRepository.AddEventsFromApiAsync(apiEvents);
                _logger.LogInformation("Events saved to database");

                // Onaylanmamış etkinlikleri getir
                var pendingEvents = await _eventRepository.GetUnapprovedEventsAsync();
                _logger.LogInformation($"Found {pendingEvents.Count()} pending events");

                return View(pendingEvents ?? new List<Event>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PendingEvents page");
                TempData["Error"] = $"Etkinlikler alınırken bir hata oluştu: {ex.Message}";
                return View(new List<Event>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshEvents()
        {
            try
            {
                _logger.LogInformation("Etkinlikler yenileniyor");
                var apiEvents = await _ticketmasterService.GetAnkaraEventsAsync();
                
                if (apiEvents != null && apiEvents.Any())
                {
                    var result = await _eventRepository.AddEventsFromApiAsync(apiEvents);
                    if (result)
                    {
                        TempData["Success"] = $"{apiEvents.Count} etkinlik başarıyla güncellendi.";
                        _logger.LogInformation($"{apiEvents.Count} etkinlik güncellendi");
                    }
                    else
                    {
                        TempData["Warning"] = "Etkinlikler güncellenirken bazı sorunlar oluştu.";
                        _logger.LogWarning("Etkinlik güncelleme kısmen başarılı");
                    }
                }
                else
                {
                    TempData["Warning"] = "API'den hiç etkinlik alınamadı.";
                    _logger.LogWarning("API'den etkinlik alınamadı");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlikler güncellenirken hata: {ex.Message}");
                TempData["Error"] = "Etkinlikler güncellenirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveEvent(int id)
        {
            try
            {
                var evt = await _eventRepository.GetEventByIdAsync(id);
                if (evt != null)
                {
                evt.IsApproved = true;
                var result = await _eventRepository.UpdateEventAsync(evt);
                if (result)
                {
                        TempData["Success"] = "Etkinlik başarıyla onaylandı ve anasayfada yayınlandı.";
                    _logger.LogInformation($"Etkinlik onaylandı: {evt.Name}");
                }
                else
                {
                    TempData["Error"] = "Etkinlik onaylanırken bir hata oluştu.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlik onaylanırken hata: {ex.Message}");
                TempData["Error"] = "Etkinlik onaylanırken bir hata oluştu.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnapproveEvent(int id)
        {
            try
            {
                var evt = await _eventRepository.GetEventByIdAsync(id);
                if (evt != null)
                {
                    evt.IsApproved = false;
                    var result = await _eventRepository.UpdateEventAsync(evt);
                    if (result)
                    {
                        TempData["Success"] = "Etkinlik anasayfadan kaldırıldı.";
                        _logger.LogInformation($"Etkinlik onayı kaldırıldı: {evt.Name}");
                    }
                    else
                    {
                        TempData["Error"] = "Etkinlik kaldırılırken bir hata oluştu.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlik kaldırılırken hata: {ex.Message}");
                TempData["Error"] = "Etkinlik kaldırılırken bir hata oluştu.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["Error"] = "Geçersiz kullanıcı ID'si.";
                    return RedirectToAction(nameof(Index));
                }

                var user = await _userRepository.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData["Error"] = "Kullanıcı bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                user.IsApproved = true;
                var result = await _userRepository.UpdateUserAsync(user);
                if (result)
                {
                    TempData["Success"] = "Kullanıcı başarıyla onaylandı.";
                    _logger.LogInformation($"Kullanıcı onaylandı: {user.Email}");
                }
                else
                {
                    TempData["Error"] = "Kullanıcı onaylanırken bir hata oluştu.";
                    _logger.LogError($"Kullanıcı onaylanamadı: {user.Email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Kullanıcı onaylanırken hata: {ex.Message}");
                TempData["Error"] = "Kullanıcı onaylanırken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var result = await _userRepository.DeleteUserAsync(id);
                if (result)
                {
                    TempData["Success"] = "Kullanıcı başarıyla silindi.";
                }
                else
                {
                    TempData["Error"] = "Kullanıcı silinirken bir hata oluştu.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Kullanıcı silinirken hata: {ex.Message}");
                TempData["Error"] = "Kullanıcı silinirken bir hata oluştu.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword(string userId)
        {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                return NotFound();
                }
            ViewBag.UserId = userId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string userId, string newPassword)
        {
            try
            {
                await _userRepository.ChangePasswordAsync(userId, newPassword);
                TempData["Success"] = "Şifre başarıyla değiştirildi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Şifre değiştirme hatası: {ex.Message}");
                TempData["Error"] = "Şifre değiştirilirken bir hata oluştu.";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            try
        {
                var evt = await _eventRepository.GetEventByIdAsync(id);
                if (evt == null)
                {
                    TempData["Error"] = "Etkinlik bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }
            return View(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlik düzenleme sayfası açılırken hata: {ex.Message}");
                TempData["Error"] = "Etkinlik düzenleme sayfası açılırken bir hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(Event evt)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(evt);
                }

                var existingEvent = await _eventRepository.GetEventByIdAsync(evt.Id);
                if (existingEvent == null)
                {
                    TempData["Error"] = "Etkinlik bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                // Mevcut etkinliğin değerlerini güncelle
                existingEvent.Name = evt.Name;
                existingEvent.Description = evt.Description;
                existingEvent.Date = evt.Date;
                existingEvent.Location = evt.Location;
                existingEvent.ImageUrl = evt.ImageUrl;
                existingEvent.IsApproved = evt.IsApproved;
                
                // Bilet fiyatları ve kontenjanları
                existingEvent.NormalTicketPrice = evt.NormalTicketPrice;
                existingEvent.VIPTicketPrice = evt.VIPTicketPrice;
                existingEvent.StudentTicketPrice = evt.StudentTicketPrice;
                existingEvent.SeniorTicketPrice = evt.SeniorTicketPrice;
                
                existingEvent.NormalTicketCount = evt.NormalTicketCount;
                existingEvent.VIPTicketCount = evt.VIPTicketCount;
                existingEvent.StudentTicketCount = evt.StudentTicketCount;
                existingEvent.SeniorTicketCount = evt.SeniorTicketCount;

                existingEvent.UpdatedAt = DateTime.Now;

                var result = await _eventRepository.UpdateEventAsync(existingEvent);
                if (result)
                {
                    TempData["Success"] = "Etkinlik başarıyla güncellendi.";
                    _logger.LogInformation($"Etkinlik güncellendi: {existingEvent.Name}");
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Etkinlik güncellenirken bir hata oluştu.";
                    return View(evt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlik güncellenirken hata: {ex.Message}");
                TempData["Error"] = "Etkinlik güncellenirken bir hata oluştu.";
                return View(evt);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            try
            {
                var evt = await _context.Events.FindAsync(id);
                if (evt != null)
                {
                    _context.Events.Remove(evt);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Etkinlik başarıyla silindi.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlik silinirken hata: {ex.Message}");
                TempData["Error"] = "Etkinlik silinirken bir hata oluştu.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement(string Title, string Content, DateTime PublishDate, DateTime? EndDate, bool IsImportant)
        {
            var announcement = new Announcement
            {
                Title = Title,
                Content = Content,
                PublishDate = PublishDate,
                EndDate = EndDate,
                IsImportant = IsImportant,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Announcements.Add(announcement);
                    await _context.SaveChangesAsync();
            
                    TempData["Success"] = "Duyuru başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            try
            {
                var announcement = await _context.Announcements.FindAsync(id);
                if (announcement != null)
                {
                    _context.Announcements.Remove(announcement);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Duyuru başarıyla silindi.";
                    _logger.LogInformation($"Duyuru silindi: {announcement.Title}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Duyuru silinirken hata: {ex.Message}");
                TempData["Error"] = "Duyuru silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAnnouncementStatus(int id)
        {
            try
            {
                var announcement = await _context.Announcements.FindAsync(id);
                if (announcement != null)
                {
                    announcement.IsActive = !announcement.IsActive;
                    announcement.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Duyuru durumu {(announcement.IsActive ? "aktif" : "pasif")} olarak güncellendi.";
                    _logger.LogInformation($"Duyuru durumu değiştirildi: {announcement.Title} - {(announcement.IsActive ? "Aktif" : "Pasif")}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Duyuru durumu değiştirilirken hata: {ex.Message}");
                TempData["Error"] = "Duyuru durumu değiştirilirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveEventApproval(int id)
        {
            try
            {
                var evt = await _eventRepository.GetEventByIdAsync(id);
                if (evt != null)
                {
                    evt.IsApproved = false;
                    var result = await _eventRepository.UpdateEventAsync(evt);
                    if (result)
                    {
                        TempData["Success"] = "Etkinlik anasayfadan kaldırıldı.";
                        _logger.LogInformation($"Etkinlik onayı kaldırıldı: {evt.Name}");
                    }
                    else
                    {
                        TempData["Error"] = "Etkinlik kaldırılırken bir hata oluştu.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlik kaldırılırken hata: {ex.Message}");
                TempData["Error"] = "Etkinlik kaldırılırken bir hata oluştu.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Create()
        {
            var newEvent = new Event
            {
                Date = DateTime.Now,
                IsApproved = false,
                // Varsayılan değerler
                NormalTicketPrice = 100,
                VIPTicketPrice = 150,
                StudentTicketPrice = 50,
                SeniorTicketPrice = 70,
                NormalTicketCount = 100,
                VIPTicketCount = 20,
                StudentTicketCount = 50,
                SeniorTicketCount = 30
            };
            return View("EditEvent", newEvent);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event evt)
        {
            if (!ModelState.IsValid)
            {
                return View("EditEvent", evt);
            }

            try
            {
                evt.CreatedAt = DateTime.UtcNow;
                evt.IsApproved = false; // Yeni etkinlikler varsayılan olarak onaysız

                var result = await _eventRepository.AddEventAsync(evt);
                if (result)
                {
                    TempData["Success"] = "Etkinlik başarıyla oluşturuldu.";
                    _logger.LogInformation($"Yeni etkinlik oluşturuldu: {evt.Name}");
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Etkinlik oluşturulurken bir hata oluştu.";
                    return View("EditEvent", evt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlik oluşturulurken hata: {ex.Message}");
                TempData["Error"] = "Etkinlik oluşturulurken bir hata oluştu.";
                return View("EditEvent", evt);
            }
        }
    }
}



