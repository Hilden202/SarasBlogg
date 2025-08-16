using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.Services;
using SarasBlogg.ViewModels;

namespace SarasBlogg.Pages
{
    public class BloggModel : PageModel
    {
        private readonly BloggService _bloggService;

        public BloggModel(BloggService bloggService)
        {
            _bloggService = bloggService;
        }

        [BindProperty]
        public BloggViewModel ViewModel { get; set; } = new();
        public string RoleSymbol => GetRoleSymbol();

        private bool IsAuth => User?.Identity?.IsAuthenticated == true;
        private string CurrentUserName => IsAuth ? (User?.Identity?.Name ?? "") : "";

        private string GetRoleSymbol()
        {
            if (User.IsInRole("superadmin")) return "\U0001F451"; // ??
            if (User.IsInRole("admin")) return "\u2B50";     // ?
            if (User.IsInRole("superuser")) return "\u26A1";     // ?
            if (User.IsInRole("user")) return "\U0001F464"; // ??
            return "";
        }

        public async Task OnGetAsync(int showId, int id)
        {
            if (showId != 0)
            {
                await _bloggService.UpdateViewCountAsync(showId);
            }
            ViewModel = await _bloggService.GetBloggViewModelAsync(false, showId);

            ViewModel.RoleSymbol = GetRoleSymbol();
        }

        public async Task<IActionResult> OnPostAsync(int deleteCommentId)
        {
            // 1) Delete som tidigare
            if (deleteCommentId != 0)
            {
                var existing = await _bloggService.GetCommentAsync(deleteCommentId);
                if (existing != null)
                {
                    await _bloggService.DeleteCommentAsync(deleteCommentId);
                    return RedirectToPage(
                        pageName: null,                 // <= stanna på samma sida
                        pageHandler: null,
                        routeValues: new { showId = existing.BloggId },
                        fragment: "comments");
                }
            }

            // 2) Direkt efter model binding: tvinga inloggades namn och relaxa ev. validering
            if (IsAuth && ViewModel?.Comment != null)
            {
                ViewModel.Comment.Name = CurrentUserName;

                // Täck både möjliga nycklar beroende på hur asp-for är skrivet i vyn
                ModelState.Remove("ViewModel.Comment.Name");
                ModelState.Remove("Comment.Name");
            }

            // 3) Sätt CreatedAt i UTC för nya kommentarer
            if (ViewModel?.Comment != null && ViewModel.Comment.Id == null)
            {
                ViewModel.Comment.CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            }

            // 4) Validera + spara
            if (ModelState.IsValid && ViewModel?.Comment?.Id == null)
            {
                string errorMessage = await _bloggService.SaveCommentAsync(ViewModel.Comment);

                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    // Visa fel och rendera om sidan med data
                    ModelState.AddModelError("Comment.Content", errorMessage);
                    ViewModel = await _bloggService.GetBloggViewModelAsync(false, ViewModel.Comment?.BloggId ?? 0);

                    ViewModel.RoleSymbol = GetRoleSymbol();

                    return Page();
                }
            }

            // 5) Tillbaka till samma blogg
            return RedirectToPage(
                pageName: null,                 // <= stanna på samma sida
                pageHandler: null,
                routeValues: new { showId = ViewModel?.Comment?.BloggId },
                fragment: "comments");
        }
    }
}
