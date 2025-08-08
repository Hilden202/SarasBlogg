using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SarasBlogg.DTOs;

namespace SarasBlogg.Models
{
    public class Blogg
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        [Required(ErrorMessage = "Vänligen ange en titel")]
        [DisplayName("Titel")]
        public string? Title { get; set; }

        [JsonPropertyName("content")]
        [Required(ErrorMessage = "Du behöver skriva något här")]
        [DisplayName("Innehåll")]
        public string? Content { get; set; }

        [JsonPropertyName("author")]
        [Display(Name = "Författare")]
        [Required(ErrorMessage = "Ange författare")]
        public string? Author { get; set; }

        [JsonPropertyName("launchDate")]
        [Required(ErrorMessage = "Du måste välja ett lanseringsdatum")]
        [DisplayName("Lansering Datum")]
        [DataType(DataType.Date)]
        public DateTime LaunchDate { get; set; }

        [JsonPropertyName("isArchived")]
        public bool IsArchived { get; set; } = false;

        [JsonPropertyName("viewCount")]
        public int ViewCount { get; set; }

        [JsonPropertyName("hidden")]
        public bool Hidden { get; set; } = false;

        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        [JsonIgnore]
        public List<BloggImageDto>? Images { get; set; }

    }
}
