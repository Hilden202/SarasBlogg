using System.Text;
using System.Text.Json;

namespace SarasBlogg.DAL
{
    public class CommentAPIManager
    {
        private readonly HttpClient _httpClient;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public CommentAPIManager(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // --- Nya DTO-baserade anrop (med TopRole) ---

        public async Task<List<DTOs.CommentWithRoleDto>> GetByBloggWithRolesAsync(int bloggId)
        {
            var resp = await _httpClient.GetAsync($"api/Comment/by-blogg/{bloggId}");
            if (!resp.IsSuccessStatusCode) return new List<DTOs.CommentWithRoleDto>();

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<DTOs.CommentWithRoleDto>>(json, _jsonOpts)
                   ?? new List<DTOs.CommentWithRoleDto>();
        }

        public async Task<List<DTOs.CommentWithRoleDto>> GetAllCommentsWithRolesAsync()
        {
            var resp = await _httpClient.GetAsync("api/Comment");
            if (!resp.IsSuccessStatusCode) return new List<DTOs.CommentWithRoleDto>();

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<DTOs.CommentWithRoleDto>>(json, _jsonOpts)
                   ?? new List<DTOs.CommentWithRoleDto>();
        }

        public async Task<DTOs.CommentWithRoleDto?> GetCommentWithRoleAsync(int id)
        {
            var resp = await _httpClient.GetAsync($"api/Comment/ById/{id}");
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DTOs.CommentWithRoleDto>(json, _jsonOpts);
        }

        // --- Bakåtkompatibla metoder (mappar DTO -> gamla modellen) ---

        public async Task<List<Models.Comment>> GetAllCommentsAsync()
        {
            var dtos = await GetAllCommentsWithRolesAsync();
            return dtos.Select(d => new Models.Comment
            {
                Id = d.Id,
                BloggId = d.BloggId,
                Name = d.Name,
                Email = null,              // Exponeras ej publikt
                Content = d.Content ?? "",
                CreatedAt = d.CreatedAt
            }).ToList();
        }

        public async Task<Models.Comment?> GetCommentAsync(int id)
        {
            var d = await GetCommentWithRoleAsync(id);
            if (d == null) return null;

            return new Models.Comment
            {
                Id = d.Id,
                BloggId = d.BloggId,
                Name = d.Name,
                Email = null,
                Content = d.Content ?? "",
                CreatedAt = d.CreatedAt
            };
        }

        // --- Skapa / radera ---

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
