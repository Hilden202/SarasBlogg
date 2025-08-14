using System.Text;
using System.Text.Json;
using NuGet.Protocol.Plugins;
using SarasBlogg.DTOs;

namespace SarasBlogg.DAL
{
    public class UserAPIManager
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        public UserAPIManager(HttpClient http)
        {
            _http = http; // BaseAddress sätts i Program.cs
        }

        // ==== AUTH ====
        public async Task<LoginResponse?> LoginAsync(string userOrEmail, string password, bool rememberMe)
        {
            var payload = new
            {
                userNameOrEmail = userOrEmail,
                password = password,
                rememberMe = rememberMe
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await _http.PostAsync("api/auth/login", content);
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LoginResponse>(json, _json);
        }

        // ==== USERS/ROLES (oförändrad funktionalitet, men använder _http istället för new HttpClient) ====
        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var response = await _http.GetAsync("api/User/all");
            if (!response.IsSuccessStatusCode) return new List<UserDto>();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<UserDto>>(json, _json) ?? new List<UserDto>();
        }

        public async Task<UserDto?> GetUserByIdAsync(string id)
        {
            var response = await _http.GetAsync($"api/User/{id}");
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserDto>(json, _json);
        }

        public async Task<List<string>> GetUserRolesAsync(string id)
        {
            var response = await _http.GetAsync($"api/User/{id}/roles");
            if (!response.IsSuccessStatusCode) return new List<string>();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(json, _json) ?? new List<string>();
        }

        public async Task<bool> AddUserToRoleAsync(string id, string roleName)
        {
            var response = await _http.PostAsync($"api/User/{id}/add-role/{roleName}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RemoveUserFromRoleAsync(string id, string roleName)
        {
            var response = await _http.DeleteAsync($"api/User/{id}/remove-role/{roleName}");
            return response.IsSuccessStatusCode;
        }

        public async Task CreateRoleAsync(string roleName)
        {
            await _http.PostAsync($"api/Role/create/{roleName}", null);
        }

        public async Task<List<string>> GetAllRolesAsync()
        {
            var response = await _http.GetAsync("api/Role/all");
            if (!response.IsSuccessStatusCode) return new List<string>();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(json, _json) ?? new List<string>();
        }

        public async Task<bool> DeleteUserAsync(string id)
        {
            var response = await _http.DeleteAsync($"api/User/delete/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteRoleAsync(string roleName)
        {
            var response = await _http.DeleteAsync($"api/Role/delete/{roleName}");
            return response.IsSuccessStatusCode;
        }
    }
}
