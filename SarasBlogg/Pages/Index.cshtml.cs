using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.Data;           // för ApplicationDbContext
using SarasBlogg.Models;
using SarasBlogg.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.DAL;  // För FirstOrDefaultAsync

namespace SarasBlogg.Pages
{
    public class IndexModel : PageModel
    {
        private readonly BloggService _bloggService;
        private readonly AboutMeAPIManager _aboutMeApiManager;

        public IEnumerable<Blogg> LatestPosts { get; set; }
        public AboutMe AboutMe { get; set; }

        public IndexModel(BloggService bloggService, AboutMeAPIManager aboutMeAPIManager)
        {
            _bloggService = bloggService;
            _aboutMeApiManager = aboutMeAPIManager;
        }

        public async Task OnGetAsync()
        {
            var allBloggs = await _bloggService.GetAllBloggsAsync();
            LatestPosts = allBloggs
                .OrderByDescending(p => p.LaunchDate)
                .Take(2)
                .ToList();


            AboutMe = await _aboutMeApiManager.GetAboutMeAsync() ?? new AboutMe();
        }
    }
}
