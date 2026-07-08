using System.Collections.Generic;

namespace CinemaShelf.Models
{
    public class HomeViewModel
    {
        public List<TmdbMovieResult> ActionMovies { get; set; } = new();
        public List<TmdbMovieResult> ComedyMovies { get; set; } = new();
        public List<TmdbMovieResult> DramaMovies { get; set; } = new();
        public List<TmdbMovieResult> SciFiMovies { get; set; } = new();
    }
}
