namespace SarasBlogg.DTOs
{
    public sealed class PersonalDataDto
    {
        public Dictionary<string, string?> Data { get; set; } = new();
        public List<string> Roles { get; set; } = new();
        public List<KeyValuePair<string, string>> Claims { get; set; } = new();

        public int CommentsCount { get; set; }
        public int LikesCount { get; set; }
        public List<CommentPreviewDto>? Comments { get; set; }
        public List<LikePreviewDto>? Likes { get; set; }
    }

    public sealed class CommentPreviewDto
    {
        public int Id { get; set; }
        public int BloggId { get; set; }
        public string BloggTitle { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public sealed class LikePreviewDto
    {
        public int Id { get; set; }
        public int BloggId { get; set; }
        public string BloggTitle { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
