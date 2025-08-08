using SarasBlogg.DTOs;
using SarasBlogg.Models;

public class BloggWithImage
{
    public Blogg Blogg { get; set; } = new();
    public List<BloggImageDto> Images { get; set; } = new();
    public BloggImageDto? FirstImage
    {
        get
        {
            return Images.OrderBy(i => i.Order).FirstOrDefault();
        }
    }

}