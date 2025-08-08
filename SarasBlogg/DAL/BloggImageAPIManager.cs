using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SarasBlogg.DTOs;

namespace SarasBlogg.DAL
{
    public class BloggImageAPIManager
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BloggImageAPIManager(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
            _http.BaseAddress = new Uri(_config["ApiSettings:BaseAddress"]);
        }

        public async Task<List<BloggImageDto>> GetImagesByBloggIdAsync(int bloggId)
        {
            var response = await _http.GetAsync($"/api/BloggImage/blogg/{bloggId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new List<BloggImageDto>();

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<BloggImageDto>>(_jsonOpts)
                   ?? new List<BloggImageDto>();
        }

        public async Task<BloggImageDto?> UploadImageAsync(IFormFile file, int bloggId)
        {
            using var content = new MultipartFormDataContent();

            var streamContent = new StreamContent(file.OpenReadStream());
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

            // Model binding i API:t är case-insensitive, "file" funkar mot BloggImageUploadDto.File
            content.Add(streamContent, "file", file.FileName);
            content.Add(new StringContent(bloggId.ToString(), Encoding.UTF8), "bloggId");

            var response = await _http.PostAsync("/api/BloggImage/upload", content);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<BloggImageDto>(_jsonOpts);
        }

        public async Task UpdateImageOrderAsync(int bloggId, List<BloggImageDto> images)
        {
            var response = await _http.PutAsJsonAsync($"/api/BloggImage/blogg/{bloggId}/order", images, _jsonOpts);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteImageAsync(int imageId)
        {
            var response = await _http.DeleteAsync($"/api/BloggImage/{imageId}");
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteImagesByBloggIdAsync(int bloggId)
        {
            var response = await _http.DeleteAsync($"/api/BloggImage/blogg/{bloggId}");
            response.EnsureSuccessStatusCode();
        }
    }
}