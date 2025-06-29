using System.Text.Json;
using SarasBlogg.Models;
using Microsoft.Extensions.Configuration;

namespace SarasBlogg.DAL
{
    public class BloggAPIManager
    {
        private readonly Uri _baseAddress;

        public BloggAPIManager(IConfiguration config)
        {
            _baseAddress = new Uri(config["ApiSettings:BaseAddress"]);
        }

        public async Task<List<Blogg>> GetAllBloggsAsync()
        {
            List<Blogg> bloggs = new();

            using (var client = new HttpClient())
            {
                client.BaseAddress = _baseAddress;
                var response = await client.GetAsync("api/Blogg");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    bloggs = JsonSerializer.Deserialize<List<Blogg>>(json);
                }
                return bloggs;
            }
        }

        public async Task<Blogg?> GetBloggAsync(int id)
        {
            Blogg? blogg = null;
            using (var client = new HttpClient())
            {
                client.BaseAddress = _baseAddress;
                var response = await client.GetAsync($"api/Blogg/{id}");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    blogg = JsonSerializer.Deserialize<Blogg>(json);
                }
                return blogg;
            }
        }

        public async Task<string?> SaveBloggAsync(Blogg blogg)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = _baseAddress;
                var json = JsonSerializer.Serialize(blogg);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/Blogg", content);

                return response.IsSuccessStatusCode ? null : await response.Content.ReadAsStringAsync();
            }
        }

        public async Task UpdateBloggAsync(Blogg blogg)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = _baseAddress;
                var json = JsonSerializer.Serialize(blogg);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                await client.PutAsync($"api/Blogg/{blogg.Id}", content);
            }
        }

        public async Task DeleteBloggAsync(int id)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = _baseAddress;
                await client.DeleteAsync($"api/Blogg/{id}");
            }
        }
    }
}
