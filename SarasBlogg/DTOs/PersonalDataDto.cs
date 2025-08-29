namespace SarasBlogg.DTOs
{
    public sealed class PersonalDataDto
    {
        public Dictionary<string, string?> Data { get; set; } = new();
        public List<string> Roles { get; set; } = new();
        public List<KeyValuePair<string, string>> Claims { get; set; } = new();
    }
}
