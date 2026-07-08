using Microsoft.AspNetCore.Mvc;
using CinemaShelf.Services;
using CinemaShelf.Data; // 🌟 Veritabanı context'i için eklendi
using CinemaShelf.Models; // 🌟 Review ve Movie modelleri için eklendi
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;

namespace CinemaShelf.Controllers
{
    public class MovieController : Controller
    {
        private readonly MovieApiService _movieApiService;
        private readonly AppDbContext _context; // 🌟 Veritabanı bağlantısı buraya tanımlandı

        // Constructor ile HEM API servisini HEM DE veritabanı context'ini içeri alıyoruz
        public MovieController(MovieApiService movieApiService, AppDbContext context)
        {
            _movieApiService = movieApiService;
            _context = context; // 🌟 İçeri alınan context değişkene bağlandı
        }

        // URL: /Movie/Details/550
        public async Task<IActionResult> Details(int id)
        {
            // 1. Önce API'den filmin detaylarını çekiyoruz (Senin mevcut API akışın)
            var apiMovie = await _movieApiService.GetMovieDetailsAsync(id);
            if (apiMovie == null)
            {
                return NotFound("Film API'den yüklenemedi.");
            }

            
            // 2. Şimdi bizim yerel veritabanımıza gidip, bu filme yapılmış yorumları VE BEĞENİLERİ çekiyoruz
            var localMovie = await _context.Movies
                .Include(m => m.Reviews)
                    .ThenInclude(r => r.AppUser)
                .Include(m => m.Reviews)
                    .ThenInclude(r => r.ReviewLikes) // 🌟 YENİ: Yorumların beğenilerini de dahil ettik
                .FirstOrDefaultAsync(m => m.TmdbId == id);

            // 3. Yorumları View'a güvenle taşımak için ViewBag kullanıyoruz
            if (localMovie != null && localMovie.Reviews != null)
            {
                ViewBag.Reviews = localMovie.Reviews;
            }
            else
            {
                ViewBag.Reviews = new List<Review>(); // Hiç yorum yoksa boş liste gitsin, patlamasın
            }

            // 4. Senin orijinal View yapını bozmamak için yine API'den gelen modeli gönderiyoruz
            return View(apiMovie);
        }

        // ==================== YENİ: AJAX İLE YORUM EKLEME METODU ====================
        [HttpPost]
        [Authorize] // Sadece giriş yapanlar yorum yapabilsin
        public async Task<IActionResult> AddReview([FromBody] ReviewInputModel input)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Json(new { success = false, message = "Yorum yapmak için giriş yapmalısınız!" });
            }

            if (input == null || string.IsNullOrWhiteSpace(input.Content) || input.Rating < 1 || input.Rating > 10)
            {
                return Json(new { success = false, message = "Geçersiz yorum veya puan verisi." });
            }

            try
            {
                // Yorum yapılacak film ortak Movies tablosunda var mı?
                var movie = await _context.Movies.FirstOrDefaultAsync(m => m.TmdbId == input.TmdbId);

                if (movie == null)
                {
                    // Eğer film ortak tablonuzda henüz yoksa, yorum yapılabilmesi için otomatik olarak ekliyoruz
                    // (Not: apiMovie detaylarından başlığı çekebiliriz ama AJAX'tan gelen başlığı kullanmak en temizi)
                    movie = new Movie
                    {
                        TmdbId = input.TmdbId,
                        Title = input.MovieTitle ?? "Bilinmeyen Film",
                        PosterPath = input.PosterPath
                    };
                    _context.Movies.Add(movie);
                    await _context.SaveChangesAsync();
                }

                // Yeni yorumu veritabanına ekle
                var review = new Review
                {
                    AppUserId = userId,
                    MovieId = movie.Id,
                    Content = input.Content,
                    Rating = input.Rating,
                    CreatedDate = DateTime.Now
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Yorumunuz başarıyla eklendi! 🎬" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }
        [HttpPost]
        [Authorize] // Sadece giriş yapmış kullanıcılar beğenebilir
        public async Task<IActionResult> ToggleLikeReview([FromBody] LikeInputModel input)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Json(new { success = false, message = "Beğenmek için giriş yapmalısınız!" });
            }

            try
            {
                // 1. Daha önce bu kullanıcı bu yorumu beğenmiş mi diye bakıyoruz
                var existingLike = await _context.ReviewLikes
                    .FirstOrDefaultAsync(rl => rl.AppUserId == userId && rl.ReviewId == input.ReviewId);

                bool isLiked;

                if (existingLike != null)
                {
                    // 2. Eğer zaten beğendiyse: Beğeniyi kaldır (Sil)
                    _context.ReviewLikes.Remove(existingLike);
                    isLiked = false;
                }
                else
                {
                    // 3. Eğer henüz beğenmediyse: Yeni beğeni ekle
                    var newLike = new ReviewLike
                    {
                        AppUserId = userId,
                        ReviewId = input.ReviewId,
                        LikedDate = DateTime.Now
                    };
                    _context.ReviewLikes.Add(newLike);
                    isLiked = true;
                }

                await _context.SaveChangesAsync();

                // 4. Güncel beğeni sayısını hesaplayıp arayüze geri gönderiyoruz
                var currentLikeCount = await _context.ReviewLikes.CountAsync(rl => rl.ReviewId == input.ReviewId);

                return Json(new { success = true, isLiked = isLiked, likeCount = currentLikeCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }

        // JS'den gelen ReviewId'yi yakalamak için küçük yardımcı model
        public class LikeInputModel
        {
            public int ReviewId { get; set; }
        }
    }

    // JS'den gelen yorum verisini karşılayacak yardımcı model
    public class ReviewInputModel
    {
        public int TmdbId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? MovieTitle { get; set; } // Otomatik kayıt için başlığı da alıyoruz
        public string? PosterPath { get; set; }  // Otomatik kayıt için afişi de alıyoruz
    }


}
