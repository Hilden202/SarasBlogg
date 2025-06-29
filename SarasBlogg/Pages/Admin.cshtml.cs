using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.DAL;
using SarasBlogg.Data;
using SarasBlogg.Services;

namespace SarasBlogg.Pages
{
    [Authorize(Roles = "admin, superadmin")]
    public class AdminModel : PageModel
    {
        // API-tjänster för datahantering
        private readonly BloggAPIManager _bloggApi;
        private readonly CommentAPIManager _commentApi;

        // Övriga tjänster
        private readonly IFileHelper _fileHelper;
        private readonly ApplicationDbContext _context; // TODO: Ta bort när ej längre behövs

        // Identitet och roller
        private readonly RoleManager<IdentityRole> _roleManager;
        public readonly UserManager<ApplicationUser> _userManager;


        public AdminModel(
            BloggAPIManager bloggApi,
            CommentAPIManager commentApi,
            IFileHelper fileHelper,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _bloggApi = bloggApi;
            _commentApi = commentApi;
            _fileHelper = fileHelper;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;

            NewBlogg = new Models.Blogg();
        }


        public List<Models.Blogg> Bloggs { get; set; }
        [BindProperty]
        public Models.Blogg NewBlogg { get; set; }

        [BindProperty]
        public IFormFile? BloggImage { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsSuperAdmin { get; set; }

        public async Task<IActionResult> OnGetAsync(int? hiddenId, int deleteId, int? editId, int? archiveId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                IsAdmin = await _userManager.IsInRoleAsync(currentUser, "admin");
                IsSuperAdmin = await _userManager.IsInRoleAsync(currentUser, "superadmin");
            }

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
                    await _commentApi.DeleteCommentsAsync(bloggToDelete.Id); // ta bort eventuella kopplade kommentarer här.

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

            NewBlogg.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Kanske inte behövs men intresseant att se under test.

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