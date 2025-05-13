using Microsoft.AspNetCore.Mvc;
using webUygulama.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using webUygulama.Services;
using webUygulama.Repositories;
using System;

namespace webUygulama.Controllers
{
    public class EventController : Controller
    {
        private readonly TicketmasterService _ticketmasterService;
        private readonly EventRepository _eventRepository;

        public EventController(TicketmasterService ticketmasterService, EventRepository eventRepository)
        {
            _ticketmasterService = ticketmasterService;
            _eventRepository = eventRepository;
        }

        // Etkinlikleri görüntülemek için
        public async Task<IActionResult> Index()
        {
            try
            {
                // 1. API'den verileri al ve kontrol et
                var apiEvents = await _ticketmasterService.GetAnkaraEventsAsync();
                ViewBag.ApiEventCount = apiEvents?.Count ?? 0;
                
                if (apiEvents == null || apiEvents.Count == 0)
                {
                    TempData["Warning"] = "API'den hiç etkinlik alınamadı!";
                    return View(new List<Event>());
                }

                // 2. Verileri veritabanına kaydet ve log tut
                foreach (var evt in apiEvents)
                {
                    if (string.IsNullOrEmpty(evt.Name))
                    {
                        TempData["Error"] = "Bazı etkinliklerin ismi boş!";
                        continue;
                    }

                    try
                    {
                        await _eventRepository.AddEventsFromApiAsync(new List<Event> { evt });
                        Console.WriteLine($"Etkinlik eklendi: {evt.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Etkinlik eklenirken hata: {evt.Name} - Hata: {ex.Message}");
                    }
                }

                // 3. Veritabanından verileri getir ve kontrol et
                var dbEvents = await _eventRepository.GetAllEvents();
                ViewBag.DbEventCount = dbEvents?.Count ?? 0;

                if (dbEvents == null || dbEvents.Count == 0)
                {
                    TempData["Error"] = "Veritabanında hiç etkinlik bulunamadı!";
                    return View(new List<Event>());
                }

                TempData["Success"] = $"Toplam {dbEvents.Count} etkinlik listelendi.";
                return View(dbEvents);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Bir hata oluştu: {ex.Message}";
                return View(new List<Event>());
            }
        }

        // Yönetici onayı için
        [HttpPost]
        public async Task<IActionResult> ApproveEvent(int id)
        {
            try
            {
                await _eventRepository.ApproveEventAsync(id);
                TempData["Success"] = "Etkinlik başarıyla onaylandı!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Etkinlik onaylanırken hata: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // Onaylı etkinlikleri getir  
        [HttpGet]
        public async Task<IActionResult> ApprovedEvents()
        {
            try
            {
                var approvedEvents = await _eventRepository.GetApprovedEventsAsync();
                ViewBag.ApprovedEventCount = approvedEvents?.Count ?? 0;
                return View(approvedEvents);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Onaylı etkinlikler getirilirken hata: {ex.Message}";
                return View(new List<Event>());
            }
        }
    }
}
