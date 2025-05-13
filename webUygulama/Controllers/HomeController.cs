using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using webUygulama.Services;
using webUygulama.Repositories;

namespace webUygulama.Controllers
{
    public class HomeController : Controller
    {
        private readonly TicketmasterService _ticketmasterService;
        private readonly EventRepository _eventRepository;

        public HomeController(TicketmasterService ticketmasterService, EventRepository eventRepository)
        {
            _ticketmasterService = ticketmasterService;
            _eventRepository = eventRepository;
        }

        public async Task<IActionResult> Index()
        {
            // Veritaban�ndan sadece onayl� etkinlikleri al�yoruz
            var approvedEvents = await _eventRepository.GetApprovedEventsAsync();

            // View'a g�nder
            return View(approvedEvents);
        }

        // Admin paneli i�in Ticketmaster'dan �ekilen veriler (�rne�in onaylanmam��lar)
        public async Task<IActionResult> PendingEvents()
        {
            var apiEvents = await _ticketmasterService.GetAnkaraEventsAsync(); // "Ankara" parametresine gerek yok
            ViewData["Events"] = apiEvents;
            return View();
        }
    }
}


