using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.Models;
using SarasBlogg.Services;
using SarasBlogg.DAL;
using SarasBlogg.Extensions; // ToSwedishTime

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
            var allBloggs = await _bloggService.GetAllBloggsAsync(false);

            // Sortera på lanseringsdatum i svensk tid (konsekvent med vyerna)
            LatestPosts = allBloggs
                .OrderByDescending(p => p.LaunchDate.ToSwedishTime())
                .Take(2)
                .ToList();

            AboutMe = await _aboutMeApiManager.GetAboutMeAsync() ?? new AboutMe();

            ViewData["ApiFirstLoadDone"] = true;
        }
    }
}
