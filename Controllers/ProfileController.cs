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

            // 2. Kullanıcıyı ve ilişkili verilerini çekiyoruz
            var user = await _context.AppUsers
                .Include(u => u.UserMovies)
                    .ThenInclude(um => um.Movie)
                .Include(u => u.Reviews)
                    .ThenInclude(r => r.Movie)
                .Include(u => u.MovieQuotes)
                    .ThenInclude(mq => mq.Movie)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Filmleri durumlarına göre ayırıyoruz
            var watchlistMovies = user.UserMovies?.Where(um => um.Status == WatchStatus.Watchlist).ToList() ?? new List<UserMovie>();
            var watchedMovies = user.UserMovies?.Where(um => um.Status == WatchStatus.Watched).ToList() ?? new List<UserMovie>();

            ViewBag.Watchlist = watchlistMovies;
            ViewBag.Watched = watchedMovies;

            // Sayaçlar
            ViewBag.TotalMovies = user.UserMovies?.Count ?? 0;
            ViewBag.WatchlistCount = watchlistMovies.Count;
            ViewBag.WatchedCount = watchedMovies.Count;
            ViewBag.ReviewsCount = user.Reviews?.Count ?? 0;
            ViewBag.QuotesCount = user.MovieQuotes?.Count ?? 0;
            ViewBag.UserMoviesList = user.UserMovies?.Select(um => um.Movie).Where(m => m != null).ToList() ?? new List<Movie>();

            // 🌟 TMDB API ENTEGRASYONU BAŞLIYOR 🌟
            var genreStats = new Dictionary<string, int>();
            var directorStats = new Dictionary<string, int>();
            int totalRuntime = 0;

            using (var client = new HttpClient())
            {
                // Kendi TMDB API Key'ini buraya yazmalısın!
                string apiKey = "be4023c1d851d362678d76d5abe82a07";

                foreach (var userMovie in watchedMovies)
                {
                    if (userMovie.Movie != null)
                    {
                        try
                        {
                            // TMDB'den film detaylarını (ve credits/ekip bilgisini) Türkçe olarak istiyoruz
                            var url = $"https://api.themoviedb.org/3/movie/{userMovie.Movie.TmdbId}?api_key={apiKey}&append_to_response=credits&language=tr-TR";
                            var response = await client.GetFromJsonAsync<TmdbMovieDetailDto>(url);

                            if (response != null)
                            {
                                // A. Süreyi topluyoruz
                                totalRuntime += response.Runtime ?? 0;

                                // B. Türleri listeye ekliyoruz
                                // B. Türleri sitemizin 5 ana kategorisine eşleyip ekliyoruz
                                if (response.Genres != null)
                                {
                                    string? selectedGenre = null;

                                    foreach (var genre in response.Genres)
                                    {
                                        string originalGenre = genre.Name.ToLower();

                                        if (originalGenre.Contains("aksiyon") || originalGenre.Contains("macera") || originalGenre.Contains("korku") || originalGenre.Contains("gerilim") || originalGenre.Contains("savaş"))
                                        {
                                            selectedGenre = "Aksiyon";
                                        }
                                        else if (originalGenre.Contains("komedi") || originalGenre.Contains("romantik") || originalGenre.Contains("romantik komedi"))
                                        {
                                            selectedGenre = "Komedi";
                                        }
                                        else if (originalGenre.Contains("dram") || originalGenre.Contains("belgesel") || originalGenre.Contains("tarih") || originalGenre.Contains("suç"))
                                        {
                                            selectedGenre = "Dram";
                                        }
                                        else if (originalGenre.Contains("bilim-kurgu") || originalGenre.Contains("bilim kurgu") || originalGenre.Contains("fantastik") || originalGenre.Contains("gizem"))
                                        {
                                            selectedGenre = "Bilim-Kurgu";
                                        }
                                        else if (originalGenre.Contains("animasyon") || originalGenre.Contains("aile") || originalGenre.Contains("anime"))
                                        {
                                            selectedGenre = "Animasyon & Anime";
                                        }

                                        // 🌟 SIHİRLİ DOKUNUŞ: Film için ilk eşleşen kategoriyi bulduğumuz an döngüden çıkıyoruz!
                                        // Böylece bir film sadece tek bir kategoriye yazılacak.
                                        if (!string.IsNullOrEmpty(selectedGenre))
                                        {
                                            break;
                                        }
                                    }

                                    // Seçilen tek kategoriyi istatistiğe ekle
                                    if (!string.IsNullOrEmpty(selectedGenre))
                                    {
                                        if (genreStats.ContainsKey(selectedGenre))
                                            genreStats[selectedGenre]++;
                                        else
                                            genreStats[selectedGenre] = 1;
                                    }
                                }

                                // C. Yönetmeni bulup ekliyoruz (Crew içinde job == "Director" olan)
                                var director = response.Credits?.Crew?.FirstOrDefault(c => c.Job == "Director")?.Name;
                                if (!string.IsNullOrEmpty(director))
                                {
                                    if (directorStats.ContainsKey(director))
                                        directorStats[director]++;
                                    else
                                        directorStats[director] = 1;
                                }
                            }
                        }
                        catch
                        {
                            // Herhangi bir filmde API hatası alınırsa uygulamanın çökmemesi için pas geçiyoruz
                        }
                    }
                }
            }

            // İstatistikleri ViewBag'lere aktarıyoruz
            ViewBag.GenreStats = genreStats.OrderByDescending(x => x.Value).Take(10).ToList();
            ViewBag.DirectorStats = directorStats.OrderByDescending(x => x.Value).Take(10).ToList();

            // 🌟 YILLARA GÖRE İZLEME İSTATİSTİKLERİ (Gerçek Süreyle Birlikte)
            ViewBag.YearlyStats = watchedMovies
                .GroupBy(um => um.AddedDate.Year)
                .Select(g => new
                {
                    Year = g.Key,
                    MovieCount = g.Count(),
                    TotalMinutes = totalRuntime // API'den gelen gerçek toplam süreleri yazdırıyoruz
                })
                .OrderByDescending(g => g.Year)
                .ToList();

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

                // DEĞİŞEN ALAN BAŞLANGICI
                if (input.NewStatus == "Watched" && !string.IsNullOrWhiteSpace(input.Comment))
                {
                    var existingReview = await _context.Reviews
                        .FirstOrDefaultAsync(r => r.AppUserId == userId && r.MovieId == input.MovieId);

                    if (existingReview == null)
                    {
                        var newReview = new Review
                        {
                            AppUserId = userId,
                            MovieId = input.MovieId,
                            Content = input.Comment.Trim(), // Başındaki ve sonundaki gereksiz boşlukları temizler
                            Rating = input.Rating,
                            CreatedDate = DateTime.Now
                        };
                        _context.Reviews.Add(newReview);
                    }
                }
                // DEĞİŞEN ALAN BİTİŞİ

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

        [HttpPost]
        public async Task<IActionResult> RemoveFromShelf(int movieId)
        {
            // 1. Giriş yapan kullanıcının ID'sini çekiyoruz.
            // Eğer projedeki diğer metotlarda farklı bir yöntem (örn: _userManager.GetUserId(User)) 
            // kullanıyorsan burayı projenle aynı yapmalısın.
            var currentUserIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserIdStr))
            {
                return Json(new { success = false, message = "Lütfen önce giriş yapın." });
            }

            // Eğer projedeki AppUser ID'si string değil de int ise dönüştürme yapıyoruz:
            if (!int.TryParse(currentUserIdStr, out int currentUserId))
            {
                return Json(new { success = false, message = "Geçersiz kullanıcı oturumu." });
            }

            try
            {
                // 2. Veritabanından ilgili kaydı 'UserMovies' tablosundan buluyoruz.
                var shelfItem = await _context.UserMovies
                    .FirstOrDefaultAsync(x => x.MovieId == movieId && x.AppUserId == currentUserId);

                if (shelfItem != null)
                {
                    // 3. İlgili kaydı veritabanından kaldırıp değişiklikleri kaydediyoruz.
                    _context.UserMovies.Remove(shelfItem);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Film başarıyla raftan kaldırıldı." });
                }

                return Json(new { success = false, message = "Film listenizde bulunamadı." });
            }
            catch (Exception ex)
            {
                // Geliştirme aşamasında hatayı konsolda görebilmek için:
                Console.WriteLine($"RemoveFromShelf Hatası: {ex.Message}");
                return Json(new { success = false, message = "Film kaldırılırken sistemsel bir hata oluştu." });
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

        // GET veya POST: /User/FollowUser
        public async Task<IActionResult> FollowUser(int targetUserId)
        {
            // 1. Oturum açmış olan mevcut kullanıcının ID'sini almalısın. 
            // Projende Session mı kullanıyorsun yoksa Cookie/Identity mi? 
            // Şimdilik örnek olması için "currentUserId" değişkeni olarak tanımlıyorum:
            int currentUserId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");

            if (currentUserId == 0 || currentUserId == targetUserId)
            {
                return RedirectToAction("Index", "Home"); // Geçersiz işlem durumunda ana sayfaya at
            }

            // Daha önce takip etmiş mi kontrol edelim
            var existingFollow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId);

            if (existingFollow != null)
            {
                // Eğer zaten takip ediyorsa: Takipten ÇIK
                _context.Follows.Remove(existingFollow);
            }
            else
            {
                // Eğer takip etmiyorsa: TAKİP ET
                var newFollow = new Follow
                {
                    FollowerId = currentUserId,
                    FollowingId = targetUserId,
                    FollowedDate = DateTime.Now
                };
                await _context.Follows.AddAsync(newFollow);
            }

            await _context.SaveChangesAsync();

            // İşlem bittikten sonra gelinen profile geri dön
            return RedirectToAction("Profile", new { id = targetUserId });
        }


        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string Username, string Email, string Bio, IFormFile? ProfilePictureFile)
        {
            try
            {
                int? userId = null;

                try
                {
                    userId = HttpContext.Session.GetInt32("UserId");
                }
                catch
                {
                    // Session kapalıysa düşmesin
                }

                AppUser? user = null;

                if (userId.HasValue)
                {
                    user = _context.AppUsers.FirstOrDefault(u => u.Id == userId.Value);
                }

                if (user == null && !string.IsNullOrEmpty(Email))
                {
                    user = _context.AppUsers.FirstOrDefault(u => u.Email == Email);
                }

                if (user == null)
                {
                    user = _context.AppUsers.FirstOrDefault();
                }

                if (user != null)
                {
                    user.Username = Username;
                    user.Email = Email;
                    user.Bio = Bio ?? "";

                    // 🌟 RESİM YÜKLEME İŞLEMİ 🌟
                    if (ProfilePictureFile != null && ProfilePictureFile.Length > 0)
                    {
                        // 1. Dosya için benzersiz bir isim oluştur (Örn: a1b2c3d4_profil.jpg)
                        string extension = Path.GetExtension(ProfilePictureFile.FileName);
                        string newFileName = Guid.NewGuid().ToString() + extension;

                        // 2. Projenin wwwroot/uploads yolunu belirle
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                        // Klasör yoksa oluştur
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string filePath = Path.Combine(uploadsFolder, newFileName);

                        // 3. Dosyayı sunucuya kaydet
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ProfilePictureFile.CopyToAsync(stream);
                        }

                        // 4. Veritabanındaki ProfilePicture sütununa erişim yolunu yaz
                        user.ProfilePicture = "/uploads/" + newFileName;
                    }

                    _context.Entry(user).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    _context.SaveChanges();
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return Content("Hata oluştu: " + ex.Message);
            }
        }
    }





    public class MovieInputModel
    {
        public int TmdbId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? PosterPath { get; set; }
    }

}
public class TmdbMovieDetailDto
{
    public List<TmdbGenreDto>? Genres { get; set; }
    public int? Runtime { get; set; }
    public TmdbCreditsDto? Credits { get; set; }
}

public class TmdbGenreDto
{
    public string Name { get; set; } = string.Empty;
}

public class TmdbCreditsDto
{
    public List<TmdbCrewDto>? Crew { get; set; }
}

public class TmdbCrewDto
{
    public string Name { get; set; } = string.Empty;
    public string Job { get; set; } = string.Empty; // "Director" olanı aratacağız
}