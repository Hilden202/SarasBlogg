using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DAL;
using SarasBlogg.DTOs;
using SarasBlogg.Models;
using SarasBlogg.Extensions; // ToSwedishTime (används i listan)
using SarasBlogg.Services;   // BloggService för cache-invalidering

namespace SarasBlogg.Pages.Admin
{
    [Authorize(Roles = "admin, superadmin, superuser")]
    public class IndexModel : PageModel
    {
        // API-tjänster för datahantering
        private readonly BloggAPIManager _bloggApi;
        private readonly BloggImageAPIManager _imageApi;
        private readonly CommentAPIManager _commentApi;

        // Cache-tjänst (publik listcache)
        private readonly BloggService _bloggService;

        // Svensk tidszon för tolkning av datum i formuläret
        private static readonly TimeZoneInfo TzSe = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

        public IndexModel(
            BloggAPIManager bloggApi,
            BloggImageAPIManager imageApi,
            CommentAPIManager commentApi,
            BloggService bloggService)
        {
            _bloggApi = bloggApi;
            _imageApi = imageApi;
            _commentApi = commentApi;
            _bloggService = bloggService;

            NewBlogg = new Models.Blogg();
        }

        public List<BloggWithImage> BloggsWithImage { get; set; } = new();
        public BloggWithImage? EditedBloggWithImages { get; set; }

        [BindProperty]
        public Models.Blogg NewBlogg { get; set; }

        [BindProperty]
        public IFormFile[]? BloggImages { get; set; } = Array.Empty<IFormFile>();

        public bool IsAdmin { get; set; }
        public bool IsSuperAdmin { get; set; }
        public bool IsSuperUser { get; set; }

        public async Task<IActionResult> OnGetAsync(int? hiddenId, int? archiveId, int? editId)
        {
            // roles
            IsAdmin = User.IsInRole("admin");
            IsSuperAdmin = User.IsInRole("superadmin");
            IsSuperUser = User.IsInRole("superuser");

            // Default date for the form (SE)
            NewBlogg ??= new Models.Blogg();
            if (NewBlogg.Id == 0 && NewBlogg.LaunchDate == default)
            {
                var todaySe = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TzSe).Date;
                NewBlogg.LaunchDate = DateTime.SpecifyKind(todaySe, DateTimeKind.Unspecified);
            }

            // ---- Toggle HIDDEN (admin + superadmin) ----
            if ((IsAdmin || IsSuperAdmin) && hiddenId is int hid and > 0)
            {
                await _bloggApi.ToggleHiddenAsync(hid);
                _bloggService.InvalidateBlogListCache();
                return RedirectToPage();
            }

            // ---- Toggle ARCHIVED (admin + superadmin) ----
            if ((IsAdmin || IsSuperAdmin) && archiveId is int aid and > 0)
            {
                await _bloggApi.ToggleArchivedAsync(aid);
                _bloggService.InvalidateBlogListCache();
                return RedirectToPage();
            }

            // load lists
            await LoadBloggsWithImagesAsync();

            // open edit form (superadmin)
            if (IsSuperAdmin && editId.HasValue && editId.Value != 0)
            {
                var row = BloggsWithImage.FirstOrDefault(x => x.Blogg.Id == editId.Value);
                if (row != null)
                {
                    EditedBloggWithImages = new BloggWithImage { Blogg = row.Blogg, Images = row.Images };
                    NewBlogg = row.Blogg;

                    if (NewBlogg.LaunchDate.Kind == DateTimeKind.Utc)
                    {
                        var se = TimeZoneInfo.ConvertTimeFromUtc(NewBlogg.LaunchDate, TzSe).Date;
                        NewBlogg.LaunchDate = DateTime.SpecifyKind(se, DateTimeKind.Unspecified);
                    }
                    else
                    {
                        NewBlogg.LaunchDate = DateTime.SpecifyKind(NewBlogg.LaunchDate.Date, DateTimeKind.Unspecified);
                    }
                }
            }

            return Page();
        }

        // Skapa/ändra blogg: endast superadmin
        public async Task<IActionResult> OnPostAsync()
        {
            IsSuperAdmin = User.IsInRole("superadmin");
            if (!IsSuperAdmin) return Forbid();

            var uploadErrors = new List<string>();
            var currentBlogg = await _bloggApi.GetBloggAsync(NewBlogg.Id);

            // Sätt användar-id
            NewBlogg.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Tolkning av <input type="date">: svensk kalenderdag -> UTC midnatt
            var localDate = DateTime.SpecifyKind(NewBlogg.LaunchDate.Date, DateTimeKind.Unspecified);
            var utcDate = TimeZoneInfo.ConvertTimeToUtc(localDate, TzSe);
            NewBlogg.LaunchDate = utcDate;

            if (NewBlogg.Id == 0)
            {
                var savedBlogg = await _bloggApi.SaveBloggAsync(NewBlogg);
                if (savedBlogg == null)
                {
                    ModelState.AddModelError(string.Empty, "Kunde inte spara blogg.");
                    await LoadBloggsWithImagesAsync();
                    return Page();
                }

                // Sätt Id från API:t
                NewBlogg.Id = savedBlogg.Id;
            }
            else
            {
                if (currentBlogg == null)
                    return NotFound();

                await _bloggApi.UpdateBloggAsync(NewBlogg);
            }

            // Bilduppladdning
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

            _bloggService.InvalidateBlogListCache();

            if (uploadErrors.Count > 0)
                TempData["UploadErrors"] = string.Join("\n", uploadErrors);

            return RedirectToPage();
        }

        // Endast superadmin
        public async Task<IActionResult> OnPostSetFirstImageAsync(int imageId, int bloggId)
        {
            if (!User.IsInRole("superadmin")) return Forbid();
            var images = await _imageApi.GetImagesByBloggIdAsync(bloggId);
            var imageToSet = images.FirstOrDefault(i => i.Id == imageId);

            if (imageToSet != null)
            {
                images.Remove(imageToSet);
                images.Insert(0, imageToSet);

                await _imageApi.UpdateImageOrderAsync(bloggId, images);
                _bloggService.InvalidateBlogListCache();
            }

            return RedirectToPage(new { editId = bloggId });
        }

        // Endast superadmin
        public async Task<IActionResult> OnPostDeleteImageAsync(int imageId, int bloggId)
        {
            if (!User.IsInRole("superadmin")) return Forbid();
            await _imageApi.DeleteImageAsync(imageId);
            _bloggService.InvalidateBlogListCache();

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

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteBloggAsync(int deleteId)
        {
            if (!User.IsInRole("superadmin")) return Forbid();

            var bloggToDelete = await _bloggApi.GetBloggAsync(deleteId);
            if (bloggToDelete != null)
            {
                await _commentApi.DeleteCommentsAsync(bloggToDelete.Id);
                await _imageApi.DeleteImagesByBloggIdAsync(bloggToDelete.Id);
                await _bloggApi.DeleteBloggAsync(bloggToDelete.Id);
                _bloggService.InvalidateBlogListCache();
            }

            return RedirectToPage();
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
