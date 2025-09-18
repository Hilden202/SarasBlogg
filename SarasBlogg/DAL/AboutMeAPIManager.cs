using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        // Viktigt: skickar alltid med null istället för att ignorera
        private static readonly JsonSerializerOptions _writeOpts = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
            var json = JsonSerializer.Serialize(aboutMe, _writeOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await _httpClient.PostAsync("api/AboutMe", content);
            resp.EnsureSuccessStatusCode(); // fånga fel direkt
            return null;
        }

        public async Task UpdateAboutMeAsync(AboutMe aboutMe)
        {
            var json = JsonSerializer.Serialize(aboutMe, _writeOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await _httpClient.PutAsync($"api/AboutMe/{aboutMe.Id}", content);
            resp.EnsureSuccessStatusCode(); // nu syns det om API vägrar spara
        }

        public async Task<bool> DeleteAboutMeAsync(int id)
        {
            var resp = await _httpClient.DeleteAsync($"api/AboutMe/{id}");
            return resp.IsSuccessStatusCode;
        }
    }
}
