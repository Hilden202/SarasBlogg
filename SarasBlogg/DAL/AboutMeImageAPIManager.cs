using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SarasBlogg.DAL
{
    public class AboutMeImageAPIManager
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AboutMeImageAPIManager(HttpClient http)
        {
            _http = http;
        }

        public async Task<string?> GetImageUrlAsync()
        {
            var resp = await _http.GetAsync("api/AboutMe/image");
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadFromJsonAsync<Dictionary<string, string?>>(_jsonOpts);
            return json is not null && json.TryGetValue("imageUrl", out var url) ? url : null;
        }

        public async Task<string?> UploadAsync(Stream stream, string fileName, string contentType)
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);

            var resp = await _http.PutAsync("api/AboutMe/image", content);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadFromJsonAsync<Dictionary<string, string?>>(_jsonOpts);
            return json is not null && json.TryGetValue("imageUrl", out var url) ? url : null;
        }

        public async Task DeleteAsync()
        {
            var resp = await _http.DeleteAsync("api/AboutMe/image");
            resp.EnsureSuccessStatusCode();
        }
    }
}
