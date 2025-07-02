using System.Text.Json;
using SarasBlogg.Models;
using Microsoft.Extensions.Configuration;

namespace SarasBlogg.DAL
{
    public class AboutMeAPIManager
    {
        private readonly Uri _baseAddress;

        public AboutMeAPIManager(IConfiguration config)
        {
            _baseAddress = new Uri(config["ApiSettings:BaseAddress"]);
        }

        public async Task<AboutMe?> GetAboutMeAsync()
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            var response = await client.GetAsync("api/AboutMe");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AboutMe>(json);
        }

        public async Task<string?> SaveAboutMeAsync(AboutMe aboutMe)
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            var json = JsonSerializer.Serialize(aboutMe);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/AboutMe", content);
            return response.IsSuccessStatusCode ? null : await response.Content.ReadAsStringAsync();
        }

        public async Task UpdateAboutMeAsync(AboutMe aboutMe)
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            var json = JsonSerializer.Serialize(aboutMe);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            await client.PutAsync($"api/AboutMe/{aboutMe.Id}", content);
        }

        public async Task<bool> DeleteAboutMeAsync(int id)
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            var response = await client.DeleteAsync($"api/AboutMe/{id}");
            return response.IsSuccessStatusCode;
        }

    }
}
