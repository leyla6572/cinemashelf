using CinemaShelf.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Text.Json;

namespace CinemaShelf.Services
{
    public class MovieApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public MovieApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            // appsettings.json dosyasından API anahtarımızı okuyoruz
            _apiKey = configuration["TmdbSettings:ApiKey"] ?? "";
        }

        // Popüler filmleri getiren metod
        public async Task<List<TmdbMovieResult>> GetPopularMoviesAsync()
        {
            var url = $"movie/popular?api_key={_apiKey}&language=tr-TR&page=1";
            var response = await _httpClient.GetFromJsonAsync<TmdbMovieResponse>(url);
            return response?.Results ?? new List<TmdbMovieResult>();
        }

        // İsme göre film arayan metod
        public async Task<List<TmdbMovieResult>> SearchMoviesAsync(string query)
        {
            var url = $"search/movie?api_key={_apiKey}&query={Uri.EscapeDataString(query)}&language=tr-TR&page=1";
            var response = await _httpClient.GetFromJsonAsync<TmdbMovieResponse>(url);
            return response?.Results ?? new List<TmdbMovieResult>();
        }

        // 🌟 Kategoriye (Türe) Göre Film Getiren Yeni Metod (Hatalardan Arındırıldı)
        public async Task<List<TmdbMovieResult>> GetMoviesByGenreAsync(int genreId)
        {
            // _httpClient zaten Program.cs'te BaseAddress olarak 'https://api.themoviedb.org/3/' almıştır, o yüzden sadece uç noktayı yazıyoruz
            var url = $"discover/movie?api_key={_apiKey}&with_genres={genreId}&language=tr-TR&sort_by=popularity.desc&page=1";

            try
            {
                // Doğru model olan TmdbMovieResponse ile tek satırda veriyi çekiyoruz
                var response = await _httpClient.GetFromJsonAsync<TmdbMovieResponse>(url);
                return response?.Results ?? new List<TmdbMovieResult>();
            }
            catch
            {
                return new List<TmdbMovieResult>();
            }
        }
    }
}