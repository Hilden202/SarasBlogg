using System.Text;
using System.Text.Json;
using SarasBlogg.Models;

namespace SarasBlogg.DAL
{
    public class BloggAPIManager
    {
        private readonly Uri _baseAddress;
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BloggAPIManager(IConfiguration config)
        {
            _baseAddress = new Uri(config["ApiSettings:BaseAddress"]);
        }

        private HttpClient CreateClient()
        {
            var client = new HttpClient { BaseAddress = _baseAddress };
            return client;
        }

        public async Task<List<Blogg>> GetAllBloggsAsync()
        {
            using var client = CreateClient();
            var resp = await client.GetAsync("api/Blogg");
            if (!resp.IsSuccessStatusCode) return new List<Blogg>();

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Blogg>>(json, _jsonOpts) ?? new List<Blogg>();
        }

        public async Task<Blogg?> GetBloggAsync(int id)
        {
            using var client = CreateClient();
            var resp = await client.GetAsync($"api/Blogg/{id}");
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Blogg>(json, _jsonOpts);
        }

        public async Task<Blogg?> SaveBloggAsync(Blogg blogg)
        {
            using var client = CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(blogg), Encoding.UTF8, "application/json");
            var resp = await client.PostAsync("api/Blogg", content);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Blogg>(json, _jsonOpts);
        }

        public async Task UpdateBloggAsync(Blogg blogg)
        {
            using var client = CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(blogg), Encoding.UTF8, "application/json");
            await client.PutAsync($"api/Blogg/{blogg.Id}", content);
        }

        public async Task DeleteBloggAsync(int id)
        {
            using var client = CreateClient();
            await client.DeleteAsync($"api/Blogg/{id}");
        }
    }
}