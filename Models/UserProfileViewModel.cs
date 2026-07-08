using System.Collections.Generic;

namespace CinemaShelf.Models
{
    public class UserProfileViewModel
    {
        public int TotalMovies { get; set; }
        public string FavoriteGenre { get; set; } = "Belirsiz";
        public List<UserShelfItem> ShelfMovies { get; set; } = new(); // Rafındaki filmlerin listesi
    }

    // Eğer projedeki raf modelinin adı farklıysa (örn: Movie veya Shelf), onu buraya bağlayacağız
    public class UserShelfItem
    {
        public int Id { get; set; }
        public int TmdbId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PosterPath { get; set; } = string.Empty;
    }
}
