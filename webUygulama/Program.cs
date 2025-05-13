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
builder.Services.AddHttpClient<TicketmasterService>(); // 🔁 Doğru kayıt şekli
builder.Services.AddScoped<TicketmasterService>();

// ✅ MVC controller ve view servisi
builder.Services.AddControllersWithViews();

// ✅ Session middleware’i
builder.Services.AddSession();

var app = builder.Build();

// ✅ Üretim ortamı için hata yönetimi
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthorization();

// ✅ Başlangıç rotası
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Register}/{id?}");

app.Run();

