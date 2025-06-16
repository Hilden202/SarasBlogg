using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.Services;
using SarasBlogg.Models;
using System.Collections.Generic;
using System.Linq;

namespace SarasBlogg.Pages
{
    public class IndexModel : PageModel
    {
        private readonly BloggService _bloggService;

        public IEnumerable<Blogg> LatestPosts { get; set; }

        public IndexModel(BloggService bloggService)
        {
            _bloggService = bloggService;
        }

        public void OnGet()
        {
            LatestPosts = _bloggService
                .GetAllBloggs()
                .OrderByDescending(p => p.LaunchDate)
                .Take(2)
                .ToList();
        }
    }
}
