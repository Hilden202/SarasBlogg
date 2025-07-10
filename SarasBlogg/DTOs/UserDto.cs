namespace SarasBlogg.DTOs
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
}
