using Microsoft.AspNetCore.Mvc;
using webUygulama.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

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
                Password = password,
                IsApproved = false,
                IsAdmin = false,
                PasswordChanged = false
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Kayıt başarılı. Yönetici onayı bekleniyor.";
            return RedirectToAction("Login");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            // Session'da kullanıcı varsa çıkış yap
            HttpContext.Session.Clear();
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                // Debug bilgisi
                Debug.WriteLine($"Giriş denemesi - Email: {email}");

                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    TempData["Error"] = "E-posta adresi bulunamadı.";
                    return View();
                }

                // Debug bilgisi
                Debug.WriteLine($"Kullanıcı bulundu - IsAdmin: {user.IsAdmin}, IsApproved: {user.IsApproved}");

                if (user.Password != password)
                {
                    TempData["Error"] = "Şifre hatalı.";
                    return View();
                }

                if (!user.IsApproved)
                {
                    TempData["Error"] = "Hesabınız henüz yönetici tarafından onaylanmadı.";
                    return View();
                }

                // Session'a kullanıcı bilgilerini kaydet
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserRole", user.IsAdmin ? "Admin" : "User");

                if (!user.PasswordChanged)
                {
                    TempData["Warning"] = "İlk girişiniz. Lütfen şifrenizi değiştirin.";
                    return RedirectToAction("ChangePassword");
                }

                if (user.IsAdmin)
                {
                    Debug.WriteLine("Yönetici girişi başarılı - Admin paneline yönlendiriliyor");
                    TempData["Success"] = "Yönetici girişi başarılı!";
                    return RedirectToAction("Index", "Admin");
                }

                TempData["Success"] = "Giriş başarılı!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Login hatası: {ex.Message}");
                TempData["Error"] = "Giriş sırasında bir hata oluştu. Lütfen tekrar deneyin.";
                return View();
            }
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


