namespace CinemaShelf.Models
{
    // API'den gelen genel arama sonucunu karşılayan sınıf
    public class TmdbMovieResponse
    {
        public int Page { get; set; }
        public List<TmdbMovieResult> Results { get; set; } = new List<TmdbMovieResult>();
    }

    // API'deki her bir filmin detaylarını karşılayan sınıf
    public class TmdbMovieResult
    {
        public int Id { get; set; } // TMDB'deki benzersiz film ID'si
        public string Title { get; set; } = string.Empty;
        public string? Overview { get; set; } // Film özeti
        public string? Poster_Path { get; set; } // Film afiş uzantısı
        public string? Release_Date { get; set; } // Vizyon tarihi
        public double Vote_Average { get; set; } // Film puanı
    }
}
