namespace SarasBlogg.DTOs
{
    public class BloggImageDto
    {
        public int Id { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public int BloggId { get; set; }
        public int Order { get; set; }
    }

}