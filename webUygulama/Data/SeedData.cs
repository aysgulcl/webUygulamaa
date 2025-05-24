using Microsoft.EntityFrameworkCore;
using webUygulama.Models;
using Microsoft.AspNetCore.Identity;

namespace webUygulama.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Admin kullanıcısı var mı kontrol et
                if (await userManager.Users.AnyAsync(u => u.IsAdmin))
                    return;

                // Admin kullanıcısını ekle
                var adminUser = new User
                {
                    UserName = "aysegulcancatal@gmail.com",
                    Email = "aysegulcancatal@gmail.com",
                    IsAdmin = true,
                    IsApproved = true,
                    PasswordChanged = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
} 