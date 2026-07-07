using CinemaShelf.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

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
    }
}
