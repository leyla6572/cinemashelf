using CinemaShelf.Data;
using CinemaShelf.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CinemaShelf.Controllers
{
    [Authorize] // Sadece giriş yapmış kullanıcılar bu controller'a erişebilir
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        // ==================== PROFİL ANA SAYFASI ====================
        public async Task<IActionResult> Index()
        {
            // 1. Giriş yapan kullanıcının ID'sini cookielerden çekiyoruz
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // 2. Kullanıcıyı ve onun rafındaki filmleri (UserMovies -> Movie) veri tabanından getiriyoruz
            var user = await _context.AppUsers
                .Include(u => u.UserMovies)
                    .ThenInclude(um => um.Movie)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            return View(user);
        }

        // ==================== KİŞİSEL RAFA FİLM EKLEME (YENİ AJAX METODUMUZ) ====================
        [HttpPost]
        public async Task<IActionResult> AddToUserShelf([FromBody] MovieInputModel input)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Json(new { success = false, message = "Lütfen önce giriş yapın!" });
            }

            if (input == null || input.TmdbId <= 0)
            {
                return Json(new { success = false, message = "Geçersiz film verisi." });
            }

            try
            {
                // 1. Film genel havuzda (Movies tablosunda) zaten var mı?
                var movie = await _context.Movies.FirstOrDefaultAsync(m => m.TmdbId == input.TmdbId);

                if (movie == null)
                {
                    // Eğer film Movies tablosunda hiç yoksa, önce oraya ekliyoruz
                    movie = new Movie
                    {
                        TmdbId = input.TmdbId,
                        Title = input.Title,
                        PosterPath = input.PosterPath
                    };
                    _context.Movies.Add(movie);
                    await _context.SaveChangesAsync(); // SQL ortak filme bir Id atadı
                }

                // 2. Kullanıcı bu filmi KENDİ rafına zaten eklemiş mi?
                var alreadyAdded = await _context.UserMovies
                    .AnyAsync(um => um.AppUserId == userId && um.MovieId == movie.Id);

                if (alreadyAdded)
                {
                    return Json(new { success = false, message = $"'{movie.Title}' zaten rafınızda ekli!" });
                }

                // 3. Kullanıcının rafına (UserMovies ara tablosuna) ekle
                var userMovie = new UserMovie
                {
                    AppUserId = userId,
                    MovieId = movie.Id,
                    Status = WatchStatus.Watchlist, // Varsayılan olarak "İzleyeceklerim" rafine atıyoruz
                    AddedDate = DateTime.Now
                };

                _context.UserMovies.Add(userMovie);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"'{movie.Title}' kişisel rafınıza başarıyla eklendi! 🍿" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }
    }

    // JavaScript'ten gelen verileri karşılamak için ufak bir yardımcı model
    public class MovieInputModel
    {
        public int TmdbId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? PosterPath { get; set; }
    }
}