using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using webUygulama.Models;
using System.Collections.Generic;
using System.Linq;

namespace webUygulama.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Kullanıcı ilgi alanları için dönüşüm ve karşılaştırıcı
            builder.Entity<User>()
                .Property(u => u.Interests)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', System.StringSplitOptions.RemoveEmptyEntries).ToList(),
                    new ValueComparer<List<string>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()
                    )
                );

            // Event ve Ticket ilişkisi
            builder.Entity<Event>()
                .HasMany(e => e.Tickets)
                .WithOne(t => t.Event)
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Event ve CartItem ilişkisi
            builder.Entity<CartItem>()
                .HasOne(c => c.Event)
                .WithMany()
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Decimal hassasiyet ayarları
            builder.Entity<Ticket>()
                .Property(t => t.TotalPrice)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Event>()
                .Property(e => e.Price)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Event>()
                .Property(e => e.NormalTicketPrice)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Event>()
                .Property(e => e.VIPTicketPrice)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Event>()
                .Property(e => e.StudentTicketPrice)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Event>()
                .Property(e => e.SeniorTicketPrice)
                .HasColumnType("decimal(18,2)");

            builder.Entity<CartItem>()
                .Property(c => c.UnitPrice)
                .HasColumnType("decimal(18,2)");
        }
    }
} 