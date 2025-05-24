using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using webUygulama.Services;
using webUygulama.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using webUygulama.Repositories;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using webUygulama.Data;

namespace webUygulama.Controllers
{
    [Authorize]
    public class EventController : Controller
    {
        private readonly TicketmasterService _ticketmasterService;
        private readonly EventRepository _eventRepository;
        private readonly ILogger<EventController> _logger;
        private readonly ApplicationDbContext _context;

        public EventController(
            TicketmasterService ticketmasterService,
            EventRepository eventRepository,
            ILogger<EventController> logger,
            ApplicationDbContext context)
        {
            _ticketmasterService = ticketmasterService;
            _eventRepository = eventRepository;
            _logger = logger;
            _context = context;
        }

        // Ana sayfa: Onaylanmış etkinlikleri göster
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Accessing Events Index page");
                var events = await _eventRepository.GetApprovedEventsAsync();
                _logger.LogInformation($"Retrieved {events.Count} approved events");
                return View(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Events Index page");
                TempData["Error"] = "Etkinlikler yüklenirken bir hata oluştu.";
                return View(new List<Event>());
            }
        }

        // Admin: Yeni etkinlik oluşturma sayfası
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new Event { Date = DateTime.Now, IsApproved = false });
        }

        // Admin: Yeni etkinlik oluşturma işlemi
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Event evt)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    evt.CreatedAt = DateTime.Now;
                    await _eventRepository.AddEventAsync(evt);
                    TempData["Success"] = "Etkinlik başarıyla oluşturuldu.";
                    return RedirectToAction(nameof(Manage));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlik oluşturulurken hata: {ex.Message}");
                ModelState.AddModelError("", "Etkinlik oluşturulurken bir hata oluştu.");
            }
            return View(evt);
        }

        // Admin: Etkinlik düzenleme sayfası
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var evt = await _eventRepository.GetEventByIdAsync(id);
            if (evt == null)
            {
                return NotFound();
            }
            return View(evt);
        }

        // Admin: Etkinlik düzenleme işlemi
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Event evt)
        {
            if (id != evt.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    evt.UpdatedAt = DateTime.Now;
                    await _eventRepository.UpdateEventAsync(evt);
                    TempData["Success"] = "Etkinlik başarıyla güncellendi.";
                    return RedirectToAction(nameof(Manage));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlik güncellenirken hata: {ex.Message}");
                ModelState.AddModelError("", "Etkinlik güncellenirken bir hata oluştu.");
            }
            return View(evt);
        }

        // Admin: Etkinlik silme işlemi
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var evt = await _eventRepository.GetEventByIdAsync(id);
                if (evt != null)
                {
                    await _eventRepository.DeleteEventAsync(evt);
                    TempData["Success"] = "Etkinlik başarıyla silindi.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlik silinirken hata: {ex.Message}");
                TempData["Error"] = "Etkinlik silinirken bir hata oluştu.";
            }
            return RedirectToAction(nameof(Manage));
        }

        // Admin: Etkinlik yönetim sayfası
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            try
            {
                var events = await _eventRepository.GetAllEventsAsync();
                return View(events);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlik yönetimi sayfası yüklenirken hata: {ex.Message}");
                TempData["Error"] = "Etkinlikler yüklenirken bir hata oluştu.";
                return View(new List<Event>());
            }
        }

        // Admin: Etkinlik onaylama/onay kaldırma
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleApproval(int id)
        {
            try
            {
                var evt = await _eventRepository.GetEventByIdAsync(id);
                if (evt != null)
                {
                    evt.IsApproved = !evt.IsApproved;
                    evt.UpdatedAt = DateTime.Now;
                    await _eventRepository.UpdateEventAsync(evt);
                    TempData["Success"] = evt.IsApproved ? "Etkinlik yayına alındı." : "Etkinlik yayından kaldırıldı.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlik onay durumu değiştirilirken hata: {ex.Message}");
                TempData["Error"] = "Etkinlik durumu değiştirilirken bir hata oluştu.";
            }
            return RedirectToAction(nameof(Manage));
        }

        // Admin: API'den etkinlikleri çekme
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> FetchFromApi()
        {
            try
            {
                var events = await _ticketmasterService.GetAnkaraEventsAsync();
                await _eventRepository.AddEventsFromApiAsync(events);
                TempData["Success"] = "Etkinlikler başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"API'den etkinlikler çekilirken hata: {ex.Message}");
                TempData["Error"] = "Etkinlikler güncellenirken bir hata oluştu.";
            }
            return RedirectToAction(nameof(Manage));
        }
    }
}
