namespace SarasBlogg.DTOs
{
    public class LoginRequest
    {
        public string UserNameOrEmail { get; set; } = "";
        public string Password { get; set; } = "";
        public bool RememberMe { get; set; }
    }
}