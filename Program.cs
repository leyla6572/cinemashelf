using Microsoft.EntityFrameworkCore; // 1. ADIM: EF Core kütüphanesini en üste ekledik
using CinemaShelf.Data; // Bizim Data klasörümüzü tanýttýk

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 2. ADIM: Veri tabaný merkezimizi (DbContext) projeye burada tanýttýk.
// appsettings.json dosyasýndaki "DefaultConnection" isimli baðlantý cümlesini kullanacak.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();