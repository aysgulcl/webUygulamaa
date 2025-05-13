using Microsoft.AspNetCore.Mvc;
using webUygulama.Models;
using webUygulama.Repositories;
using webUygulama.Services;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace webUygulama.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserRepository _userRepository;
        private readonly EventRepository _eventRepository;
        private readonly TicketmasterService _ticketmasterService;

        public AdminController(
            UserRepository userRepository,
            EventRepository eventRepository,
            TicketmasterService ticketmasterService)
        {
            _userRepository = userRepository;
            _eventRepository = eventRepository;
            _ticketmasterService = ticketmasterService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            if (email == "aysegulcancatal@gmail.com" && password == "123456")
            {
                HttpContext.Session.SetString("AdminEmail", email);
                return RedirectToAction("Index");
            }

            TempData["Error"] = "Geçersiz giriş bilgileri.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var adminEmail = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(adminEmail))
                return RedirectToAction("Login");

            ViewBag.AdminEmail = adminEmail;

            // Onay bekleyen kullanıcıları alıyoruz
            var pendingUsers = await _userRepository.GetPendingUsersAsync();

            // Onay bekleyen etkinlikleri alıyoruz
            var pendingEvents = await _eventRepository.GetPendingEventsAsync();

            // Kullanıcıları ve etkinlikleri ViewData'ya aktarıyoruz
            ViewData["PendingUsers"] = pendingUsers;
            ViewData["PendingEvents"] = pendingEvents;

            return View();
        }

        // API'den çekilen, henüz onaylanmamış etkinlikleri listeler
        [HttpGet]
        public async Task<IActionResult> PendingEvents()
        {
            var adminEmail = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(adminEmail))
                return RedirectToAction("Login");

            try 
            {
                // API'den etkinlikleri çek
                var apiEvents = await _ticketmasterService.GetAnkaraEventsAsync();
                
                if (apiEvents == null || !apiEvents.Any())
                {
                    TempData["Warning"] = "API'den hiç etkinlik alınamadı.";
                    return View(new List<Event>());
                }

                TempData["Info"] = $"API'den {apiEvents.Count} etkinlik alındı.";

                // API'den gelen etkinlikleri veritabanına kaydet
                await _eventRepository.AddEventsFromApiAsync(apiEvents);

                // Onaylanmamış tüm etkinlikleri getir
                var pendingEvents = await _eventRepository.GetPendingEventsAsync();
                
                if (pendingEvents == null || !pendingEvents.Any())
                {
                    TempData["Warning"] = "Veritabanında onay bekleyen etkinlik bulunamadı.";
                    return View(new List<Event>());
                }

                TempData["Success"] = $"{pendingEvents.Count} onay bekleyen etkinlik bulundu.";
                return View(pendingEvents);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Etkinlikler alınırken bir hata oluştu: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["ErrorDetail"] = $"Detaylı hata: {ex.InnerException.Message}";
                }
                return View(new List<Event>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApproveEvent(int eventId)
        {
            // Onaylamak için etkinliği alıyoruz
            var eventToApprove = await _eventRepository.GetEventByIdAsync(eventId);

            if (eventToApprove == null)
            {
                // Etkinlik bulunamadıysa hata mesajı
                TempData["Error"] = "Etkinlik bulunamadı.";
                return RedirectToAction("PendingEvents");
            }

            // Etkinliği onaylıyoruz
            await _eventRepository.ApproveEventAsync(eventId);

            // Başarı mesajı
            TempData["Success"] = "Etkinlik başarıyla onaylandı.";

            // Yönlendirme
            return RedirectToAction("PendingEvents");
        }

        [HttpPost]
        public async Task<IActionResult> ApproveUser(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Index");
            }

            // Kullanıcıyı onaylıyoruz
            user.IsApproved = true;
            await _userRepository.UpdateUserAsync(user);

            TempData["Success"] = "Kullanıcı başarıyla onaylandı.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null || user.IsAdmin || !user.IsApproved)
                return RedirectToAction("Index");

            if (!user.PasswordChanged)
                return View(user);

            TempData["Info"] = "Şifreniz zaten değiştirilmiş.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(int userId, string newPassword)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return RedirectToAction("Index");

            if (user.IsAdmin)
            {
                TempData["Error"] = "Yönetici hesabı için şifre değiştirilemez.";
                return RedirectToAction("Index");
            }

            user.Password = newPassword;
            user.PasswordChanged = true;
            await _userRepository.UpdateUserAsync(user);

            TempData["Success"] = "Şifre başarıyla güncellendi.";
            return RedirectToAction("Index");
        }
    }
}


