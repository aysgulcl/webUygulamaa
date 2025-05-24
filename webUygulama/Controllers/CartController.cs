using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webUygulama.Models;
using webUygulama.Data;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;

namespace webUygulama.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger;
        private readonly UserManager<User> _userManager;

        public CartController(ApplicationDbContext context, ILogger<CartController> logger, UserManager<User> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var cartItems = await _context.CartItems
                .Include(c => c.Event)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Kullanıcı bulunamadı" });

            var @event = await _context.Events.FindAsync(model.EventId);
            if (@event == null) return Json(new { success = false, message = "Etkinlik bulunamadı" });

            // Bilet tipine göre kontrol ve fiyat belirleme
            int availableCount = 0;
            decimal price = 0;

            if (model.TicketType == null)
            {
                return Json(new { success = false, message = "Bilet tipi belirtilmedi" });
            }

            switch (model.TicketType.ToLower())
            {
                case "normal":
                    availableCount = @event.NormalTicketCount;
                    price = @event.NormalTicketPrice;
                    break;
                case "student":
                    availableCount = @event.StudentTicketCount;
                    price = @event.StudentTicketPrice;
                    break;
                case "senior":
                    availableCount = @event.SeniorTicketCount;
                    price = @event.SeniorTicketPrice;
                    break;
                default:
                    return Json(new { success = false, message = "Geçersiz bilet tipi" });
            }

            if (availableCount < model.Quantity)
            {
                return Json(new { success = false, message = "Yeterli bilet yok" });
            }

            // Sepete ekle
            var cartItem = new CartItem
            {
                EventId = model.EventId,
                UserId = user.Id,
                TicketType = model.TicketType,
                Quantity = model.Quantity,
                UnitPrice = price,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.CartItems.Add(cartItem);

            // Kontenjanı güncelle
            switch (model.TicketType.ToLower())
            {
                case "normal":
                    @event.NormalTicketCount -= model.Quantity;
                    break;
                case "student":
                    @event.StudentTicketCount -= model.Quantity;
                    break;
                case "senior":
                    @event.SeniorTicketCount -= model.Quantity;
                    break;
            }

            await _context.SaveChangesAsync();

            var cartCount = await _context.CartItems
                .Where(c => c.UserId == user.Id)
                .SumAsync(c => c.Quantity);

            return Json(new { success = true, cartCount });
        }

        [HttpPost]
        [Route("Cart/RemoveFromCart/{id}")]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Kullanıcı bulunamadı" });

            var cartItem = await _context.CartItems
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Sepet öğesi bulunamadı" });
            }

            // Kontenjanı geri ekle
            switch (cartItem.TicketType.ToLower())
            {
                case "normal":
                    cartItem.Event.NormalTicketCount += cartItem.Quantity;
                    break;
                case "student":
                    cartItem.Event.StudentTicketCount += cartItem.Quantity;
                    break;
                case "senior":
                    cartItem.Event.SeniorTicketCount += cartItem.Quantity;
                    break;
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Kullanıcı bulunamadı" });

            var cartItems = await _context.CartItems
                .Include(c => c.Event)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return Json(new { success = false, message = "Sepetiniz boş" });
            }

            // Satın alınan biletleri kaydet
            foreach (var item in cartItems)
            {
                var ticket = new Ticket
                {
                    EventId = item.EventId,
                    UserId = user.Id,
                    CustomerName = user.UserName ?? string.Empty,
                    CustomerEmail = user.Email ?? string.Empty,
                    Quantity = item.Quantity,
                    TotalPrice = item.UnitPrice * item.Quantity,
                    Status = "Active"
                };

                _context.Tickets.Add(ticket);
            }

            // Sepeti temizle
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<JsonResult> UpdateQuantity(int cartItemId, int quantity)
        {
            try
            {
                var userId = User.Identity?.Name;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Oturum bulunamadı." });
                }

                var cartItem = await _context.CartItems
                    .Include(c => c.Event)
                    .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Sepet öğesi bulunamadı." });
                }

                // Stok kontrolü
                int availableStock = 0;
                switch (cartItem.TicketType.ToLower())
                {
                    case "normal":
                        availableStock = cartItem.Event.NormalTicketCount + cartItem.Quantity;
                        break;
                    case "student":
                        availableStock = cartItem.Event.StudentTicketCount + cartItem.Quantity;
                        break;
                    case "senior":
                        availableStock = cartItem.Event.SeniorTicketCount + cartItem.Quantity;
                        break;
                    case "vip":
                        availableStock = cartItem.Event.VIPTicketCount + cartItem.Quantity;
                        break;
                }

                if (quantity > availableStock)
                {
                    return Json(new { success = false, message = "Seçilen miktar stok limitini aşıyor." });
                }

                // Stok güncelleme
                int stockDifference = cartItem.Quantity - quantity;
                switch (cartItem.TicketType.ToLower())
                {
                    case "normal":
                        cartItem.Event.NormalTicketCount += stockDifference;
                        break;
                    case "student":
                        cartItem.Event.StudentTicketCount += stockDifference;
                        break;
                    case "senior":
                        cartItem.Event.SeniorTicketCount += stockDifference;
                        break;
                    case "vip":
                        cartItem.Event.VIPTicketCount += stockDifference;
                        break;
                }

                cartItem.Quantity = quantity;
                cartItem.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                var cartCount = await _context.CartItems
                    .Where(c => c.UserId == userId)
                    .SumAsync(c => c.Quantity);

                decimal total = cartItem.Quantity * cartItem.UnitPrice;

                return Json(new { 
                    success = true, 
                    message = "Miktar güncellendi.", 
                    cartCount = cartCount,
                    total = total 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Miktar güncelleme hatası");
                return Json(new { success = false, message = "Miktar güncellenirken bir hata oluştu." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var userId = User.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(0);
            }

            var count = await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);

            return Json(count);
        }

        [HttpGet]
        public async Task<JsonResult> GetCartItems()
        {
            var userId = User.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new List<CartItem>());
            }

            var cartItems = await _context.CartItems
                .Include(c => c.Event)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return Json(cartItems);
        }
    }
} 