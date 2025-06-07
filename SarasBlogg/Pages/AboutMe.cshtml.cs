using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.Data;
using SarasBlogg.Services;

namespace SarasBlogg.Pages
{
    public class AboutMeModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileHelper _fileHelper;

        public AboutMeModel(ApplicationDbContext context, IFileHelper fileHelper)
        {
            _context = context;
            _fileHelper = fileHelper;
        }

        [BindProperty]
        public Models.AboutMe AboutMe { get; set; }

        [BindProperty]
        public IFormFile? AboutMeImage { get; set; }

        public async Task OnGetAsync()
        {
            AboutMe = await _context.AboutMe.FirstOrDefaultAsync();

            AboutMe = AboutMe ?? new Models.AboutMe();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentAboutMe = await _context.AboutMe.FindAsync(AboutMe.Id);

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
            else
            {
                // Om ingen ny bild laddats upp och det finns en existerande post, behåll bilden
                if (currentAboutMe != null)
                {
                    AboutMe.Image = currentAboutMe.Image;
                }
            }

            AboutMe.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (AboutMe.Id == 0)
            {
                _context.AboutMe.Add(AboutMe);
            }
            else
            {
                if (currentAboutMe == null)
                {
                    return NotFound();
                }

                _context.Entry(currentAboutMe).CurrentValues.SetValues(AboutMe);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }

}

