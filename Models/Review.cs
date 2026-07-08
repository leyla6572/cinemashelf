using System;
using System.ComponentModel.DataAnnotations;

namespace CinemaShelf.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty; // Kullanıcının yorum metni

        [Range(1, 10, ErrorMessage = "Puan 1 ile 10 arasında olmalıdır.")]
        public int Rating { get; set; } // Kullanıcının verdiği 10 üzerinden puan

        public DateTime CreatedDate { get; set; } = DateTime.Now; // Yorumun yapılma tarihi

        // 🔗 İLİŞKİLER (Foreign Keys)

        // Yorumu hangi kullanıcı yaptı?
        public int AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        // Yorum hangi filme yapıldı? (Ortak Movies tablonuza bağlanıyor)
        public int MovieId { get; set; }
        public Movie? Movie { get; set; }

        public List<ReviewLike> ReviewLikes { get; set; } = new();
    }
}