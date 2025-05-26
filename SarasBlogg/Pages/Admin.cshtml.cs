using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.Data;
using SarasBlogg.Services;

namespace SarasBlogg.Pages
{
    public class AdminModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileHelper _fileHelper;

        public AdminModel(ApplicationDbContext context, IFileHelper fileHelper)
        {
            _context = context;
            NewBlogg = new Models.Blogg();
            _fileHelper = fileHelper;
        }
        public List<Models.Blogg> Bloggs { get; set; }
        [BindProperty]
        public Models.Blogg NewBlogg { get; set; }

        [BindProperty]
        public IFormFile BloggImage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? hiddenId, int deleteId, int? editId, int? archiveId)
        {
            if (NewBlogg == null)
            {
                NewBlogg = new Models.Blogg();
            }
            if (hiddenId.HasValue && hiddenId.Value != 0)
            {
                var bloggToHide = await _context.Blogg.FirstOrDefaultAsync(b => b.Id == hiddenId.Value);

                if (bloggToHide != null)
                {
                    bloggToHide.Hidden = !bloggToHide.Hidden;
                    await _context.SaveChangesAsync();
                }
            }
            if (deleteId != 0)

            {
                Models.Blogg bloggToDelete = await _context.Blogg.FindAsync(deleteId);
                if (bloggToDelete != null) // && User.FindFirstValue(ClaimTypes.NameIdentifier) == blogToBeDeleted.UserId
                {
                    _fileHelper.DeleteImage(bloggToDelete.Image, "img/blogg");

                    _context.Blogg.Remove(bloggToDelete);
                    await _context.SaveChangesAsync();
                }

                return RedirectToPage();
            }

            Bloggs = await _context.Blogg.ToListAsync();

            if (editId.HasValue && editId.Value != 0)
            {
                var bloggToEdit = await _context.Blogg.FirstOrDefaultAsync(b => b.Id == editId.Value);
                if (bloggToEdit != null)
                {
                    NewBlogg = bloggToEdit; // viktig ändring
                }
            }

            if (archiveId.HasValue && archiveId.Value != 0)
            {
                var bloggToArchive = Bloggs.FirstOrDefault(b => b.Id == archiveId.Value);
                if (bloggToArchive != null)
                {
                    bloggToArchive.IsArchived = !bloggToArchive.IsArchived;
                    await _context.SaveChangesAsync();
                }
            }

            return Page();

        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentBlogg = await _context.Blogg.FindAsync(NewBlogg.Id);
            //if (currentBlogg == null)
            if (BloggImage != null)
            {
                // Ta bort gammal bild från databasen (om den finns)
                if (currentBlogg != null && !string.IsNullOrEmpty(currentBlogg.Image))
                {
                    _fileHelper.DeleteImage(currentBlogg.Image, "img/blogg");
                }

                // Spara ny bild
                var newFileName = await _fileHelper.SaveImageAsync(BloggImage, "img/blogg");
                NewBlogg.Image = newFileName;
            }
            else
            {
                // Om ingen ny bild laddats upp och det finns en existerande post, behåll bilden
                if (currentBlogg != null)
                {
                    NewBlogg.Image = currentBlogg.Image;
                }
            }

            NewBlogg.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (NewBlogg.Id == 0)
            {
                _context.Blogg.Add(NewBlogg);
            }
            else
            {
                if (currentBlogg == null)
                {
                    return NotFound();
                }

                _context.Entry(currentBlogg).CurrentValues.SetValues(NewBlogg);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage();

        }

    }
}




