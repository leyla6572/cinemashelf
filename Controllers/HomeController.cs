using cinemashelf.Models;
using CinemaShelf.Data;
using CinemaShelf.Models;
using CinemaShelf.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;


namespace CinemaShelf.Controllers
{
    public class HomeController : Controller
    {
        private readonly MovieApiService _movieApiService;
        private readonly AppDbContext _context; // Veri tabaný merkezimiz

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
                // TMDB ID'lerine göre filmleri asenkron olarak çekiyoruz
                ActionMovies = await _movieApiService.GetMoviesByGenreAsync(28),
                ComedyMovies = await _movieApiService.GetMoviesByGenreAsync(35),
                DramaMovies = await _movieApiService.GetMoviesByGenreAsync(18),
                SciFiMovies = await _movieApiService.GetMoviesByGenreAsync(878)
            };

            return View(viewModel);
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

        // ?? VERÝ TABANINA FÝLM KAYDEDEN YENÝ METODIMIZ
        [HttpPost]
        public async Task<IActionResult> SaveToShelf([FromBody] Movie movie)
        {
            if (movie == null)
            {
                return BadRequest(new { success = false, message = "Film verisi boþ geldi!" });
            }

            try
            {
                // Ayný film veri tabanýnda zaten var mý kontrolü (TMDB ID'sine göre)
                var existingMovie = await _context.Movies.FirstOrDefaultAsync(m => m.TmdbId == movie.TmdbId);

                if (existingMovie != null)
                {
                    return Json(new { success = false, message = $"'{movie.Title}' zaten rafýnýzda ekli!" });
                }

                // Eðer yoksa veri tabanýna ekle
                _context.Movies.Add(movie);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"'{movie.Title}' baþarýyla rafa eklendi! ??" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Hata oluþtu: " + ex.Message });
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