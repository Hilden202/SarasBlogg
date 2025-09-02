namespace SarasBlogg.ViewModels
{
    public class BloggViewModel
    {
        public List<Models.Blogg>? Bloggs { get; set; }
        public Models.Blogg? Blogg { get; set; }
        public bool IsArchiveView { get; set; } = false;
        public List<Models.Comment>? Comments { get; set; }
        public Models.Comment? Comment { get; set; }
        public string RoleSymbol { get; set; } = "";
        public string RoleCss { get; set; } = "";
        public HashSet<int> VerifiedCommentIds { get; set; } = new();

        // Extra dictionaries for role symbols and CSS classes
        public Dictionary<string, string> RoleCssByName { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        //public Dictionary<string, string> RoleSymbolByName { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    }
}
