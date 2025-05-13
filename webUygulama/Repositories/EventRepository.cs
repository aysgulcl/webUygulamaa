using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using webUygulama.Models;

namespace webUygulama.Repositories
{
    public class EventRepository
    {
        private readonly AppDbContext _context;

        public EventRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Event>> GetAllEvents()
        {
            return await _context.Events.ToListAsync();
        }

        public async Task<List<Event>> GetApprovedEvents()
        {
            return await _context.Events.Where(e => e.IsApproved).ToListAsync();
        }

        public async Task AddEvent(Event @event)
        {
            await _context.Events.AddAsync(@event);
            await _context.SaveChangesAsync();
        }

        public async Task AddEvents(List<Event> events)
        {
            await _context.Events.AddRangeAsync(events);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateEvent(Event @event)
        {
            _context.Events.Update(@event);
            await _context.SaveChangesAsync();
        }

        // Onaylı etkinlikleri getir
        public async Task<List<Event>> GetApprovedEventsAsync()
        {
            return await _context.Events
                .Where(e => e.IsApproved)  // Sadece onaylı etkinlikler
                .OrderBy(e => e.Date)      // Tarihe göre sırala
                .ToListAsync();            // Listele
        }

        // API'den gelen etkinlikleri ekle
        public async Task AddEventsFromApiAsync(List<Event> events)
        {
            foreach (var eventItem in events)
            {
                var existingEvent = await _context.Events
                    .FirstOrDefaultAsync(e => e.ApiEventId == eventItem.ApiEventId);

                if (existingEvent == null)
                {
                    _context.Events.Add(eventItem); // Etkinlik yoksa ekle
                }
                // Eğer etkinlik zaten varsa, ekleme yapılmaz
            }

            await _context.SaveChangesAsync();
        }

        // Onay bekleyen etkinlikleri getir
        public async Task<List<Event>> GetPendingEventsAsync()
        {
            return await _context.Events
                                 .Where(e => !e.IsApproved) // Onaylanmamış etkinlikler
                                 .ToListAsync();
        }

        // Etkinlik id'sine göre bir etkinlik getir
        public async Task<Event> GetEventByIdAsync(int eventId)
        {
            return await _context.Events
                                 .FirstOrDefaultAsync(e => e.Id == eventId);
        }

        
        public async Task ApproveEventAsync(int eventId)  // string yerine int kullanıyoruz
        {
            var eventToApprove = await _context.Events
                                            .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventToApprove != null)
            {
                eventToApprove.IsApproved = true; // Onayla
                await _context.SaveChangesAsync(); // Değişiklikleri kaydet
            }
        }

       
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}


