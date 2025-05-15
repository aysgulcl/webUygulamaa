using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using webUygulama.Services;
using webUygulama.Models;

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

        [HttpGet]
        // Kullanıcılar için: Onaylanmış etkinlikleri listele
        public async Task<IActionResult> Index()
        {
            var events = await _eventRepository.GetApprovedEventsAsync();
            return View(events);
        }

        // Admin için: Tüm etkinlikleri listele
        public async Task<IActionResult> AdminList()
        {
            var events = await _eventRepository.GetAllEventsAsync();
            return View(events);
        }

        // Admin: Etkinlikleri Ticketmaster API'den çek ve kaydet
        [HttpPost]
        public async Task<IActionResult> FetchFromApi()
        {
            var apiEvents = await _ticketmasterService.GetAnkaraEventsAsync();

            foreach (var evt in apiEvents)
            {
                if (!await _eventRepository.EventExistsAsync(evt.TicketmasterId))
                {
                    await _eventRepository.AddEventAsync(evt);
                }
            }

            return RedirectToAction("AdminList");
        }

        // Admin: Etkinliği onayla
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var evt = await _eventRepository.GetEventByIdAsync(id);
            if (evt != null)
            {
                evt.IsApproved = true;
                await _eventRepository.UpdateEventAsync(evt);
            }

            return RedirectToAction("AdminList");
        }
    }
}
