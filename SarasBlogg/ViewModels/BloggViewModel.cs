using Microsoft.AspNetCore.Mvc;
using SarasBlogg.Models;

namespace SarasBlogg.ViewModels
{
    public class BloggViewModel
    {
        public List<Models.Blogg>? Bloggs { get; set; }
        public Models.Blogg? Blogg { get; set; }
        public bool IsArchiveView { get; set; } = false;
        public List<Models.Comment>? Comments { get; set; }
        public Comment? Comment { get; set; }
    }
}
