using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace SarasBlogg.Models
{
    public class AboutMe
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vänligen ange en titel")]
        [DisplayName("Titel")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Du behöver skriva något här")]
        [DisplayName("Innehåll")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Bild")]
        public string? Image { get; set; }

        [DisplayName("Användare")]
        public string UserId { get; set; } = string.Empty;
    }
}
