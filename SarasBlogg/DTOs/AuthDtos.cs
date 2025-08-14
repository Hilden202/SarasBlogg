namespace SarasBlogg.DTOs
{
    public record LoginRequest(string UserNameOrEmail, string Password, bool RememberMe);

    public class LoginResponse
    {
        public string AccessToken { get; set; } = "";
        public DateTime AccessTokenExpiresUtc { get; set; }
        public string RefreshToken { get; set; } = "";
        public DateTime RefreshTokenExpiresUtc { get; set; }
    }
}
