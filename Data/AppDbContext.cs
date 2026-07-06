using CinemaShelf.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CinemaShelf.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // SQL Server'da oluşacak tablolarımız
        public DbSet<Movie> Movies { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<UserMovie> UserMovies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserMovie (Ara Tablo) ilişkilerini ve kurallarını netleştiriyoruz
            modelBuilder.Entity<UserMovie>()
                .HasOne(um => um.AppUser)
                .WithMany(u => u.UserMovies)
                .HasForeignKey(um => um.AppUserId);

            modelBuilder.Entity<UserMovie>()
                .HasOne(um => um.Movie)
                .WithMany(m => m.UserMovies)
                .HasForeignKey(um => um.MovieId);

            // Enum yapısının veri tabanına string (metin) olarak kaydedilmesi için (Örn: "Watched", "Watchlist")
            modelBuilder.Entity<UserMovie>()
                .Property(um => um.Status)
                .HasConversion<string>();
        }
    }
}
