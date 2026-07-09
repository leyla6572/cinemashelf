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

        [Authorize]
        public async Task<IActionResult> Index()
        {
            // 1. Giriş yapan kullanıcının ID'sini cookielerden çekiyoruz
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // 2. Kullanıcıyı; rafı, yorumları VE yeni eklediğimiz film replikleriyle birlikte çekiyoruz
            var user = await _context.AppUsers
                .Include(u => u.UserMovies)
                    .ThenInclude(um => um.Movie)
                .Include(u => u.Reviews)
                    .ThenInclude(r => r.Movie)
                .Include(u => u.MovieQuotes) // 🌟 YENİ: Replikleri dahil ediyoruz
                    .ThenInclude(mq => mq.Movie)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Filmleri durumlarına göre (WatchStatus) ayırıp ViewBag'lere teslim ediyoruz
            ViewBag.Watchlist = user.UserMovies?.Where(um => um.Status == WatchStatus.Watchlist).ToList() ?? new List<UserMovie>();
            ViewBag.Watched = user.UserMovies?.Where(um => um.Status == WatchStatus.Watched).ToList() ?? new List<UserMovie>();

            // Dinamik istatistikler ve profil sekmelerinin sayaçları
            ViewBag.TotalMovies = user.UserMovies?.Count ?? 0;
            ViewBag.WatchlistCount = user.UserMovies?.Count(um => um.Status == WatchStatus.Watchlist) ?? 0;
            ViewBag.WatchedCount = user.UserMovies?.Count(um => um.Status == WatchStatus.Watched) ?? 0;
            ViewBag.ReviewsCount = user.Reviews?.Count ?? 0;
            ViewBag.QuotesCount = user.MovieQuotes?.Count ?? 0; // 🌟 YENİ: Replik sayısı sayacı

            // Replik ekleme modalında kullanıcının rafındaki (seçebileceği) filmleri listelemek için gönderiyoruz
            ViewBag.UserMoviesList = user.UserMovies?.Select(um => um.Movie).Where(m => m != null).ToList() ?? new List<Movie>();

            // View'a ana model olarak 'user' nesnesini gönderiyoruz
            return View(user);
        }

        // ==================== KİŞİSEL RAFA FİLM EKLEME (AJAX METODU) ====================
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
                var movie = await _context.Movies.FirstOrDefaultAsync(m => m.TmdbId == input.TmdbId);

                if (movie == null)
                {
                    movie = new Movie
                    {
                        TmdbId = input.TmdbId,
                        Title = input.Title,
                        PosterPath = input.PosterPath
                    };
                    _context.Movies.Add(movie);
                    await _context.SaveChangesAsync();
                }

                var alreadyAdded = await _context.UserMovies
                    .AnyAsync(um => um.AppUserId == userId && um.MovieId == movie.Id);

                if (alreadyAdded)
                {
                    return Json(new { success = false, message = $"'{movie.Title}' zaten rafınızda ekli!" });
                }

                var userMovie = new UserMovie
                {
                    AppUserId = userId,
                    MovieId = movie.Id,
                    Status = WatchStatus.Watchlist,
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

        // ==================== RAFTAKİ FİLM DURUMUNU GÜNCELLEME ====================
        [HttpPost]
        public async Task<IActionResult> UpdateShelfStatus([FromBody] ShelfUpdateInputModel input)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Json(new { success = false, message = "Oturum bulunamadı!" });
            }

            try
            {
                var shelfItem = await _context.UserMovies
                    .FirstOrDefaultAsync(um => um.AppUserId == userId && um.MovieId == input.MovieId);

                if (shelfItem == null)
                {
                    return Json(new { success = false, message = "Film listenizde bulunamadı!" });
                }

                if (Enum.TryParse(typeof(WatchStatus), input.NewStatus, out var parsedStatus))
                {
                    shelfItem.Status = (WatchStatus)parsedStatus;
                }
                else
                {
                    return Json(new { success = false, message = "Geçersiz film durumu!" });
                }

                if (input.NewStatus == "Watched" && (!string.IsNullOrEmpty(input.Comment) || input.Rating > 0))
                {
                    var existingReview = await _context.Reviews
                        .FirstOrDefaultAsync(r => r.AppUserId == userId && r.MovieId == input.MovieId);

                    if (existingReview == null)
                    {
                        var newReview = new Review
                        {
                            AppUserId = userId,
                            MovieId = input.MovieId,
                            Content = input.Comment ?? "İzledim olarak işaretlendi.",
                            Rating = input.Rating,
                            CreatedDate = DateTime.Now
                        };
                        _context.Reviews.Add(newReview);
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Film durumu başarıyla güncellendi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        // ==================== 🌟 YENİ: YENİ REPLİK KAYDETME (AJAX METODU) ====================
        [HttpPost]
        public async Task<IActionResult> AddQuote([FromBody] QuoteCreateInputModel input)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Json(new { success = false, message = "Oturum bulunamadı, lütfen tekrar giriş yapın." });
            }

            if (input == null || string.IsNullOrWhiteSpace(input.Content) || input.MovieId <= 0)
            {
                return Json(new { success = false, message = "Lütfen geçerli bir film seçin ve replik alanını doldurun." });
            }

            try
            {
                var newQuote = new MovieQuote
                {
                    Content = input.Content.Trim(),
                    MovieId = input.MovieId,
                    AppUserId = userId,
                    CreatedDate = DateTime.Now
                };

                _context.MovieQuotes.Add(newQuote);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Repliğiniz başarıyla profilinize eklendi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }

        // JS'den gelen verileri karşılayan yardımcı modeller
        public class ShelfUpdateInputModel
        {
            public int MovieId { get; set; }
            public string NewStatus { get; set; }
            public int Rating { get; set; }
            public string? Comment { get; set; }
        }

        // 🌟 YENİ: Replik modalından gelen JSON'ı karşılayan model
        public class QuoteCreateInputModel
        {
            public int MovieId { get; set; }
            public string Content { get; set; } = string.Empty;
        }
    }

    public class MovieInputModel
    {
        public int TmdbId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? PosterPath { get; set; }
    }
}