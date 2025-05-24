using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using webUygulama.Models;
using Microsoft.Extensions.Logging;
using webUygulama.Repositories;
using System.Net.Http.Headers;
using System.Linq;

namespace webUygulama.Services
{
    public class TicketmasterService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TicketmasterService> _logger;
        private readonly EventRepository _eventRepository;

        public TicketmasterService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ILogger<TicketmasterService> logger,
            EventRepository eventRepository)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            
            _apiKey = configuration["TicketmasterApi:ApiKey"] ?? throw new ArgumentNullException("API key bulunamadı");
            
            _logger.LogInformation($"TicketmasterService başlatıldı, API key: {_apiKey}");
        }

        private string MapApiCategoryToSystemCategory(string apiCategory, string apiGenre = "", string apiSubGenre = "")
        {
            // API kategorisini küçük harfe çevirip boşlukları kaldır
            var normalizedCategory = (apiCategory + " " + apiGenre + " " + apiSubGenre)
                .ToLower()
                .Trim();

            // API kategorilerini bizim kategorilerimizle eşleştir
            if (normalizedCategory.Contains("music") || normalizedCategory.Contains("müzik"))
                return "Müzik";
            if (normalizedCategory.Contains("theatre") || normalizedCategory.Contains("tiyatro"))
                return "Tiyatro";
            if (normalizedCategory.Contains("sport") || normalizedCategory.Contains("spor"))
                return "Spor";
            if (normalizedCategory.Contains("film") || normalizedCategory.Contains("movie") || normalizedCategory.Contains("sinema"))
                return "Sinema";
            if (normalizedCategory.Contains("dance") || normalizedCategory.Contains("dans"))
                return "Dans";
            if (normalizedCategory.Contains("art") || normalizedCategory.Contains("exhibition") || normalizedCategory.Contains("sergi"))
                return "Sergi";
            if (normalizedCategory.Contains("festival"))
                return "Festival";
            if (normalizedCategory.Contains("concert") || normalizedCategory.Contains("konser"))
                return "Konser";
            if (normalizedCategory.Contains("comedy") || normalizedCategory.Contains("standup") || normalizedCategory.Contains("stand-up"))
                return "Stand-up";
            if (normalizedCategory.Contains("workshop") || normalizedCategory.Contains("atölye"))
                return "Workshop";

            // Eşleşme bulunamazsa varsayılan kategori
            return "Diğer";
        }

        public async Task<List<Event>> GetEventsAsync()
        {
            return await GetAnkaraEventsAsync();
        }

        public async Task<List<Event>> GetAnkaraEventsAsync()
        {
            try
            {
                _logger.LogInformation("Ankara etkinlikleri alınıyor");
                var startDate = DateTime.Now.ToString("yyyy-MM-dd");

                if (_httpClient.BaseAddress == null)
                {
                    _logger.LogError("HttpClient BaseAddress null");
                    throw new InvalidOperationException("HttpClient BaseAddress is not set");
                }

                var apiUrl = new Uri(_httpClient.BaseAddress, $"discovery/v2/events?apikey={_apiKey}&locale=tr&size=100&city=Ankara&countryCode=TR&startDateTime={startDate}T00:00:00Z");
                
                _logger.LogInformation($"API isteği yapılıyor: {apiUrl}");

                var response = await _httpClient.GetAsync(apiUrl);
                var content = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"API yanıt durumu: {response.StatusCode}");
                _logger.LogInformation($"API yanıt içeriği: {content}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"API hatası: {response.StatusCode}");
                    _logger.LogError($"Hata detayı: {content}");
                    return new List<Event>();
                }

                var json = JObject.Parse(content);
                var events = new List<Event>();

                if (json["_embedded"]?["events"] is JArray eventArray)
                {
                    foreach (var item in eventArray)
                    {
                        try 
                        {
                            var apiCategory = item["classifications"]?[0]?["segment"]?["name"]?.ToString() ?? "";
                            var apiGenre = item["classifications"]?[0]?["genre"]?["name"]?.ToString() ?? "";
                            var apiSubGenre = item["classifications"]?[0]?["subGenre"]?["name"]?.ToString() ?? "";

                            var evt = new Event
                            {
                                ExternalId = item["id"]?.ToString(),
                                Name = item["name"]?.ToString() ?? "İsimsiz Etkinlik",
                                Description = item["description"]?.ToString() ?? item["info"]?.ToString() ?? "Açıklama bulunmuyor",
                                Date = DateTime.Parse(item["dates"]?["start"]?["dateTime"]?.ToString() ?? DateTime.Now.ToString()),
                                Location = item["_embedded"]?["venues"]?[0]?["name"]?.ToString() ?? "Konum belirtilmemiş",
                                ImageUrl = item["images"]?.FirstOrDefault()?["url"]?.ToString() ?? "",
                                Price = item["priceRanges"]?.FirstOrDefault()?["min"]?.Value<decimal>() ?? 0,
                                Category = MapApiCategoryToSystemCategory(apiCategory, apiGenre, apiSubGenre),
                                IsApproved = false,
                                CreatedAt = DateTime.Now
                            };
                            events.Add(evt);
                            _logger.LogInformation($"Etkinlik eklendi: {evt.Name} (Kategori: {evt.Category})");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Etkinlik işlenirken hata: {ex.Message}");
                            continue;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("API yanıtında _embedded.events bulunamadı");
                    _logger.LogWarning($"API yanıt yapısı: {json}");
                }

                _logger.LogInformation($"Toplam {events.Count} etkinlik bulundu");
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetAnkaraEventsAsync'de hata: {ex.Message}");
                _logger.LogError($"Hata detayı: {ex.StackTrace}");
                return new List<Event>();
            }
        }

        public async Task<bool> SaveAnkaraEventsAsync()
        {
            try
            {
                var events = await GetAnkaraEventsAsync();
                _logger.LogInformation($"Kaydedilecek {events.Count} etkinlik alındı");
                
                if (events.Any())
                {
                    await _eventRepository.AddEventsFromApiAsync(events);
                    _logger.LogInformation("Etkinlikler başarıyla veritabanına kaydedildi");
                    return true;
                }
                
                _logger.LogWarning("Kaydedilecek etkinlik bulunamadı");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Etkinlikler kaydedilirken hata: {ex.Message}");
                return false;
            }
        }
    }
} 