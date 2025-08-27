namespace SarasBlogg.DTOs
{
    public class CommentWithRoleDto
    {
        public int Id { get; set; }
        public int BloggId { get; set; }
        public string Name { get; set; } = "";
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? TopRole { get; set; } // "superadmin" | "admin" | "superuser" | "user" | null
    }
}
