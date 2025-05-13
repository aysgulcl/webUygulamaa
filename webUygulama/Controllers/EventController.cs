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

        public async Task<IActionResult> Index()
        {
            try
            {
                // API'den etkinlikleri al
                var apiEvents = await _ticketmasterService.GetAnkaraEventsAsync();
                
                // Her etkinliği veritabanına kaydet
                foreach (var evt in apiEvents)
                {
                    await _eventRepository.AddEvent(evt);
                }

                // Tüm etkinlikleri getir
                var allEvents = await _eventRepository.GetAllEvents();
                
                if (allEvents.Count == 0)
                {
                    TempData["Warning"] = "Hiç etkinlik bulunamadı.";
                }
                else
                {
                    TempData["Success"] = $"{allEvents.Count} etkinlik listelendi.";
                }

                return View(allEvents);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Bir hata oluştu: {ex.Message}";
                return View(new List<Event>());
            }
        }

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
                TempData["Error"] = $"Etkinlik onaylanırken hata oluştu: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ApprovedEvents()
        {
            try
            {
                var approvedEvents = await _eventRepository.GetApprovedEventsAsync();
                return View(approvedEvents);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Onaylı etkinlikler getirilirken hata oluştu: {ex.Message}";
                return View(new List<Event>());
            }
        }
    }
}
