using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using webUygulama.Models;
using Microsoft.Extensions.Logging;
using webUygulama.Data;

namespace webUygulama.Repositories
{
    public class EventRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EventRepository> _logger;

        public EventRepository(ApplicationDbContext context, ILogger<EventRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Tüm etkinlikleri getir (admin için)
        public async Task<List<Event>> GetAllEventsAsync()
        {
            try
            {
                _logger.LogInformation("Veritabanından tüm etkinlikler alınıyor");
                
                if (!_context.Database.CanConnect())
                {
                    _logger.LogError("Veritabanına bağlanılamıyor");
                    return new List<Event>();
                }

                var events = await _context.Events
                    .AsNoTracking()
                    .OrderByDescending(e => e.Date)
                    .ToListAsync();
                
                _logger.LogInformation($"Toplam {events.Count} etkinlik bulundu");
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlikler alınırken hata oluştu: {ex.Message}");
                return new List<Event>();
            }
        }

        // Onaylı etkinlikleri getir (kullanıcılar için)
        public async Task<List<Event>> GetApprovedEventsAsync()
        {
            try
            {
                return await _context.Events
                    .Where(e => e.IsApproved && e.Date >= DateTime.Now)
                    .OrderBy(e => e.Date)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Onaylı etkinlikler alınırken hata oluştu: {ex.Message}");
                return new List<Event>();
            }
        }

        // ID'ye göre etkinlik getir
        public async Task<Event?> GetEventByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning($"Geçersiz etkinlik ID: {id}");
                    return null;
                }

                return await _context.Events.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ID {id} olan etkinlik alınırken hata oluştu: {ex.Message}");
                return null;
            }
        }

        // Yeni etkinlik ekle
        public async Task<bool> AddEventAsync(Event evt)
        {
            if (evt == null)
            {
                _logger.LogError("Eklenmeye çalışılan etkinlik null");
                return false;
            }

            try
            {
                evt.CreatedAt = DateTime.Now;
                evt.UpdatedAt = null;
                await _context.Events.AddAsync(evt);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Yeni etkinlik eklendi: {evt.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlik eklenirken hata oluştu: {ex.Message}");
                return false;
            }
        }

        // Aynı ID'ye sahip etkinlik var mı kontrol et
        public async Task<bool> EventExistsAsync(string externalId)
        {
            return await _context.Events.AnyAsync(e => e.ExternalId == externalId);
        }

        // Etkinliği güncelle
        public async Task<bool> UpdateEventAsync(Event evt)
        {
            if (evt == null)
            {
                _logger.LogError("Güncellenmeye çalışılan etkinlik null");
                return false;
            }

            try
            {
                var existingEvent = await _context.Events.FindAsync(evt.Id);
                if (existingEvent == null)
                {
                    _logger.LogError($"ID {evt.Id} olan etkinlik bulunamadı");
                    return false;
                }

                existingEvent.Name = evt.Name;
                existingEvent.Description = evt.Description;
                existingEvent.Date = evt.Date;
                existingEvent.Location = evt.Location;
                existingEvent.Price = evt.Price;
                existingEvent.ImageUrl = evt.ImageUrl;
                existingEvent.Category = evt.Category;
                existingEvent.IsApproved = evt.IsApproved;
                existingEvent.UpdatedAt = DateTime.Now;

                // Bilet fiyatlarını güncelle
                existingEvent.NormalTicketPrice = evt.NormalTicketPrice;
                existingEvent.VIPTicketPrice = evt.VIPTicketPrice;
                existingEvent.StudentTicketPrice = evt.StudentTicketPrice;
                existingEvent.SeniorTicketPrice = evt.SeniorTicketPrice;

                // Bilet kontenjanlarını güncelle
                existingEvent.NormalTicketCount = evt.NormalTicketCount;
                existingEvent.VIPTicketCount = evt.VIPTicketCount;
                existingEvent.StudentTicketCount = evt.StudentTicketCount;
                existingEvent.SeniorTicketCount = evt.SeniorTicketCount;

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Etkinlik güncellendi: {evt.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlik güncellenirken hata oluştu: {ex.Message}");
                return false;
            }
        }

        // Onaylanmamış etkinlikleri getir
        public async Task<List<Event>> GetUnapprovedEventsAsync()
        {
            return await _context.Events
                .Where(e => !e.IsApproved)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        // API'den gelen etkinlikleri ekle
        public async Task<bool> AddEventsFromApiAsync(List<Event> apiEvents)
        {
            if (apiEvents == null || !apiEvents.Any())
            {
                _logger.LogWarning("API'den gelen etkinlik listesi boş");
                return false;
            }

            try
            {
                _logger.LogInformation($"API'den gelen {apiEvents.Count} etkinlik işleniyor");

                foreach (var apiEvent in apiEvents)
                {
                    if (string.IsNullOrEmpty(apiEvent.ExternalId))
                    {
                        _logger.LogWarning($"ExternalId boş olan etkinlik atlanıyor: {apiEvent.Name}");
                        continue;
                    }

                    var existingEvent = await _context.Events
                        .FirstOrDefaultAsync(e => e.ExternalId == apiEvent.ExternalId);

                    if (existingEvent == null)
                    {
                        apiEvent.CreatedAt = DateTime.Now;
                        apiEvent.UpdatedAt = null;
                        apiEvent.IsApproved = false; // API'den gelen etkinlikler onaylanmamış olarak gelsin
                        
                        // Varsayılan bilet fiyatları ve kontenjanları
                        apiEvent.NormalTicketPrice = apiEvent.Price;
                        apiEvent.VIPTicketPrice = apiEvent.Price * 1.5m;
                        apiEvent.StudentTicketPrice = apiEvent.Price * 0.5m;
                        apiEvent.SeniorTicketPrice = apiEvent.Price * 0.7m;
                        
                        apiEvent.NormalTicketCount = 100;
                        apiEvent.VIPTicketCount = 20;
                        apiEvent.StudentTicketCount = 50;
                        apiEvent.SeniorTicketCount = 30;
                        
                        await _context.Events.AddAsync(apiEvent);
                        _logger.LogInformation($"Yeni etkinlik ekleniyor: {apiEvent.Name}");
                    }
                    else
                    {
                        existingEvent.Name = apiEvent.Name;
                        existingEvent.Description = apiEvent.Description;
                        existingEvent.Date = apiEvent.Date;
                        existingEvent.Location = apiEvent.Location;
                        existingEvent.Price = apiEvent.Price;
                        existingEvent.ImageUrl = apiEvent.ImageUrl;
                        existingEvent.UpdatedAt = DateTime.Now;
                        // IsApproved durumunu değiştirme, mevcut durumunu koru
                        
                        // Bilet fiyatlarını güncelle
                        existingEvent.NormalTicketPrice = apiEvent.Price;
                        existingEvent.VIPTicketPrice = apiEvent.Price * 1.5m;
                        existingEvent.StudentTicketPrice = apiEvent.Price * 0.5m;
                        existingEvent.SeniorTicketPrice = apiEvent.Price * 0.7m;
                        
                        _logger.LogInformation($"Mevcut etkinlik güncelleniyor: {existingEvent.Name}");
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Tüm etkinlikler başarıyla kaydedildi");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlikler API'den eklenirken hata oluştu: {ex.Message}");
                return false;
            }
        }

        // Etkinliği sil
        public async Task<bool> DeleteEventAsync(Event evt)
        {
            if (evt == null)
            {
                _logger.LogError("Silinmeye çalışılan etkinlik null");
                return false;
            }

            try
            {
                _context.Events.Remove(evt);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Etkinlik silindi: {evt.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlik silinirken hata oluştu: {ex.Message}");
                return false;
            }
        }
    }
}


