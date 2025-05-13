using Microsoft.EntityFrameworkCore;

namespace webUygulama.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSet tanımlamaları
        public DbSet<User> Users { get; set; }

        // ✅ Etkinlikler tablosu
        public DbSet<Event> Events { get; set; }

        // ✅ Admin kullanıcıyı seed et
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Email = "aysegulcancatal@gmail.com",
                Password = "123456",
                IsApproved = true,
                IsAdmin = true,
                PasswordChanged = true
            });
        }
    }
}
