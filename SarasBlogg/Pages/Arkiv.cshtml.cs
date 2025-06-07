using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.Data;
using SarasBlogg.Services;
using SarasBlogg.ViewModels;

namespace SarasBlogg.Pages
{
    public class ArkivModel : PageModel
    {
        private readonly BloggService _bloggService;

        public ArkivModel(BloggService bloggService)
        {
            _bloggService = bloggService;
        }

        [BindProperty]
        public BloggViewModel ViewModel { get; set; } = new();

        public async Task OnGetAsync(int showId, int id)
        {
            if (showId != 0)
            {
                await _bloggService.UpdateViewCountAsync(showId);
            }
            ViewModel = await _bloggService.GetBloggViewModelAsync(true, showId);
        }

        public async Task<IActionResult> OnPostAsync(int deleteCommentId)
        {
            if (deleteCommentId != 0)
            {
                var comment = await _bloggService.GetCommentAsync(deleteCommentId);
                if (comment != null)
                {
                    await _bloggService.DeleteCommentAsync(deleteCommentId);
                    return RedirectToPage("./Arkiv", new { showId = comment.BloggId });
                }
            }

            if (ModelState.IsValid && ViewModel.Comment?.Id == null)
            {
                string errorMessage = await _bloggService.SaveCommentAsync(ViewModel.Comment);

                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    ModelState.AddModelError(string.Empty, errorMessage);

                    ViewModel = await _bloggService.GetBloggViewModelAsync(false, ViewModel.Comment?.BloggId ?? 0);

                    return Page();
                }
            }

            return RedirectToPage("./Arkiv", new { showId = ViewModel.Comment?.BloggId, commentId = ViewModel.Comment?.BloggId });
        }
    }
}
