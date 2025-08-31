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

        public async Task<IActionResult> OnGetAsync(int? hiddenId, int deleteId, int? editId, int? archiveId)
        {
            if (TempData.TryGetValue("UploadErrors", out var errsObj) && errsObj is string errs && !string.IsNullOrWhiteSpace(errs))
                ModelState.AddModelError(string.Empty, errs);

            // Roller från JWT-claims
            IsAdmin = User.IsInRole("admin");
            IsSuperAdmin = User.IsInRole("superadmin");
            IsSuperUser = User.IsInRole("superuser");

            // Initiera modell och sätt standarddatum (SE) för NY post
            NewBlogg ??= new Models.Blogg();
            if (NewBlogg.Id == 0 && NewBlogg.LaunchDate == default)
            {
                var todaySe = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TzSe).Date;
                // Unspecified => <input type="date"> får exakt kalenderdag utan tz-shift
                NewBlogg.LaunchDate = DateTime.SpecifyKind(todaySe, DateTimeKind.Unspecified);
            }

            // Dölj/Visa: admin + superadmin
            if ((IsAdmin || IsSuperAdmin) && hiddenId.HasValue && hiddenId.Value != 0)
            {
                var bloggToHide = await _bloggApi.GetBloggAsync(hiddenId.Value);
                if (bloggToHide != null)
                {
                    bloggToHide.Hidden = !bloggToHide.Hidden;
                    await _bloggApi.UpdateBloggAsync(bloggToHide);
                    _bloggService.InvalidateBlogListCache();
                }
            }

            // Ta bort: endast superadmin
            if (deleteId != 0)
            {
                if (!IsSuperAdmin) return Forbid();
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

            await LoadBloggsWithImagesAsync();

            // Öppna för redigering i formuläret: endast superadmin
            if (IsSuperAdmin && editId.HasValue && editId.Value != 0)
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

                    // Visa datum som svensk kalenderdag i formuläret
                    if (NewBlogg.LaunchDate.Kind == DateTimeKind.Utc)
                    {
                        var se = TimeZoneInfo.ConvertTimeFromUtc(NewBlogg.LaunchDate, TzSe).Date;
                        NewBlogg.LaunchDate = DateTime.SpecifyKind(se, DateTimeKind.Unspecified);
                    }
                    else
                    {
                        // Säkerställ att vi inte råkar bära med UTC-kind i inputfältet
                        NewBlogg.LaunchDate = DateTime.SpecifyKind(NewBlogg.LaunchDate.Date, DateTimeKind.Unspecified);
                    }
                }
            }

            // Arkivera/avarkivera: admin + superadmin
            if ((IsAdmin || IsSuperAdmin) && archiveId.HasValue && archiveId.Value != 0)
            {
                var bloggToArchive = await _bloggApi.GetBloggAsync(archiveId.Value);
                if (bloggToArchive != null)
                {
                    bloggToArchive.IsArchived = !bloggToArchive.IsArchived;
                    await _bloggApi.UpdateBloggAsync(bloggToArchive);
                    _bloggService.InvalidateBlogListCache();
                }

                await LoadBloggsWithImagesAsync();
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
