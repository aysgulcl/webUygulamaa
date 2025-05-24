using webUygulama.Models;

namespace webUygulama.Services
{
    public class TicketPricingService
    {
        // Bilet türüne göre fiyat çarpanları
        private readonly Dictionary<TicketType, decimal> _priceMultipliers = new()
        {
            { TicketType.Normal, 1.0m },      // Normal bilet - baz fiyat
            { TicketType.VIP, 1.5m },         // VIP bilet - %50 daha pahalı
            { TicketType.Student, 0.5m },     // Öğrenci bileti - %50 indirimli
            { TicketType.Senior, 0.7m }       // 65 yaş üstü - %30 indirimli
        };

        public decimal CalculateTicketPrice(decimal basePrice, TicketType ticketType, int quantity)
        {
            // Bilet türüne göre çarpanı al (eğer tanımlı değilse 1.0 kullan)
            decimal multiplier = _priceMultipliers.GetValueOrDefault(ticketType, 1.0m);
            
            // Fiyat hesaplama: Baz fiyat * Çarpan * Miktar
            decimal totalPrice = basePrice * multiplier * quantity;
            
            // Fiyatı yuvarla (2 decimal'e)
            return Math.Round(totalPrice, 2);
        }

        // Bilet türüne göre indirim yüzdesini döndür
        public string GetDiscountText(TicketType ticketType)
        {
            return ticketType switch
            {
                TicketType.VIP => "%50 Ek Ücret",
                TicketType.Student => "%50 İndirim",
                TicketType.Senior => "%30 İndirim",
                _ => "Standart Fiyat"
            };
        }

        // Tüm bilet türlerinin fiyatlarını hesapla
        public Dictionary<TicketType, decimal> GetAllPricesForEvent(decimal basePrice)
        {
            var prices = new Dictionary<TicketType, decimal>();
            
            foreach (var ticketType in Enum.GetValues<TicketType>())
            {
                prices[ticketType] = CalculateTicketPrice(basePrice, ticketType, 1);
            }

            return prices;
        }
    }
} 