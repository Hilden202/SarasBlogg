using System.Text;
using System.Text.Json;
using SarasBlogg.Models;

namespace SarasBlogg.DAL
{
    public class ForbiddenWordAPIManager
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ForbiddenWordAPIManager(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ForbiddenWord>> GetAllAsync()
        {
            var resp = await _httpClient.GetAsync("api/ForbiddenWord");
            if (!resp.IsSuccessStatusCode) return new List<ForbiddenWord>();

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ForbiddenWord>>(json, _jsonOpts) ?? new();
        }

        public async Task<List<string>> GetForbiddenPatternsAsync()
        {
            var forbiddenWords = await GetAllAsync();
            return forbiddenWords.Select(f => f.WordPattern).ToList();
        }

        public async Task<ForbiddenWord?> GetByIdAsync(int id)
        {
            var resp = await _httpClient.GetAsync($"api/ForbiddenWord/{id}");
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ForbiddenWord>(json, _jsonOpts);
        }

        public async Task<string?> SaveAsync(ForbiddenWord word)
        {
            var content = new StringContent(JsonSerializer.Serialize(word), Encoding.UTF8, "application/json");
            var resp = await _httpClient.PostAsync("api/ForbiddenWord", content);
            return resp.IsSuccessStatusCode ? null : await resp.Content.ReadAsStringAsync();
        }

        public async Task<bool> UpdateAsync(ForbiddenWord word)
        {
            var content = new StringContent(JsonSerializer.Serialize(word), Encoding.UTF8, "application/json");
            var resp = await _httpClient.PutAsync($"api/ForbiddenWord/{word.Id}", content);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var resp = await _httpClient.DeleteAsync($"api/ForbiddenWord/{id}");
            return resp.IsSuccessStatusCode;
        }
    }
}
