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
        [BindProperty] public string? CroppedImageData { get; set; }


        public async Task OnGetAsync()
        {
            AboutMe = await _aboutMeApiManager.GetAboutMeAsync() ?? new Models.AboutMe();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentAboutMe = await _aboutMeApiManager.GetAboutMeAsync();

            if (RemoveImage)
            {
                await _aboutMeImageApi.DeleteAsync();
                AboutMe.Image = null;
            }
            else if (!string.IsNullOrWhiteSpace(CroppedImageData))
            {
                // data:image/png;base64,.....
                var base64 = CroppedImageData.Contains(",")
                    ? CroppedImageData.Split(',', 2)[1]
                    : CroppedImageData;

                var bytes = Convert.FromBase64String(base64);
                using var ms = new MemoryStream(bytes);
                var url = await _aboutMeImageApi.UploadAsync(ms, "aboutme-cropped.png", "image/png");
                AboutMe.Image = url;
            }
            else if (AboutMeImage is { Length: > 0 })
            {
                using var s = AboutMeImage.OpenReadStream();
                var url = await _aboutMeImageApi.UploadAsync(s, AboutMeImage.FileName, AboutMeImage.ContentType);
                AboutMe.Image = url;
            }
            else if (currentAboutMe != null)
            {
                AboutMe.Image = currentAboutMe.Image; // oförändrad
            }

            AboutMe.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            AboutMe.Title = string.IsNullOrWhiteSpace(AboutMe.Title) ? null : AboutMe.Title;
            AboutMe.Content = string.IsNullOrWhiteSpace(AboutMe.Content) ? null : AboutMe.Content;

            if (AboutMe.Id == 0)
                await _aboutMeApiManager.SaveAboutMeAsync(AboutMe);   // POST
            else
                await _aboutMeApiManager.UpdateAboutMeAsync(AboutMe); // PUT

            return RedirectToPage();
        }
    }
}