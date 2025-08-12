using System.Text;
using System.Text.Json;
using SarasBlogg.Models;

namespace SarasBlogg.DAL
{
    public class ContactMeAPIManager
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ContactMeAPIManager(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ContactMe>> GetAllMessagesAsync()
        {
            var resp = await _httpClient.GetAsync("api/ContactMe");
            if (!resp.IsSuccessStatusCode) return new List<ContactMe>();

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ContactMe>>(json, _jsonOpts) ?? new List<ContactMe>();
        }

        public async Task<string?> SaveMessageAsync(ContactMe contact)
        {
            var content = new StringContent(JsonSerializer.Serialize(contact), Encoding.UTF8, "application/json");
            var resp = await _httpClient.PostAsync("api/ContactMe", content);
            return resp.IsSuccessStatusCode ? null : await resp.Content.ReadAsStringAsync();
        }

        public async Task DeleteMessageAsync(int id)
        {
            await _httpClient.DeleteAsync($"api/ContactMe/{id}");
        }
    }
}
