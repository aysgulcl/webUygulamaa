using Microsoft.AspNetCore.Mvc;
using webUygulama.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using webUygulama.Data;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace webUygulama.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ApplicationDbContext context,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        // GET: /Account/Profile
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: /Account/UpdateProfile
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(List<string> interests)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                // İlgi alanlarını güncelle
                user.Interests = interests ?? new List<string>();
                
                // Değişiklikleri kaydet
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Kullanıcı {user.Email} ilgi alanlarını güncelledi. Seçilen alanlar: {string.Join(", ", interests)}");
                    TempData["Success"] = "İlgi alanlarınız başarıyla güncellendi.";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    _logger.LogError($"Kullanıcı {user.Email} ilgi alanları güncellenirken hata oluştu: {string.Join(", ", result.Errors)}");
                    TempData["Error"] = "İlgi alanlarınız güncellenirken bir hata oluştu.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"İlgi alanları güncellenirken hata: {ex.Message}");
                TempData["Error"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
            }

            return RedirectToAction("Index", "Home");
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
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                TempData["Error"] = "Bu e-posta adresi zaten kayıtlı.";
                return View();
            }

            var newUser = new User
            {
                UserName = email,
                Email = email,
                IsApproved = false,
                IsAdmin = false,
                PasswordChanged = false
            };

            var result = await _userManager.CreateAsync(newUser, password);
            if (result.Succeeded)
            {
                TempData["Success"] = "Kayıt başarılı. Yönetici onayı bekleniyor.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View();
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    if (!user.IsApproved)
                    {
                        ModelState.AddModelError(string.Empty, "Hesabınız henüz onaylanmamış.");
                        return View(model);
                    }

                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
                    if (result.Succeeded)
                    {
                        // Bu kullanıcıyı admin yap
                        if (model.Email == "aysegulcancatal@gmail.com" && !user.IsAdmin)
                        {
                            user.IsAdmin = true;
                            await _userManager.UpdateAsync(user);
                            await _userManager.AddToRoleAsync(user, "Admin");
                        }

                        if (!user.PasswordChanged)
                        {
                            return RedirectToAction("ChangePassword");
                        }

                        if (user.IsAdmin)
                        {
                            return RedirectToAction("Index", "Admin");
                        }

                        return RedirectToAction("Index", "Home");
                    }
                }
                ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi");
            }
            return View(model);
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
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Yeni şifreler eşleşmiyor.";
                return View();
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Mevcut şifreniz hatalı.";
                return View();
            }

            user.PasswordChanged = true;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Şifreniz başarıyla değiştirildi.";

            if (user.IsAdmin)
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Kullanıcı çıkış yaptı.");
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
    }
}


