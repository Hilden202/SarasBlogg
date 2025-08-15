// SarasBlogg/DAL/UserAPIManager.cs
using System.Text.Json;
using System.Net.Http.Json;
using SarasBlogg.DTOs;

namespace SarasBlogg.DAL
{
    public class UserAPIManager
    {
        private readonly HttpClient _http;

        // Läser case-insensitive och skriver camelCase i request
        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public UserAPIManager(HttpClient http)
        {
            _http = http; // BaseAddress sätts i Program.cs
        }

        // ==== AUTH ====
        public async Task<LoginResponse?> LoginAsync(string userOrEmail, string password, bool rememberMe, CancellationToken ct = default)
        {
            var payload = new LoginRequest(userOrEmail, password, rememberMe);
            using var res = await _http.PostAsJsonAsync("api/auth/login", payload, _json, ct);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<LoginResponse>(_json, ct);
        }

        public async Task<bool> LogoutAsync(CancellationToken ct = default)
        {
            try
            {
                using var res = await _http.PostAsync("api/auth/logout", content: null, ct);
                return res.IsSuccessStatusCode; // 200 OK förväntas
            }
            catch
            {
                return false; // best-effort, ignorera nätverksfel
            }
        }

        // ==== USERS/ROLES (oförändrad funktionalitet, använder _http) ====
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
