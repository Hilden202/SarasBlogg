using System.Text.Json;
using SarasBlogg.Models;

namespace SarasBlogg.DAL
{
    public class ContactMeAPIManager
    {
        private readonly Uri _baseAddress;

        public ContactMeAPIManager(IConfiguration config)
        {
            _baseAddress = new Uri(config["ApiSettings:BaseAddress"]);
        }

        public async Task<List<ContactMe>> GetAllMessagesAsync()
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            var response = await client.GetAsync("api/ContactMe");

            if (!response.IsSuccessStatusCode)
                return new List<ContactMe>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ContactMe>>(json) ?? new List<ContactMe>();
        }

        public async Task<string?> SaveMessageAsync(ContactMe contact)
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            var json = JsonSerializer.Serialize(contact);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/ContactMe", content);
            return response.IsSuccessStatusCode ? null : await response.Content.ReadAsStringAsync();
        }

        public async Task DeleteMessageAsync(int id)
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            await client.DeleteAsync($"api/ContactMe/{id}");
        }
    }
}
