using Microsoft.EntityFrameworkCore;
using webUygulama.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace webUygulama.Repositories
{
    public class UserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        // E-posta ile kullanıcı alma metodu
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);  // E-posta ile kullanıcıyı arar
        }

        // Kullanıcıyı onaylama metodu
        public async Task ApproveUserAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.IsApproved = true;  // Onay durumunu true yap
                user.PasswordChanged = false;  // Şifre değiştirilmedi olarak işaretle
                await _context.SaveChangesAsync();  // Değişiklikleri kaydet
            }
        }

        // Yönetici olarak giriş yapma metodu
        public async Task<User?> ValidateAdminLoginAsync(string email, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

            if (user != null && user.IsAdmin)  // Kullanıcı var ve yönetici mi kontrol et
            {
                return user;
            }

            return null;  // Yönetici değilse veya kullanıcı bulunamazsa null döner
        }

        // Tüm kullanıcıları alma metodu (Admin için)
        public async Task<List<User>> GetUsersAsync()
        {
            return await _context.Users.ToListAsync();  // Tüm kullanıcıları asenkron olarak alır
        }

        // Kullanıcıyı ID ile alma metodu
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);  // Kullanıcıyı ID ile alır
        }

        // Kullanıcıyı güncelleme metodu (Şifre de dahil)
        public async Task UpdateUserAsync(User user)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);

            if (existingUser != null)
            {
                // Veritabanındaki mevcut kullanıcıyı güncelle
                existingUser.Email = user.Email;
                existingUser.Password = user.Password;  // Şifreyi güncelle
                existingUser.IsApproved = user.IsApproved;
                existingUser.PasswordChanged = user.PasswordChanged;  // Şifre değiştirildi bilgisini güncelle

                await _context.SaveChangesAsync();  // Değişiklikleri kaydet
            }
        }

        public async Task<List<User>> GetPendingUsersAsync()
        {
            // Onay bekleyen (IsApproved == false) kullanıcıları alıyoruz
            return await _context.Users
                                 .Where(u => !u.IsApproved)
                                 .ToListAsync();
        }


        // Kullanıcı şifresini değiştirme ve PasswordChanged'i güncelleme metodu
        public async Task ChangePasswordAsync(int userId, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user != null)
            {
                user.Password = newPassword;  // Yeni şifreyi güncelle
                user.PasswordChanged = true;  // Şifre değiştirildi olarak işaretle
                await _context.SaveChangesAsync();  // Değişiklikleri kaydet
            }
        }
    }
}

