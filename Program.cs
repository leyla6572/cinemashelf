using Microsoft.EntityFrameworkCore; // 1. ADIM: EF Core kütüphanesini en üste ekledik
using CinemaShelf.Data;
using Microsoft.AspNetCore.Authentication.Cookies; // Bizim Data klasörümüzü tanýttýk

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 2. ADIM: Veri tabaný merkezimizi (DbContext) projeye burada tanýttýk.
// appsettings.json dosyasýndaki "DefaultConnection" isimli baðlantý cümlesini kullanacak.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// TMDB API Servis Kaydý
builder.Services.AddHttpClient<CinemaShelf.Services.MovieApiService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["TmdbSettings:BaseUrl"] ?? "https://api.themoviedb.org/3/");
});

// Önce Kimlik Doðrulama (Cookie) servisini ekliyoruz
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Giriþ yapmamýþ biri yetkili sayfaya girerse buraya atar
        options.ExpireTimeSpan = TimeSpan.FromDays(7); // 7 gün boyunca beni hatýrla
    });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();