using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SarasBlogg.Models
{
    public class Blogg
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vänligen ange en titel")]
        [DisplayName("Titel")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Du behöver skriva något här")]
        [DisplayName("Innehåll")]
        public string? Content { get; set; }
        [Display(Name = "Författare")]
        public string? Author { get; set; }
        [Display(Name = "Bild")]
        public string? Image { get; set; }

        [Required(ErrorMessage = "Ange ett lanseringsdatum")]
        [DisplayName("Lansering Datum")]
        [DataType(DataType.Date)]
        public DateTime LaunchDate { get; set; }

        public bool IsArchived { get; set; } = false;

        public bool Hidden { get; set; } = false;
    }
}
