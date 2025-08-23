using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SarasBlogg.Models
{
    public class ContactMe
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        [Required(ErrorMessage = "Vänligen ange ditt namn.")]
        [DisplayName("Namn")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        [Required(ErrorMessage = "Vänligen ange en e-postadress.")]
        [EmailAddress(ErrorMessage = "Vänligen ange en giltig e-postadress.")]
        [DisplayName("E-postadress")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("subject")]
        [Required(ErrorMessage = "Vänligen ange ett ämne.")]
        [DisplayName("Ämne")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        [Required(ErrorMessage = "Vänligen skriv ditt meddelande.")]
        [DisplayName("Meddelande")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
