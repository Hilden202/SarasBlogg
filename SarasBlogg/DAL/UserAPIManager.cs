// SarasBlogg/DAL/UserAPIManager.cs
using System.Text.Json;
using System.Net.Http.Json;
using SarasBlogg.DTOs;
using static System.Net.WebRequestMethods;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

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

        public async Task<BasicResultDto?> ChangeUserNameAsync(string userId, string newUserName, CancellationToken ct = default)
        {
            var payload = new ChangeUserNameRequestDto { NewUserName = newUserName };
            using var res = await _http.PutAsJsonAsync($"api/User/{userId}/username", payload, _json, ct);
            return await res.Content.ReadFromJsonAsync<BasicResultDto>(_json, ct);
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
        public async Task<BasicResultDto?> RegisterAsync(
            string userName, string email, string password,
           string? name = null, int? birthYear = null,
            bool subscribeNewPosts = false,
            CancellationToken ct = default)
        {
            var payload = new RegisterRequest
            {
                UserName = userName,
                Email = email,
                Password = password,
                Name = name,
                BirthYear = birthYear,
                SubscribeNewPosts = subscribeNewPosts
            }
            ;
            using var res = await _http.PostAsJsonAsync("api/auth/register", payload, _json, ct);

            // Läs råtext en gång
            var raw = await res.Content.ReadAsStringAsync(ct);

            // Försök först JSON
            try
            {
                var dto = System.Text.Json.JsonSerializer.Deserialize<BasicResultDto>(raw, _json);
                if (dto != null) return dto;
            }
            catch { /* fall back to text */ }

            // Fallback: bygg ett BasicResultDto från texten/statuskoden
            return new BasicResultDto
            {
                Succeeded = res.IsSuccessStatusCode,
                Message = string.IsNullOrWhiteSpace(raw) ? $"HTTP {(int)res.StatusCode}" : raw
            };
        }

        public async Task<BasicResultDto?> ConfirmEmailAsync(string userId, string code, CancellationToken ct = default)
        {
            var payload = new ConfirmEmailRequestDto { UserId = userId, Code = code };
            using var res = await _http.PostAsJsonAsync("api/auth/confirm-email", payload, _json, ct);
            var body = await res.Content.ReadFromJsonAsync<BasicResultDto>(_json, ct);
            return body ?? new BasicResultDto { Succeeded = res.IsSuccessStatusCode, Message = res.ReasonPhrase };
        }
        public async Task<UserDto?> GetUserByUserNameAsync(string username)
        {
            var response = await _http.GetAsync($"/api/users/by-username/{username}");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<UserDto>(_json);
        }
        public async Task<BasicResultDto?> ResendConfirmationAsync(string email, CancellationToken ct = default)
        {
            var payload = new EmailDto(email);
            using var res = await _http.PostAsJsonAsync("api/auth/resend-confirmation", payload, _json, ct);
            var body = await res.Content.ReadFromJsonAsync<BasicResultDto>(_json, ct);
            return body ?? new BasicResultDto { Succeeded = res.IsSuccessStatusCode, Message = res.ReasonPhrase };
        }

        public async Task<BasicResultDto?> ForgotPasswordAsync(string email, CancellationToken ct = default)
        {
            var payload = new EmailDto(email);
            using var res = await _http.PostAsJsonAsync("api/auth/forgot-password", payload, _json, ct);
            var body = await res.Content.ReadFromJsonAsync<BasicResultDto>(_json, ct);
            return body ?? new BasicResultDto { Succeeded = res.IsSuccessStatusCode, Message = res.ReasonPhrase };
        }

        public async Task<BasicResultDto?> ResetPasswordAsync(string userId, string token, string newPassword, CancellationToken ct = default)
        {
            var payload = new ResetPasswordDto(userId, token, newPassword);
            using var res = await _http.PostAsJsonAsync("api/auth/reset-password", payload, _json, ct);
            var body = await res.Content.ReadFromJsonAsync<BasicResultDto>(_json, ct);
            return body ?? new BasicResultDto { Succeeded = res.IsSuccessStatusCode, Message = res.ReasonPhrase };
        }

        public async Task<BasicResultDto?> ChangeMyUserNameAsync(string newUserName, CancellationToken ct = default)
        {
            var payload = new ChangeUserNameRequestDto { NewUserName = newUserName };
            using var res = await _http.PutAsJsonAsync("api/User/me/username", payload, _json, ct);
            return await res.Content.ReadFromJsonAsync<BasicResultDto>(_json, ct)
                ?? new BasicResultDto { Succeeded = res.IsSuccessStatusCode, Message = res.ReasonPhrase };
        }

        public async Task<UserDto?> GetMeAsync(CancellationToken ct = default)
        {
            using var res = await _http.GetAsync("api/users/me", ct); // <-- ny route
            if (!res.IsSuccessStatusCode) return null;

            var me = await res.Content.ReadFromJsonAsync<MeResponse>(_json, ct);
            if (me is null) return null;

            return new UserDto
            {
                Id = me.Id,
                UserName = me.UserName,
                Email = me.Email,
                Roles = me.Roles?.ToList() ?? new List<string>(),
                Name = me.Name,
                BirthYear = me.BirthYear,
                PhoneNumber = me.PhoneNumber,
                EmailConfirmed = me.EmailConfirmed,
                NotifyOnNewPost = me.NotifyOnNewPost
            };
        }


        public async Task<BasicResultDto?> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)
        {
            var dto = new ChangePasswordDto { CurrentPassword = currentPassword, NewPassword = newPassword };
            using var res = await _http.PostAsJsonAsync("api/auth/change-password", dto, ct);
            if (!res.IsSuccessStatusCode)
            {
                // Försök ändå läsa ev. BasicResultDto med felmeddelande
                var err = await res.Content.ReadFromJsonAsync<BasicResultDto>(cancellationToken: ct);
                return err ?? new BasicResultDto { Succeeded = false, Message = $"HTTP {(int)res.StatusCode}" };
            }
            return await res.Content.ReadFromJsonAsync<BasicResultDto>(cancellationToken: ct);
        }
        public async Task<BasicResultDto?> SetPasswordAsync(string newPassword, CancellationToken ct = default)
        {
            var dto = new SetPasswordDto { NewPassword = newPassword };
            using var res = await _http.PostAsJsonAsync("api/auth/set-password", dto, ct);
            var body = await res.Content.ReadFromJsonAsync<BasicResultDto>(cancellationToken: ct);
            if (!res.IsSuccessStatusCode) return body ?? new BasicResultDto { Succeeded = false, Message = $"HTTP {(int)res.StatusCode}" };
            return body;
        }

        public Task<BasicResultDto?> ChangeEmailStartAsync(string newEmail, CancellationToken ct = default)
             => _http.PostAsJsonAsync("api/auth/change-email/start", new ChangeEmailStartDto { NewEmail = newEmail }, ct)
            .ContinueWith(async t => (await t.Result.Content.ReadFromJsonAsync<BasicResultDto>(cancellationToken: ct))!, ct).Unwrap();

        public Task<BasicResultDto?> ChangeEmailConfirmAsync(string userId, string code, string newEmail, CancellationToken ct = default)
            => _http.PostAsJsonAsync($"api/auth/change-email/confirm?newEmail={Uri.EscapeDataString(newEmail)}",
            new ChangeEmailConfirmDto { UserId = userId, Code = code }, ct)
            .ContinueWith(async t => (await t.Result.Content.ReadFromJsonAsync<BasicResultDto>(cancellationToken: ct))!, ct).Unwrap();

        // ==== PROFILE ====
        public async Task<BasicResultDto?> UpdateMyProfileAsync(
            string? phoneNumber, string? name, int? birthYear,
            bool? notifyOnNewPost = null,
            CancellationToken ct = default)
        {
            var payload = new UpdateProfileDto
            {
                PhoneNumber = phoneNumber,
                Name = name,
                BirthYear = birthYear,
                NotifyOnNewPost = notifyOnNewPost
            };

            using var res = await _http.PutAsJsonAsync("api/User/me/profile", payload, _json, ct);
            var body = await res.Content.ReadFromJsonAsync<BasicResultDto>(_json, ct);
            if (!res.IsSuccessStatusCode)
                return body ?? new BasicResultDto { Succeeded = false, Message = $"HTTP {(int)res.StatusCode}" };
            return body;
        }


        public async Task<PersonalDataDto?> GetMyPersonalDataAsync(CancellationToken ct = default)
        {
            using var res = await _http.GetAsync("api/User/me/personal-data", ct);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<PersonalDataDto>(_json, ct);
        }

        public async Task<(byte[]? bytes, string filename, string contentType)> DownloadMyPersonalDataAsync(CancellationToken ct = default)
        {
            using var res = await _http.GetAsync("api/User/me/personal-data/download", ct);
            if (!res.IsSuccessStatusCode) return (null, "", "");
            var bytes = await res.Content.ReadAsByteArrayAsync(ct);
            var ctType = res.Content.Headers.ContentType?.ToString() ?? "application/json";
            return (bytes, "PersonalData.json", ctType);
        }

        public async Task<BasicResultDto?> DeleteMeAsync(string? password, CancellationToken ct = default)
        {
            var payload = new { password };
            var req = new HttpRequestMessage(HttpMethod.Delete, "api/User/me")
            { Content = JsonContent.Create(payload) };
            using var res = await _http.SendAsync(req, ct);
            return await res.Content.ReadFromJsonAsync<BasicResultDto>(_json, ct)
                   ?? new BasicResultDto { Succeeded = res.IsSuccessStatusCode, Message = res.ReasonPhrase };
        }
        
        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var response = await _http.GetAsync($"api/users/by-email/{Uri.EscapeDataString(email)}");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<UserDto>(_json);
        }
        
        public async Task<BasicResultDto?> SendResetLinkAsync(string email, CancellationToken ct = default)
        {
            var payload = new EmailDto(email);
            using var res = await _http.PostAsJsonAsync("api/auth/send-reset-link", payload, _json, ct);

            // Försök läsa som BasicResultDto direkt
            var body = await res.Content.ReadFromJsonAsync<BasicResultDto>(_json, ct);

            // Om inget kommer tillbaka (t.ex. null) → skapa fallback
            if (body == null)
            {
                var raw = await res.Content.ReadAsStringAsync(ct);
                return new BasicResultDto
                {
                    Succeeded = res.IsSuccessStatusCode,
                    Message = string.IsNullOrWhiteSpace(raw)
                        ? $"HTTP {(int)res.StatusCode}"
                        : raw
                };
            }

            return body;
        }
    }
}
