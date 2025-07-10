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

            if (AboutMeImage != null)
            {
                // Ta bort gammal bild från databasen (om den finns)
                if (currentAboutMe != null && !string.IsNullOrEmpty(currentAboutMe.Image))
                {
                    _fileHelper.DeleteImage(currentAboutMe.Image, "img/aboutme");
                }

                // Spara ny bild
                var newFileName = await _fileHelper.SaveImageAsync(AboutMeImage, "img/aboutme");
                AboutMe.Image = newFileName;
            }

            // Om ingen ny bild laddats upp och det finns en existerande post, behåll bilden
            else if (currentAboutMe != null)
            {
                AboutMe.Image = currentAboutMe.Image;
            }


            AboutMe.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (AboutMe.Id == 0)
            {
                await _aboutMeApiManager.SaveAboutMeAsync(AboutMe); // POST
            }
            else
            {
                await _aboutMeApiManager.UpdateAboutMeAsync(AboutMe); // PUT
            }

            return RedirectToPage();
        }
    }

}

