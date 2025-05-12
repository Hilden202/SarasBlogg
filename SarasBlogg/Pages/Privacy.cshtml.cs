using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.Data;

namespace SarasBlogg.Pages
{
    public class PrivacyModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PrivacyModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Models.AboutMe AboutMe { get; set; }

        [BindProperty]
        public IFormFile AboutMeImage { get; set; }

        public async Task OnGetAsync()
        {
            AboutMe = await _context.AboutMe.FirstOrDefaultAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Hantera filnamn för bild
            string fileName = AboutMe.Image;

            if (AboutMeImage != null)
            {
                // Ta bort gammal bild om det finns
                if (!string.IsNullOrEmpty(AboutMe.Image))
                {
                    DeleteImage(AboutMe.Image);
                }

                // Skapa nytt filnamn och spara bild
                fileName = $"{Random.Shared.Next(0, 1000000)}_{AboutMeImage.FileName}";
                var filePath = Path.Combine("wwwroot", "imgaboutme", fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await AboutMeImage.CopyToAsync(fileStream);
                }
            }

            AboutMe.Image = fileName;
            AboutMe.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (AboutMe.Id == 0)
            {
                _context.AboutMe.Add(AboutMe);
            }
            else
            {
                var existingAboutMe = await _context.AboutMe.FindAsync(AboutMe.Id);
                if (existingAboutMe == null)
                {
                    return NotFound();
                }

                // Behåll den gamla bilden om ingen ny laddats upp
                if (AboutMeImage == null)
                {
                    AboutMe.Image = existingAboutMe.Image;
                }
                else if (!string.IsNullOrEmpty(existingAboutMe.Image))
                {
                    DeleteImage(existingAboutMe.Image);
                }

                _context.Entry(existingAboutMe).CurrentValues.SetValues(AboutMe);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        private void DeleteImage(string? imageName)
        {
            if (!string.IsNullOrEmpty(imageName))
            {
                var filePath = Path.Combine("wwwroot", "imgaboutme", imageName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
        }
    }

}
