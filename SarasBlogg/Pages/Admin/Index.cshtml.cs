using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DAL;
using SarasBlogg.Data;
using SarasBlogg.Services;

namespace SarasBlogg.Pages.Admin
{
    [Authorize(Roles = "admin, superadmin")]
    public class IndexModel : PageModel
    {
        // API-tjänster för datahantering
        private readonly BloggAPIManager _bloggApi;
        private readonly CommentAPIManager _commentApi;

        // Övriga tjänster
        private readonly IFileHelper _fileHelper;

        // Identitet och roller
        private readonly RoleManager<IdentityRole> _roleManager;
        public readonly UserManager<ApplicationUser> _userManager;


        public IndexModel(
            BloggAPIManager bloggApi,
            CommentAPIManager commentApi,
            IFileHelper fileHelper,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _bloggApi = bloggApi;
            _commentApi = commentApi;
            _fileHelper = fileHelper;
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
                var bloggToHide = await _bloggApi.GetBloggAsync(hiddenId.Value);

                if (bloggToHide != null)
                {
                    bloggToHide.Hidden = !bloggToHide.Hidden;
                    await _bloggApi.UpdateBloggAsync(bloggToHide);
                }
            }
            if (deleteId != 0)

            {
                var bloggToDelete = await _bloggApi.GetBloggAsync(deleteId);

                if (bloggToDelete != null) // && User.FindFirstValue(ClaimTypes.NameIdentifier) == blogToBeDeleted.UserId
                {
                    await _commentApi.DeleteCommentsAsync(bloggToDelete.Id); // ta bort eventuella kopplade kommentarer här.

                    _fileHelper.DeleteImage(bloggToDelete.Image, "img/blogg");

                    await _bloggApi.DeleteBloggAsync(bloggToDelete.Id);

                }

                return RedirectToPage();
            }

            Bloggs = await _bloggApi.GetAllBloggsAsync();

            if (editId.HasValue && editId.Value != 0)
            {
                var bloggToEdit = await _bloggApi.GetBloggAsync(editId.Value);
                if (bloggToEdit != null)
                {
                    NewBlogg = bloggToEdit; // viktig ändring
                }
            }

            if (archiveId.HasValue && archiveId.Value != 0)
            {
                var bloggToArchive = await _bloggApi.GetBloggAsync(archiveId.Value);
                if (bloggToArchive != null)
                {
                    bloggToArchive.IsArchived = !bloggToArchive.IsArchived;
                    await _bloggApi.UpdateBloggAsync(bloggToArchive);
                }

                Bloggs = await _bloggApi.GetAllBloggsAsync(); // Efter uppdatering via API måste listan hämtas om manuellt,
            }

            return Page();

        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentBlogg = await _bloggApi.GetBloggAsync(NewBlogg.Id);

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
                // → Behåll befintlig bild om ingen ny laddats upp
                if (currentBlogg != null)
                {
                    NewBlogg.Image = currentBlogg.Image;
                }
            }

            //NewBlogg.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier); // → Lägg till användar-id (för logg/säkerhet)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"[AUTH] IsAuthenticated: {User.Identity.IsAuthenticated}, UserId: {userId}");

            NewBlogg.UserId = userId; // tillåter null

            // 🛠 Garantera att LaunchDate skickas som UTC med T00:00:00Z
            NewBlogg.LaunchDate = DateTime.SpecifyKind(NewBlogg.LaunchDate.Date, DateTimeKind.Utc);

            if (NewBlogg.Id == 0)
            {
                // → Sparar nu utan felhantering
                //await _bloggApi.SaveBloggAsync(NewBlogg);

                // 🔄 Om senare lägga till felhantering:

                var result = await _bloggApi.SaveBloggAsync(NewBlogg);
                if (!string.IsNullOrEmpty(result))
                {
                    ModelState.AddModelError(string.Empty, $"Kunde inte spara blogg: {result}");
                    Bloggs = await _bloggApi.GetAllBloggsAsync();
                    return Page(); // Visa fel i formuläret
                }

            }
            else
            {
                // → Om blogg redan finns, uppdatera
                if (currentBlogg == null)
                {
                    return NotFound(); // Säkerhetskoll
                }

                await _bloggApi.UpdateBloggAsync(NewBlogg);
            }

            return RedirectToPage(); // → Alltid redirect efter POST (Post/Redirect/Get-mönstret)
        }

    }
}