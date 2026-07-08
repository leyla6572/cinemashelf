using System;

namespace CinemaShelf.Models
{
    public class ReviewLike
    {
        public int Id { get; set; }

        // Beğeniyi yapan kullanıcı
        public int AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        // Beğenilen yorum
        public int ReviewId { get; set; }
        public Review? Review { get; set; }

        public DateTime LikedDate { get; set; } = DateTime.Now;
    }
}