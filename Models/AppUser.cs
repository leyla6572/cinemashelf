using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaShelf.Models
{
    public class AppUser
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçersiz e-posta adresi.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty; // Şifreyi güvenli tutmak için

        // 🌟 SOSYAL PLATFORM İÇİN YENİ EKLENEN ALANLAR
        public string? Bio { get; set; } // Kullanıcı biyografisi
        public string? ProfilePicture { get; set; } = "default-profile.png"; // Varsayılan profil resmi
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // İlişki: Bir kullanıcının rafında birçok film olabilir.
        public ICollection<UserMovie> UserMovies { get; set; } = new List<UserMovie>();

        public List<Review> Reviews { get; set; } = new();
        public ICollection<MovieQuote> MovieQuotes { get; set; } = new List<MovieQuote>();

        // 🌟 TAKİPLEŞME SİSTEMİ NAVIGASYON ÖZELLİKLERİ
        public ICollection<Follow> Followers { get; set; } = new List<Follow>(); // Beni takip edenler
        public ICollection<Follow> Followings { get; set; } = new List<Follow>(); // Benim takip ettiklerim

        
    }

}