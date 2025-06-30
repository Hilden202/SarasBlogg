using System.Text.Json;
using SarasBlogg.Models;
using Microsoft.Extensions.Configuration;

namespace SarasBlogg.DAL
{
    public class ForbiddenWordAPIManager
    {
        private readonly Uri _baseAddress;

        public ForbiddenWordAPIManager(IConfiguration config)
        {
            _baseAddress = new Uri(config["ApiSettings:BaseAddress"]);
        }

        public async Task<List<ForbiddenWord>> GetAllAsync()
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            var response = await client.GetAsync("api/ForbiddenWord");

            if (!response.IsSuccessStatusCode) return new List<ForbiddenWord>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ForbiddenWord>>(json) ?? new();
        }
        public async Task<List<string>> GetForbiddenPatternsAsync()
        {
            var forbiddenWords = await GetAllAsync();
            return forbiddenWords.Select(f => f.WordPattern).ToList();
        }

        public async Task<ForbiddenWord?> GetByIdAsync(int id)
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            var response = await client.GetAsync($"api/ForbiddenWord/{id}");

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ForbiddenWord>(json);
        }

        public async Task<string?> SaveAsync(ForbiddenWord word)
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            var json = JsonSerializer.Serialize(word);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/ForbiddenWord", content);
            return response.IsSuccessStatusCode ? null : await response.Content.ReadAsStringAsync();
        }

        public async Task<bool> UpdateAsync(ForbiddenWord word)
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            var json = JsonSerializer.Serialize(word);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"api/ForbiddenWord/{word.Id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            var response = await client.DeleteAsync($"api/ForbiddenWord/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
