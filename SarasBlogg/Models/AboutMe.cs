using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace SarasBlogg.Models
{
    public class AboutMe
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vänligen ange en titel")]
        [DisplayName("Titel")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Du behöver skriva något här")]
        [DisplayName("Innehåll")]
        public string? Content { get; set; }

        [Display(Name = "Bild")]
        public string? Image { get; set; }

        [Required(ErrorMessage = "Ange ett lanseringsdatum")]
        [DisplayName("Lansering Datum")]
        [DataType(DataType.Date)]
        public string? UserId { get; set; }
    }
}
