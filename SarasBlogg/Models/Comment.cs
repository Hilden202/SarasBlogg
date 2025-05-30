using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SarasBlogg.Models
{
    public class Comment
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("name")]
        [DisplayName("Namn*")]
        [Required(ErrorMessage = "Du behöver ange ett namn")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("content")]
        [DisplayName("Kommentar*")]
        [Required(ErrorMessage = "Du behöver skriva något här")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("bloggId")]
        public int BloggId { get; set; }
    }
}
