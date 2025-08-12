using System.Text;
using System.Text.Json;

namespace SarasBlogg.DAL
{
    public class CommentAPIManager
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public CommentAPIManager(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Models.Comment>> GetAllCommentsAsync()
        {
            var resp = await _httpClient.GetAsync("api/Comment");
            if (!resp.IsSuccessStatusCode) return new List<Models.Comment>();

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Models.Comment>>(json, _jsonOpts) ?? new List<Models.Comment>();
        }

        public async Task<Models.Comment?> GetCommentAsync(int id)
        {
            var resp = await _httpClient.GetAsync($"api/Comment/ById/{id}");
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Models.Comment>(json, _jsonOpts);
        }

        public async Task<string?> SaveCommentAsync(Models.Comment comment)
        {
            var content = new StringContent(JsonSerializer.Serialize(comment), Encoding.UTF8, "application/json");
            var resp = await _httpClient.PostAsync("api/Comment", content);

            if (resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadAsStringAsync();
        }

        public async Task DeleteCommentAsync(int id)
        {
            await _httpClient.DeleteAsync($"api/Comment/ById/{id}");
        }

        public async Task DeleteCommentsAsync(int bloggId)
        {
            await _httpClient.DeleteAsync($"api/Comment/ByBlogg/{bloggId}");
        }
    }
}
