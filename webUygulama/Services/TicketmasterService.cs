using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.Json;
using System.Collections.Generic;
using webUygulama.Models;

namespace webUygulama.Services
{
    public class TicketmasterService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public TicketmasterService(IConfiguration configuration, HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            // appsettings.json içindeki TicketmasterAPI:ApiKey kısmından alıyoruz
            _apiKey = configuration["TicketmasterAPI:ApiKey"];

            if (string.IsNullOrEmpty(_apiKey))
                throw new ArgumentException("Ticketmaster API anahtarı bulunamadı. Lütfen appsettings.json dosyasını kontrol edin.");
        }

        public async Task<List<Event>> GetAnkaraEventsAsync()
        {
            string url = $"https://app.ticketmaster.com/discovery/v2/events.json?" +
                         $"apikey={_apiKey}&city=Ankara&countryCode=TR&size=10";

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var jsonDocument = JsonDocument.Parse(content);
                    var events = new List<Event>();

                    // _embedded.events array'ini kontrol et
                    if (jsonDocument.RootElement.TryGetProperty("_embedded", out var embedded) &&
                        embedded.TryGetProperty("events", out var eventsArray))
                    {
                        foreach (var eventElement in eventsArray.EnumerateArray())
                        {
                            var evt = new Event
                            {
                                // Id'yi kaldırıyoruz çünkü artık auto-increment olacak
                                ApiEventId = eventElement.GetProperty("id").GetString(),
                                Name = eventElement.GetProperty("name").GetString(),
                                Description = eventElement.TryGetProperty("description", out var desc) ?
                                    desc.GetString() : "Açıklama bulunmuyor",
                                Date = eventElement.TryGetProperty("dates", out var dates) &&
                                    dates.TryGetProperty("start", out var start) &&
                                    start.TryGetProperty("dateTime", out var dateTime) ?
                                    DateTime.Parse(dateTime.GetString()) : DateTime.Now,
                                Url = eventElement.GetProperty("url").GetString(),
                                ImageUrl = eventElement.TryGetProperty("images", out var images) &&
                                    images.EnumerateArray().Any() ?
                                    images.EnumerateArray().First().GetProperty("url").GetString() : "",
                                City = "Ankara",
                                IsApproved = false // Varsayılan olarak onaysız
                            };
                            events.Add(evt);
                        }
                    }
                    return events;
                }
                else
                {
                    throw new Exception($"API Hatası: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"İstek sırasında bir hata oluştu: {ex.Message}");
            }
        }
    }
}
