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

        public async Task<Event> GetEventByApiIdAsync(string apiEventId)
        {
            return await _context.Events.FirstOrDefaultAsync(e => e.ApiEventId == apiEventId);
        }

        public async Task AddEvent(Event @event)
        {
            var existingEvent = await GetEventByApiIdAsync(@event.ApiEventId);
            if (existingEvent == null)
            {
                await _context.Events.AddAsync(@event);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Event>> GetApprovedEventsAsync()
        {
            return await _context.Events
                .Where(e => e.IsApproved)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public async Task ApproveEventAsync(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                @event.IsApproved = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Event>> GetPendingEventsAsync()
        {
            return await _context.Events
                .Where(e => !e.IsApproved)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
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

        // Etkinlik id'sine göre bir etkinlik getir
        public async Task<Event> GetEventByIdAsync(int eventId)
        {
            return await _context.Events
                                 .FirstOrDefaultAsync(e => e.Id == eventId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}


