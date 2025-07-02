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
        [Required(ErrorMessage = "Vänligen ange en titel")]
        [DisplayName("Titel")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        [Required(ErrorMessage = "Du behöver skriva något här")]
        [DisplayName("Innehåll")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("image")]
        [Display(Name = "Bild")]
        public string? Image { get; set; }

        [JsonPropertyName("userId")]
        [DisplayName("Användare")]
        public string UserId { get; set; } = string.Empty;

    }
}
