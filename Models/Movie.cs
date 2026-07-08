using System.ComponentModel.DataAnnotations;

namespace CinemaShelf.Models
{
    public class Movie
    {
        [Key]
        public int Id { get; set; }

        // TMDB API'den gelen benzersiz film ID'si (Örn: Kara Şövalye için 155)
        [Required]
        public int TmdbId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? PosterPath { get; set; } // Film afişinin resmi (API'den gelecek link)

        // İlişki: Bir film, birçok kullanıcının rafında bulunabilir.
        public ICollection<UserMovie> UserMovies { get; set; } = new List<UserMovie>();

        public List<Review> Reviews { get; set; } = new();
    }
}
