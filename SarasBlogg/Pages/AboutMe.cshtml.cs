using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DAL;
using SarasBlogg.Services;

namespace SarasBlogg.Pages
{
    public class AboutMeModel : PageModel
    {
        private readonly AboutMeAPIManager _aboutMeApiManager;
        private readonly IFileHelper _fileHelper;

        public AboutMeModel(AboutMeAPIManager aboutMeAPIManager, IFileHelper fileHelper)
        {
            _aboutMeApiManager = aboutMeAPIManager;
            _fileHelper = fileHelper;
        }

        [BindProperty]
        public Models.AboutMe AboutMe { get; set; }

        [BindProperty]
        public IFormFile? AboutMeImage { get; set; }

        [BindProperty]
        public bool RemoveImage { get; set; }

        public async Task OnGetAsync()
        {
            AboutMe = await _aboutMeApiManager.GetAboutMeAsync();

            if (AboutMe == null)
            {
                AboutMe = new Models.AboutMe();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentAboutMe = await _aboutMeApiManager.GetAboutMeAsync();

            // 1) Ta bort bild helt
            if (RemoveImage)
            {
                if (currentAboutMe != null && !string.IsNullOrEmpty(currentAboutMe.Image))
                {
                    // ändrat: "about" istället för "img/aboutme"
                    await _fileHelper.DeleteImageAsync(currentAboutMe.Image, "about");
                }
                AboutMe.Image = null;
            }
            // 2) Ny bild uppladdad -> ladda upp först, radera gammal efteråt
            else if (AboutMeImage != null)
            {
                // ändrat: spara i uploads/about/...
                var newUrl = await _fileHelper.SaveImageAsync(AboutMeImage, "about");

                if (currentAboutMe != null && !string.IsNullOrEmpty(currentAboutMe.Image))
                {
                    await _fileHelper.DeleteImageAsync(currentAboutMe.Image, "about");
                }

                AboutMe.Image = newUrl;
            }
            // 3) Behåll befintlig bild
            else if (currentAboutMe != null)
            {
                AboutMe.Image = currentAboutMe.Image;
            }

            AboutMe.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (AboutMe.Id == 0)
                await _aboutMeApiManager.SaveAboutMeAsync(AboutMe);  // POST
            else
                await _aboutMeApiManager.UpdateAboutMeAsync(AboutMe); // PUT

            return RedirectToPage();
        }


    }

}

