using Microsoft.AspNetCore.Mvc;
using webUygulama.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace webUygulama.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(string email, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                TempData["Error"] = "Bu e-posta adresi zaten kayıtlı.";
                return View();
            }

            var newUser = new User
            {
                Email = email,
                Password = password, // Şifre açık şekilde kaydediliyor
                IsApproved = false,
                IsAdmin = false,
                PasswordChanged = false
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Kayıt başarılı. Yönetici onayı bekleniyor.";
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

            if (user == null)
            {
                TempData["Error"] = "E-posta veya şifre hatalı.";
                return View();
            }

            if (!user.IsApproved)
            {
                TempData["Error"] = "Hesabınız henüz yönetici tarafından onaylanmadı.";
                return View();
            }

            HttpContext.Session.SetString("UserEmail", user.Email);

            if (!user.PasswordChanged)
            {
                TempData["Success"] = "Lütfen şifrenizi değiştirin.";
                return RedirectToAction("ChangePassword", "Account");
            }

            if (user.IsAdmin)
            {
                return RedirectToAction("Index", "Admin");
            }

            TempData["Success"] = "Giriş başarılı!";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var email = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || user.Password != currentPassword)
            {
                TempData["Error"] = "Mevcut şifreniz hatalı.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Yeni şifreler eşleşmiyor.";
                return View();
            }

            user.Password = newPassword;
            user.PasswordChanged = true;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Şifreniz başarıyla değiştirildi.";

            if (user.IsAdmin)
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}


