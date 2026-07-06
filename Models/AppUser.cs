using System.ComponentModel.DataAnnotations;

namespace CinemaShelf.Models
{
    public class AppUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty; // Şifreyi güvenli tutmak için

        // İlişki: Bir kullanıcının rafında birçok film olabilir.
        public ICollection<UserMovie> UserMovies { get; set; } = new List<UserMovie>();
    }
}