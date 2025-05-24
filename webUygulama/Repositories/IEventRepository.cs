using System.Collections.Generic;
using System.Threading.Tasks;
using webUygulama.Models;

namespace webUygulama.Repositories
{
    public interface IEventRepository
    {
        Task<List<Event>> GetAllEventsAsync();
        Task<Event?> GetEventByIdAsync(int id);
        Task<bool> UpdateEventAsync(Event evt);
        Task<List<Event>> GetUnapprovedEventsAsync();
        Task<bool> AddEventsFromApiAsync(List<Event> events);
        Task<bool> DeleteEventAsync(int id);
    }
} 