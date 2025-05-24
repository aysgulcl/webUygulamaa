using Microsoft.EntityFrameworkCore;
using webUygulama.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using webUygulama.Data;
using Microsoft.AspNetCore.Identity;

namespace webUygulama.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public UserRepository(
            ApplicationDbContext context,
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // E-posta ile kullanıcı alma metodu
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        // Kullanıcıyı onaylama metodu
        public async Task ApproveUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsApproved = true;
                user.PasswordChanged = false;
                await _userManager.UpdateAsync(user);
            }
        }

        // Yönetici olarak giriş yapma metodu
        public async Task<User?> ValidateAdminLoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null && user.IsAdmin)
            {
                var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
                if (result.Succeeded)
                {
                    return user;
                }
            }
            return null;
        }

        // Tüm kullanıcıları alma metodu (Admin için)
        public async Task<List<User>> GetUsersAsync()
        {
            return await _userManager.Users.ToListAsync();
        }

        // Kullanıcıyı ID ile alma metodu
        public async Task<User?> GetUserByIdAsync(string id)
        {
            return await _userManager.FindByIdAsync(id);
        }

        // Kullanıcıyı güncelleme metodu
        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                await _userManager.UpdateAsync(user);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<User>> GetPendingUsersAsync()
        {
            return await _userManager.Users
                .Where(u => !u.IsApproved)
                .ToListAsync();
        }

        // Kullanıcı şifresini değiştirme metodu
        public async Task ChangePasswordAsync(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, newPassword);
                user.PasswordChanged = true;
                await _userManager.UpdateAsync(user);
            }
        }

        public async Task<bool> DeleteUserAsync(string id)
        {
            try
            {
                var user = await GetUserByIdAsync(id);
                if (user != null)
                {
                    await _userManager.DeleteAsync(user);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}

