// Pages/AboutMe.cshtml.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DAL;

namespace SarasBlogg.Pages
{
    public class AboutMeModel : PageModel
    {
        private readonly AboutMeAPIManager _aboutMeApiManager;
        private readonly AboutMeImageAPIManager _aboutMeImageApi;

        public AboutMeModel(AboutMeAPIManager aboutMeAPIManager, AboutMeImageAPIManager aboutMeImageApi)
        {
            _aboutMeApiManager = aboutMeAPIManager;
            _aboutMeImageApi = aboutMeImageApi;
        }

        [BindProperty] public Models.AboutMe AboutMe { get; set; } = new();
        [BindProperty] public IFormFile? AboutMeImage { get; set; }
        [BindProperty] public bool RemoveImage { get; set; }

        public async Task OnGetAsync()
        {
            AboutMe = await _aboutMeApiManager.GetAboutMeAsync() ?? new Models.AboutMe();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentAboutMe = await _aboutMeApiManager.GetAboutMeAsync();

            if (RemoveImage)
            {
                await _aboutMeImageApi.DeleteAsync();   // tar bort i GitHub + nollar i DB
                AboutMe.Image = null;                   // spegla lokalt
            }
            else if (AboutMeImage is { Length: > 0 })
            {
                using var s = AboutMeImage.OpenReadStream();
                var url = await _aboutMeImageApi.UploadAsync(s, AboutMeImage.FileName, AboutMeImage.ContentType);
                AboutMe.Image = url;                    // spegla lokalt
            }
            else if (currentAboutMe != null)
            {
                AboutMe.Image = currentAboutMe.Image;   // oförändrad
            }

            AboutMe.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (AboutMe.Id == 0)
                await _aboutMeApiManager.SaveAboutMeAsync(AboutMe);   // POST
            else
                await _aboutMeApiManager.UpdateAboutMeAsync(AboutMe); // PUT

            return RedirectToPage();
        }
    }
}