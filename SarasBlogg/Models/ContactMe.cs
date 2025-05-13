using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace SarasBlogg.Models
{
    public class ContactMe
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vänligen ange ditt namn.")]
        [DisplayName("Namn")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vänligen ange en e-postadress.")]
        [EmailAddress(ErrorMessage = "Vänligen ange en giltig e-postadress.")]
        [DisplayName("E-postadress")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vänligen ange ett ämne.")]
        [DisplayName("Ämne")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vänligen skriv ditt meddelande.")]
        [DisplayName("Meddelande")]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
