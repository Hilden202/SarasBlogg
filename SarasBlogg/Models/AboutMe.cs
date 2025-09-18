using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SarasBlogg.Models
{
    public class AboutMe
    {
        [JsonPropertyName("id")]
        [Key]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        [DisplayName("Titel")]
        public string? Title { get; set; }

        [JsonPropertyName("content")]
        [DisplayName("Innehåll")]
        public string? Content { get; set; }

        [JsonPropertyName("image")]
        [Display(Name = "Bild")]
        public string? Image { get; set; }

        [JsonPropertyName("userId")]
        [DisplayName("Användare")]
        public string UserId { get; set; }

    }
}
