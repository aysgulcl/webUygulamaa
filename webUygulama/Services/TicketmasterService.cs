using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.Json;
using System.Collections.Generic;
using webUygulama.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace webUygulama.Services
{
    public class TicketmasterService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;

        public TicketmasterService(IConfiguration configuration, HttpClient httpClient, AppDbContext context)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _context = context ?? throw new ArgumentNullException(nameof(context));

            _apiKey = configuration["TicketmasterAPI:ApiKey"];
            if (string.IsNullOrEmpty(_apiKey))
                throw new ArgumentException("Ticketmaster API anahtarı bulunamadı. Lütfen appsettings.json dosyasını kontrol edin.");
        }

        public async Task<List<Event>> GetAnkaraEventsAsync()
        {
            string url = $"https://app.ticketmaster.com/discovery/v2/events.json?apikey={_apiKey}&city=Ankara&countryCode=TR&size=10";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API hatası: {response.StatusCode}");
            }

            string content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(content);
            var events = new List<Event>();

            if (jsonDocument.RootElement.TryGetProperty("_embedded", out var embedded) &&
                embedded.TryGetProperty("events", out var eventsArray))
            {
                foreach (var eventElement in eventsArray.EnumerateArray())
                {
                    var evt = new Event
                    {
                        TicketmasterId = eventElement.GetProperty("id").GetString(),
                        Name = eventElement.GetProperty("name").GetString(),
                        Url = eventElement.GetProperty("url").GetString(),

                        ImageUrl = eventElement.TryGetProperty("images", out var images) &&
                                   images.ValueKind == JsonValueKind.Array &&
                                   images.EnumerateArray().Any()
                                   ? images.EnumerateArray().First().GetProperty("url").GetString()
                                   : "",

                        StartDateTime = eventElement.TryGetProperty("dates", out var dates) &&
                                        dates.TryGetProperty("start", out var start) &&
                                        start.TryGetProperty("dateTime", out var dateTime)
                                        ? DateTime.Parse(dateTime.GetString())
                                        : DateTime.MinValue,

                        Genre = eventElement.TryGetProperty("classifications", out var classifications) &&
                                classifications.ValueKind == JsonValueKind.Array &&
                                classifications.EnumerateArray().First().TryGetProperty("genre", out var genre) &&
                                genre.TryGetProperty("name", out var genreName)
                                ? genreName.GetString()
                                : "",

                        IsApproved = false
                    };

                    events.Add(evt);
                }
            }

            return events;
        }

        public async Task SaveAnkaraEventsAsync()
        {
            var events = await GetAnkaraEventsAsync();

            foreach (var eventItem in events)
            {
                var existingEvent = await _context.Events
                    .FirstOrDefaultAsync(e => e.TicketmasterId == eventItem.TicketmasterId);

                if (existingEvent == null)
                {
                    _context.Events.Add(eventItem);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}


