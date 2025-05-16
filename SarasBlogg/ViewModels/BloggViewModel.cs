namespace SarasBlogg.ViewModels
{
    public class BloggViewModel
    {
        public List<Models.Blogg> Bloggs { get; set; }
        public Models.Blogg Blogg { get; set; }
        public bool IsArchiveView { get; set; } = false;

        public List<Models.Comment> AllComments { get; set; }
    }
}
