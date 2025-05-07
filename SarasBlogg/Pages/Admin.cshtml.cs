using System.Dynamic;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.Data;
using SarasBlogg.Models;

namespace SarasBlogg.Pages
{
    public class AdminModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AdminModel(ApplicationDbContext context)
        {
            _context = context;
        }
        public List<Models.Blogg> Bloggs { get; set; }
        [BindProperty]
        public Models.Blogg NewBlogg { get; set; }

        [BindProperty]
        public IFormFile UploadedImage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? deleteIndex, int? editId, int? archiveId)
        {
            if (deleteIndex.HasValue && deleteIndex.Value != 0) // && User.FindFirstValue(ClaimTypes.NameIdentifier) == blogToBeDeleted.UserId
            {
                var bloggToDelete = await _context.Blogg.FindAsync(deleteIndex.Value);
                if (bloggToDelete != null)
                {
                    string fileName = "./wwwroot/img/" + bloggToDelete.Image;
                    if (System.IO.File.Exists(fileName))
                    {
                        System.IO.File.Delete(fileName);
                    }
                    _context.Blogg.Remove(bloggToDelete);
                    await _context.SaveChangesAsync();
                }

                return RedirectToPage(); // undvik att fortsätta ladda annan data i onödan
            }

            // Hämta blogglista om inget delete har skett
            Bloggs = await _context.Blogg.ToListAsync();

            if (archiveId.HasValue && archiveId.Value != 0)
            {
                var bloggToArchive = Bloggs.FirstOrDefault(b => b.Id == archiveId.Value);
                if (bloggToArchive != null)
                {
                    bloggToArchive.IsArchived = !bloggToArchive.IsArchived;
                    await _context.SaveChangesAsync();
                }
            }

            if (editId.HasValue && editId.Value != 0)
            {
                NewBlogg = await _context.Blogg.FirstOrDefaultAsync(b => b.Id == editId.Value);
            }
            else
            {
                NewBlogg = new Blogg();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            string fileName = "";


            if (ModelState.IsValid)
            {
                // Hantera save (ny eller uppdaterad blogg)
                if (UploadedImage != null)
                {
                    fileName = Random.Shared.Next(0, 1000000).ToString() + "_" + UploadedImage.FileName;
                    using (var fileStream = new FileStream("./wwwroot/img/" + fileName, FileMode.Create))
                    {
                        await UploadedImage.CopyToAsync(fileStream);
                    }
                    NewBlogg.Image = fileName;
                }

                if (NewBlogg.Id == 0)
                {
                    _context.Blogg.Add(NewBlogg);
                }
                else
                {
                    _context.Blogg.Update(NewBlogg);
                }

                await _context.SaveChangesAsync();

                Bloggs = await _context.Blogg.ToListAsync(); // Hämta uppdaterad lista
            }

            return RedirectToPage(); // Ladda om samma sida


        }
    }
}
