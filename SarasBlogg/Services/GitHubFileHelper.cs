using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace SarasBlogg.Services
{
    public class GitHubFileHelper : IFileHelper

    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        private readonly string _token;
        private readonly string _userName;
        private readonly string _repository;
        private readonly string _branch;
        private readonly string _uploadFolder;

        public GitHubFileHelper(IConfiguration config)
        {
            _config = config;
            _httpClient = new HttpClient();

            _token = _config["GitHubUpload:Token"];
            _userName = _config["GitHubUpload:UserName"];
            _repository = _config["GitHubUpload:Repository"];
            _branch = _config["GitHubUpload:Branch"];
            _uploadFolder = _config["GitHubUpload:UploadFolder"];

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SarasBloggApp");
        }

        public async Task<string> SaveImageAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                return null;

            var extension = Path.GetExtension(file.FileName); // ".jpg", ".png"
            var fileName = $"{Guid.NewGuid().ToString().Replace("-", "")}{extension}";

            var uploadPath = $"{_uploadFolder}/{fileName}";

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var fileBytes = ms.ToArray();
            var base64Content = Convert.ToBase64String(fileBytes);

            var body = new
            {
                message = $"Upload {fileName} via SarasBlogg",
                content = base64Content,
                branch = _branch
            };

            var json = JsonSerializer.Serialize(body);
            var url = $"https://api.github.com/repos/{_userName}/{_repository}/contents/{uploadPath}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(url, content);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"GitHub upload failed: {response.StatusCode}");

            return $"https://raw.githubusercontent.com/{_userName}/{_repository}/{_branch}/{uploadPath}";
        }

        public async Task DeleteImageAsync(string imageUrl, string folder)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;

            var marker = $"{_uploadFolder}/";
            var start = imageUrl.IndexOf(marker, StringComparison.Ordinal);
            if (start == -1) return;

            var relativePath = imageUrl.Substring(start);

            // 🔹 Hämta SHA
            var shaUrl = $"https://api.github.com/repos/{_userName}/{_repository}/contents/{relativePath}?ref={_branch}";
            var shaResponse = await _httpClient.GetAsync(shaUrl);
            if (!shaResponse.IsSuccessStatusCode) return;

            var jsonDoc = JsonDocument.Parse(await shaResponse.Content.ReadAsStringAsync());
            if (!jsonDoc.RootElement.TryGetProperty("sha", out var shaProp)) return;

            var sha = shaProp.GetString();

            var body = new
            {
                message = $"Delete {relativePath} via SarasBlogg",
                sha = sha,
                branch = _branch
            };

            var json = JsonSerializer.Serialize(body);
            var deleteContent = new StringContent(json, Encoding.UTF8, "application/json");

            var deleteUrl = $"https://api.github.com/repos/{_userName}/{_repository}/contents/{relativePath}";
            var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, deleteUrl)
            {
                Content = deleteContent
            };

            var deleteResponse = await _httpClient.SendAsync(deleteRequest);
            if (!deleteResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"[GitHub] Failed to delete image: {deleteResponse.StatusCode}");
            }
        }
    }
}
