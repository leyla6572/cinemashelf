using Microsoft.AspNetCore.Mvc;
using CinemaShelf.Services;
using System.Threading.Tasks;

namespace CinemaShelf.Controllers
{
    public class MovieController : Controller
    {
        private readonly MovieApiService _movieApiService;

        // Constructor ile servisimizi içeri alıyoruz
        public MovieController(MovieApiService movieApiService)
        {
            _movieApiService = movieApiService;
        }

        // URL: /Movie/Details/550
        public async Task<IActionResult> Details(int id)
        {
            var movie = await _movieApiService.GetMovieDetailsAsync(id);

            if (movie == null)
            {
                return NotFound(); // Film bulunamadıysa 404 hatası dönsün
            }

            return View(movie);
        }
    }
}