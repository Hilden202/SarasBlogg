using System.Text.Json.Serialization;

namespace SarasBlogg.Models
{
    public class ForbiddenWord
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("wordPattern")]
        public string WordPattern { get; set; } = string.Empty;
    }
}
