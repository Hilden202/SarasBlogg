using System.Text;
using System.Text.Json;
using SarasBlogg.Models;

namespace SarasBlogg.DAL
{
    public class AboutMeAPIManager
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AboutMeAPIManager(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<AboutMe?> GetAboutMeAsync()
        {
            var resp = await _httpClient.GetAsync("api/AboutMe");
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AboutMe>(json, _jsonOpts);
        }

        public async Task<string?> SaveAboutMeAsync(AboutMe aboutMe)
        {
            var content = new StringContent(JsonSerializer.Serialize(aboutMe), Encoding.UTF8, "application/json");
            var resp = await _httpClient.PostAsync("api/AboutMe", content);
            return resp.IsSuccessStatusCode ? null : await resp.Content.ReadAsStringAsync();
        }

        public async Task UpdateAboutMeAsync(AboutMe aboutMe)
        {
            var content = new StringContent(JsonSerializer.Serialize(aboutMe), Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"api/AboutMe/{aboutMe.Id}", content);
        }

        public async Task<bool> DeleteAboutMeAsync(int id)
        {
            var resp = await _httpClient.DeleteAsync($"api/AboutMe/{id}");
            return resp.IsSuccessStatusCode;
        }
    }
}
