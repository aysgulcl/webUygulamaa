using Microsoft.EntityFrameworkCore;
using webUygulama.Models;
using webUygulama.Repositories;
using webUygulama.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ Connection string üzerinden DbContext kaydı
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Repository'leri DI sistemine ekle
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<EventRepository>();

// ✅ TicketmasterService'e HttpClient ile erişim
builder.Services.AddHttpClient<TicketmasterService>();
builder.Services.AddScoped<TicketmasterService>();

// Session ayarlarını güncelle
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout süresi
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ✅ MVC controller ve view servisi
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ✅ Üretim ortamı için hata yönetimi
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Session middleware'ini ekle
app.UseSession();

app.UseAuthorization();

// ✅ Başlangıç rotası
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();

