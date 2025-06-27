using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.Data;           // för ApplicationDbContext
using SarasBlogg.Models;
using SarasBlogg.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;  // För FirstOrDefaultAsync

namespace SarasBlogg.Pages
{
    public class IndexModel : PageModel
    {
        private readonly BloggService _bloggService;
        private readonly ApplicationDbContext _context;

        public IEnumerable<Blogg> LatestPosts { get; set; }
        public AboutMe AboutMe { get; set; }

        public IndexModel(BloggService bloggService, ApplicationDbContext context)
        {
            _bloggService = bloggService;
            _context = context;
        }

        public async Task OnGetAsync()
        {
            LatestPosts = _bloggService
                .GetAllBloggs()
                .OrderByDescending(p => p.LaunchDate)
                .Take(2)
                .ToList();

            AboutMe = await _context.AboutMe.FirstOrDefaultAsync();
            AboutMe ??= new AboutMe();
        }
    }
}
