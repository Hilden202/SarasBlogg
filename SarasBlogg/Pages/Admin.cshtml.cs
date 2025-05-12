using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.Data;

namespace SarasBlogg.Pages
{
    public class AdminModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AdminModel(ApplicationDbContext context)
        {
            _context = context;
            NewBlogg = new Models.Blogg();
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
                    DeleteImage(bloggToDelete.Image);

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
            string fileName = NewBlogg.Image;

            if (BloggImage != null)
            {
                if (!string.IsNullOrEmpty(NewBlogg.Image))
                {
                    DeleteImage(NewBlogg.Image);
                }

                fileName = Random.Shared.Next(0, 1000000).ToString() + "_" + BloggImage.FileName;
                using (var fileStream = new FileStream("./wwwroot/img/" + fileName, FileMode.Create))
                {
                    await BloggImage.CopyToAsync(fileStream);
                }

            }

            NewBlogg.Image = fileName;

            NewBlogg.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (NewBlogg.Id == 0)
            {
                _context.Blogg.Add(NewBlogg);
            }
            else //Detta var en klurig nöt då bilder hela tiden försvann annars->
            {
                var existingBlogg = await _context.Blogg.FindAsync(NewBlogg.Id);
                if (existingBlogg == null)
                {
                    return NotFound();
                }

                // Behåll den gamla bilden om ingen ny laddats upp
                if (string.IsNullOrEmpty(NewBlogg.Image))
                {
                    NewBlogg.Image = existingBlogg.Image;
                }
                else if (!string.IsNullOrEmpty(NewBlogg.Image))
                {
                    DeleteImage(existingBlogg.Image);
                }

                // Uppdatera resten
                _context.Entry(existingBlogg).CurrentValues.SetValues(NewBlogg);
            }//<------------------------------------------------------------------

            await _context.SaveChangesAsync();

            Bloggs = await _context.Blogg.ToListAsync();

            return RedirectToPage(); // Reload the same page
        }
        private void DeleteImage(string? imageName)
        {
            if (!string.IsNullOrEmpty(imageName))
            {
                string filePath = "./wwwroot/img/" + imageName;
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
        }
    }
}




