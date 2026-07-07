using System;
using System.ComponentModel.DataAnnotations;

namespace CinemaShelf.Models
{
    public class Follow
    {
        [Key]
        public int Id { get; set; }

        // Takip eden kullanıcının ID'si (Örn: Leyla)
        public int FollowerId { get; set; }
        public AppUser Follower { get; set; } = null!;

        // Takip edilen kullanıcının ID'si (Örn: Ahmet)
        public int FollowingId { get; set; }
        public AppUser Following { get; set; } = null!;

        public DateTime FollowedDate { get; set; } = DateTime.Now;
    }
}