using System;
using System.Collections.Generic;

namespace CinemaShelf.Models
{
    public class TmdbMovieDetailResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Overview { get; set; } = string.Empty;
        public string Poster_Path { get; set; } = string.Empty;
        public string Backdrop_Path { get; set; } = string.Empty; // Arka plan görseli
        public string Release_Date { get; set; } = string.Empty;
        public double Vote_Average { get; set; }
        public int Runtime { get; set; } // Film süresi (dakika)
        public long Budget { get; set; } // Bütçe
        public List<GenreModel> Genres { get; set; } = new();
    }

    public class GenreModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
