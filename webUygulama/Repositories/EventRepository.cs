using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using webUygulama.Models;

namespace webUygulama.Services
{
    public class EventRepository
    {
        private readonly AppDbContext _context;

        public EventRepository(AppDbContext context)
        {
            _context = context;
        }

        // Tüm etkinlikleri getir (admin için)
        public async Task<List<Event>> GetAllEventsAsync()
        {
            return await _context.Events
                .OrderByDescending(e => e.StartDateTime)
                .ToListAsync();
        }

        // Onaylı etkinlikleri getir (kullanıcılar için)
        public async Task<List<Event>> GetApprovedEventsAsync()
        {
            return await _context.Events
                .Where(e => e.IsApproved)
                .OrderBy(e => e.StartDateTime)
                .ToListAsync();
        }

        // ID'ye göre etkinlik getir
        public async Task<Event> GetEventByIdAsync(int id)
        {
            return await _context.Events.FindAsync(id);
        }

        // Yeni etkinlik ekle
        public async Task AddEventAsync(Event evt)
        {
            _context.Events.Add(evt);
            await _context.SaveChangesAsync();
        }

        // Aynı ID'ye sahip etkinlik var mı kontrol et
        public async Task<bool> EventExistsAsync(string ticketmasterId)
        {
            return await _context.Events.AnyAsync(e => e.TicketmasterId == ticketmasterId);
        }

        // Etkinliği güncelle (örneğin onaylama)
        public async Task UpdateEventAsync(Event evt)
        {
            _context.Events.Update(evt);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Event>> GetUnapprovedEventsAsync()
        {
            return await _context.Events
                .Where(e => !e.IsApproved)
                .OrderByDescending(e => e.StartDateTime)
                .ToListAsync();
        }

        public async Task AddEventsFromApiAsync(List<Event> apiEvents)
        {
            // Aynı ApiEventId’ye sahip olanları filtrele
            var newEvents = apiEvents
                .Where(e => !_context.Events.Any(db => db.TicketmasterId == e.TicketmasterId))
                .ToList();

            if (!newEvents.Any())
                return;

            // Toplu ekle ve tek seferde kaydet
            await _context.Events.AddRangeAsync(newEvents);
            await _context.SaveChangesAsync();
        }
    }
}


