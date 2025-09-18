using System.Text;
using System.Text.Json;
using SarasBlogg.Models;

namespace SarasBlogg.DAL
{
    public class BloggAPIManager
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BloggAPIManager(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Blogg>> GetAllBloggsAsync()
        {
            var resp = await _httpClient.GetAsync("api/Blogg");
            if (!resp.IsSuccessStatusCode) return new List<Blogg>();

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Blogg>>(json, _jsonOpts) ?? new List<Blogg>();
        }

        public async Task<Blogg?> GetBloggAsync(int id)
        {
            var resp = await _httpClient.GetAsync($"api/Blogg/{id}");
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Blogg>(json, _jsonOpts);
        }

        public async Task<Blogg?> SaveBloggAsync(Blogg blogg)
        {
            var content = new StringContent(JsonSerializer.Serialize(blogg), Encoding.UTF8, "application/json");
            var resp = await _httpClient.PostAsync("api/Blogg", content);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Blogg>(json, _jsonOpts);
        }

        public async Task UpdateBloggAsync(Blogg blogg)
        {
            var content = new StringContent(JsonSerializer.Serialize(blogg), Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"api/Blogg/{blogg.Id}", content);
        }

        public async Task<bool> ToggleHiddenAsync(int id)
        {
            var resp = await _httpClient.PatchAsync($"api/Blogg/{id}/hidden", null);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                throw new HttpRequestException($"PATCH hidden failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
            }
            return true;
        }

        public async Task<bool> ToggleArchivedAsync(int id)
        {
            var resp = await _httpClient.PatchAsync($"api/Blogg/{id}/archived", null);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                throw new HttpRequestException($"PATCH archived failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
            }
            return true;
        }


        public async Task DeleteBloggAsync(int id)
        {
            await _httpClient.DeleteAsync($"api/Blogg/{id}");
        }
    }
}
