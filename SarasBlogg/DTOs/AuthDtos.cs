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
    public sealed class RegisterRequest
    {
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public sealed class BasicResultDto
    {
        public bool Succeeded { get; set; }
        public string? Message { get; set; }
        public string? ConfirmEmailUrl { get; set; }
    }
    public sealed class ConfirmEmailRequestDto
    {
        public string UserId { get; set; } = "";
        public string Code { get; set; } = "";
    }
    public record EmailDto(string Email);

    public record ResetPasswordDto(string UserId, string Token, string NewPassword);

}
