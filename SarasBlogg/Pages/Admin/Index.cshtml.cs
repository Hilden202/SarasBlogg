using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DAL;
using SarasBlogg.DTOs;
using SarasBlogg.Models;
using SarasBlogg.Extensions; // <-- ToSwedishTime
using SarasBlogg.Services;   // <-- BloggService for cache invalidation

namespace SarasBlogg.Pages.Admin
{
    [Authorize(Roles = "admin, superadmin")]
    public class IndexModel : PageModel
    {
        // API-tjänster för datahantering
        private readonly BloggAPIManager _bloggApi;
        private readonly BloggImageAPIManager _imageApi;
        private readonly CommentAPIManager _commentApi;

        // Cache-tjänst (publik listcache)
        private readonly BloggService _bloggService; // <-- injiceras

        // Svensk tidszon för konvertering till UTC vid persistens
        private static readonly TimeZoneInfo TzSe = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

        public IndexModel(
            BloggAPIManager bloggApi,
            BloggImageAPIManager imageApi,
            CommentAPIManager commentApi,
            BloggService bloggService) // <-- lägg till i DI
        {
            _bloggApi = bloggApi;
            _imageApi = imageApi;
            _commentApi = commentApi;
            _bloggService = bloggService; // <-- spara ref

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
            if (TempData.TryGetValue("UploadErrors", out var errsObj) && errsObj is string errs && !string.IsNullOrWhiteSpace(errs))
                ModelState.AddModelError(string.Empty, errs);
            // Roller kommer från JWT-claims som sattes vid login
            IsAdmin = User.IsInRole("admin");
            IsSuperAdmin = User.IsInRole("superadmin");

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
                    _bloggService.InvalidateBlogListCache(); // <-- viktigt
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
                    _bloggService.InvalidateBlogListCache(); // <-- viktigt
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
                    _bloggService.InvalidateBlogListCache(); // <-- viktigt
                }

                await LoadBloggsWithImagesAsync();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var uploadErrors = new List<string>();
            var currentBlogg = await _bloggApi.GetBloggAsync(NewBlogg.Id);

            // Sätt användar-id
            NewBlogg.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Normalisera LaunchDate:
            // - datum från användare (svensk lokal tid via ToSwedishTime)
            // - midnatt (Date)
            // - konvertera till UTC (T00:00:00Z)
            var seDate = NewBlogg.LaunchDate.ToSwedishTime().Date; // svensk lokal dag
            var utcDate = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(seDate, DateTimeKind.Unspecified), TzSe);
            NewBlogg.LaunchDate = DateTime.SpecifyKind(utcDate, DateTimeKind.Utc);

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

            if (BloggImages is { Length: > 0 })
            {
                foreach (var f in BloggImages.Where(f => f != null && f.Length > 0))
                {
                    try
                    {
                        await _imageApi.UploadImageAsync(f, NewBlogg.Id);
                        await Task.Delay(200);
                    }
                    catch (Exception ex)
                    {
                        uploadErrors.Add($"Kunde inte ladda upp {f.FileName}: {ex.Message}");
                    }
                }
            }


            _bloggService.InvalidateBlogListCache(); // <-- viktigt efter skapa/uppdatera/bilder
            if (uploadErrors.Count > 0)
                TempData["UploadErrors"] = string.Join("\n", uploadErrors);
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
                _bloggService.InvalidateBlogListCache(); // <-- viktigt
            }

            return RedirectToPage(new { editId = bloggId });
        }

        public async Task<IActionResult> OnPostDeleteImageAsync(int imageId, int bloggId)
        {
            await _imageApi.DeleteImageAsync(imageId);
            _bloggService.InvalidateBlogListCache(); // <-- viktigt

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
