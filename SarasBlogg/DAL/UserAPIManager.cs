using System.Text.Json;
using SarasBlogg.DTOs;

namespace SarasBlogg.DAL
{
    public class UserAPIManager
    {
        private readonly Uri _baseAddress;

        public UserAPIManager(IConfiguration config)
        {
            _baseAddress = new Uri(config["ApiSettings:BaseAddress"]);
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            var response = await client.GetAsync("api/User/all");

            if (!response.IsSuccessStatusCode)
                return new List<UserDto>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<UserDto>>(json) ?? new List<UserDto>();
        }

        public async Task<UserDto?> GetUserByIdAsync(string id)
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            var response = await client.GetAsync($"api/User/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserDto>(json);
        }

        public async Task<List<string>> GetUserRolesAsync(string id)
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            var response = await client.GetAsync($"api/User/{id}/roles");

            if (!response.IsSuccessStatusCode)
                return new List<string>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        public async Task<bool> AddUserToRoleAsync(string id, string roleName)
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            var response = await client.PostAsync($"api/User/{id}/add-role/{roleName}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RemoveUserFromRoleAsync(string id, string roleName)
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            var response = await client.DeleteAsync($"api/User/{id}/remove-role/{roleName}");
            return response.IsSuccessStatusCode;
        }

        public async Task CreateRoleAsync(string roleName)
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            await client.PostAsync($"api/Role/create/{roleName}", null);
        }

        public async Task<List<string>> GetAllRolesAsync()
        {
            using var client = new HttpClient { BaseAddress = _baseAddress };
            var response = await client.GetAsync("api/Role/all");

            if (!response.IsSuccessStatusCode)
                return new List<string>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }


    }
}
