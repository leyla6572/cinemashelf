using System.ComponentModel.DataAnnotations;

namespace CinemaShelf.Models
{
    // Kullanıcının filmi hangi rafa koyduğunu belirten enum yapısı
    public enum WatchStatus
    {
        Watched,   // İzlediklerim
        Watchlist  // İzleyeceklerim
    }

    public class UserMovie
    {
        [Key]
        public int Id { get; set; }

        // Hangi kullanıcı?
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        // Hangi film?
        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;

        // Hangi rafta duruyor? (İzledim mi, İzleyecek miyim?)
        [Required]
        public WatchStatus Status { get; set; }

        // Kullanıcının bu filme yaptığı kişisel yorum
        [StringLength(1000)]
        public string? UserComment { get; set; }

        // Kullanıcının bu filme verdiği puan (Örn: 1 ile 5 arası yıldız)
        public int? Rating { get; set; }

        public DateTime AddedDate { get; set; } = DateTime.Now;
    }
}
