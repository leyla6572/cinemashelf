using cinemashelf.Models;
using CinemaShelf.Data;
using CinemaShelf.Models;
using CinemaShelf.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;


namespace CinemaShelf.Controllers
{
    public class HomeController : Controller
    {
        private readonly MovieApiService _movieApiService;
        private readonly AppDbContext _context; // Veri tabanı merkezimiz

        // Constructor içinde hem API servisini hem de DbContext'i talep ediyoruz
        public HomeController(MovieApiService movieApiService, AppDbContext context)
        {
            _movieApiService = movieApiService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeViewModel
            {
                ActionMovies = await _movieApiService.GetMoviesByGenreAsync(28),
                ComedyMovies = await _movieApiService.GetMoviesByGenreAsync(35),
                DramaMovies = await _movieApiService.GetMoviesByGenreAsync(18),
                SciFiMovies = await _movieApiService.GetMoviesByGenreAsync(878),

                // 🌟 YENİ: Animasyon ve Anime filmleri için 16 ID'sini kullanıyoruz
                AnimationMovies = await _movieApiService.GetMoviesByGenreAsync(16)
            };

            // (Eğer daha önce yaptıysan veritabanı sözlük kodların da burada kalabilir, dokunma)

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetMoreMovies(int genreId, int page = 2)
        {
            // TODO: Buraya kendi çalışan TMDB API anahtarını yazdığından emin ol!
            string apiKey = "be4023c1d851d362678d76d5abe82a07";

            string url = $"https://api.themoviedb.org/3/discover/movie?api_key={apiKey}&with_genres={genreId}&page={page}&language=tr-TR";

            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();

                        // Ham JSON'ı parse edip sadece "results" kısmını alıyoruz
                        using (JsonDocument doc = JsonDocument.Parse(jsonString))
                        {
                            if (doc.RootElement.TryGetProperty("results", out JsonElement results))
                            {
                                // JavaScript'e sadece filmlerin olduğu listeyi (Array) dönüyoruz
                                return Content(results.GetRawText(), "application/json");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"TMDB API Hatası: {ex.Message}");
                }
            }

            return Json(new List<object>()); // Hata durumunda boş dizi
        }

        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction("Index");
            }
            var searchResults = await _movieApiService.SearchMoviesAsync(query);
            ViewData["SearchQuery"] = query;
            return View(searchResults);
        }

        // ?? VERİ TABANINA FİLM KAYDEDEN YENİ METODIMIZ
        [HttpPost]
        public async Task<IActionResult> SaveToShelf([FromBody] Movie movie)
        {
            if (movie == null)
            {
                return BadRequest(new { success = false, message = "Film verisi boş geldi!" });
            }

            try
            {
                // Aynı film veri tabanında zaten var mı kontrolü (TMDB ID'sine göre)
                var existingMovie = await _context.Movies.FirstOrDefaultAsync(m => m.TmdbId == movie.TmdbId);

                if (existingMovie != null)
                {
                    return Json(new { success = false, message = $"'{movie.Title}' zaten rafınızda ekli!" });
                }

                // Eğer yoksa veri tabanına ekle
                _context.Movies.Add(movie);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"'{movie.Title}' başarıyla rafa eklendi! ??" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}