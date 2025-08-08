using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DAL;
using SarasBlogg.Data;
using SarasBlogg.DTOs;
using SarasBlogg.Models;

namespace SarasBlogg.Pages.Admin
{
    [Authorize(Roles = "admin, superadmin")]
    public class IndexModel : PageModel
    {
        // API-tjänster för datahantering
        private readonly BloggAPIManager _bloggApi;
        private readonly BloggImageAPIManager _imageApi;
        private readonly CommentAPIManager _commentApi;

        // Identitet och roller
        private readonly RoleManager<IdentityRole> _roleManager;
        public readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(
            BloggAPIManager bloggApi,
            BloggImageAPIManager imageApi,
            CommentAPIManager commentApi,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _bloggApi = bloggApi;
            _imageApi = imageApi;
            _commentApi = commentApi;
            _userManager = userManager;
            _roleManager = roleManager;

            NewBlogg = new Models.Blogg();
        }

        public List<BloggWithImage> BloggsWithImage { get; set; } = new();
        public BloggWithImage? EditedBloggWithImages { get; set; }

        [BindProperty]
        public Models.Blogg NewBlogg { get; set; }

        [BindProperty]
        public IFormFile[] BloggImages { get; set; } = Array.Empty<IFormFile>();

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
                if (bloggToDelete != null)
                {
                    // Ta bort kopplade kommentarer och bilder före bloggen
                    await _commentApi.DeleteCommentsAsync(bloggToDelete.Id);
                    await _imageApi.DeleteImagesByBloggIdAsync(bloggToDelete.Id);
                    await _bloggApi.DeleteBloggAsync(bloggToDelete.Id);
                }

                return RedirectToPage();
            }

            await LoadBloggsWithImagesAsync();

            if (editId.HasValue && editId.Value != 0)
            {
                var blogg = BloggsWithImage.FirstOrDefault(b => b.Blogg.Id == editId.Value);
                if (blogg != null)
                {
                    EditedBloggWithImages = new BloggWithImage
                    {
                        Blogg = blogg.Blogg,
                        Images = blogg.Images
                    };

                    NewBlogg = blogg.Blogg;
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

                await LoadBloggsWithImagesAsync();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentBlogg = await _bloggApi.GetBloggAsync(NewBlogg.Id);

            // Sätt användar-id och normalisera datum (UTC)
            NewBlogg.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            NewBlogg.LaunchDate = DateTime.SpecifyKind(NewBlogg.LaunchDate.Date, DateTimeKind.Utc);

            if (NewBlogg.Id == 0)
            {
                var savedBlogg = await _bloggApi.SaveBloggAsync(NewBlogg);
                if (savedBlogg == null)
                {
                    ModelState.AddModelError(string.Empty, "Kunde inte spara blogg.");
                    await LoadBloggsWithImagesAsync();
                    return Page();
                }

                // Få rätt Id från API:t
                NewBlogg.Id = savedBlogg.Id;
            }
            else
            {
                if (currentBlogg == null)
                    return NotFound();

                await _bloggApi.UpdateBloggAsync(NewBlogg);
            }

            // Hantera bilder via API
            if (BloggImages is { Length: > 0 })
            {
                foreach (var image in BloggImages)
                {
                    if (image is { Length: > 0 })
                    {
                        _ = await _imageApi.UploadImageAsync(image, NewBlogg.Id);
                    }
                }
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSetFirstImageAsync(int imageId, int bloggId)
        {
            // Hämta alla bilder för bloggen
            var images = await _imageApi.GetImagesByBloggIdAsync(bloggId);
            var imageToSet = images.FirstOrDefault(i => i.Id == imageId);

            if (imageToSet != null)
            {
                // Flytta vald bild först i listan
                images.Remove(imageToSet);
                images.Insert(0, imageToSet);

                // Spara nya ordningen i API:et/databasen
                await _imageApi.UpdateImageOrderAsync(bloggId, images);
            }

            return RedirectToPage(new { editId = bloggId });
        }

        public async Task<IActionResult> OnPostDeleteImageAsync(int imageId, int bloggId)
        {
            await _imageApi.DeleteImageAsync(imageId);
            await LoadBloggsWithImagesAsync();

            var blogg = BloggsWithImage.FirstOrDefault(b => b.Blogg.Id == bloggId);
            if (blogg != null)
            {
                EditedBloggWithImages = new BloggWithImage
                {
                    Blogg = blogg.Blogg,
                    Images = blogg.Images
                };
            }

            return RedirectToPage(new { editId = bloggId });
        }

        private async Task LoadBloggsWithImagesAsync()
        {
            var allBloggs = await _bloggApi.GetAllBloggsAsync();
            BloggsWithImage = new List<BloggWithImage>();

            foreach (var blogg in allBloggs)
            {
                var images = await _imageApi.GetImagesByBloggIdAsync(blogg.Id);

                BloggsWithImage.Add(new BloggWithImage
                {
                    Blogg = blogg,
                    Images = images
                });
            }
        }
    }
}