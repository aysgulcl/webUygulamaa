using System.Collections.Generic;
using System.Threading.Tasks;
using webUygulama.Models;

namespace webUygulama.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task ApproveUserAsync(string userId);
        Task<User?> ValidateAdminLoginAsync(string email, string password);
        Task<List<User>> GetUsersAsync();
        Task<User?> GetUserByIdAsync(string id);
        Task<bool> UpdateUserAsync(User user);
        Task<List<User>> GetPendingUsersAsync();
        Task ChangePasswordAsync(string userId, string newPassword);
        Task<bool> DeleteUserAsync(string id);
    }
} 