using System;

namespace CinemaShelf.Models
{
    public class MovieQuote
    {
        public int Id { get; set; }

        // Repliğin kendisi
        public string Content { get; set; } = string.Empty;

        // Ne zaman paylaşıldı?
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // İlişkiler: Hangi filmden alındı?
        public int MovieId { get; set; }
        public Movie? Movie { get; set; }

        // İlişkiler: Hangi kullanıcı paylaştı?
        public int AppUserId { get; set; }
        public AppUser? AppUser { get; set; }
    }
}