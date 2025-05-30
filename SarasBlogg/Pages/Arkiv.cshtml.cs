using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.Data;
using SarasBlogg.ViewModels;

namespace SarasBlogg.Pages
{
    public class ArkivModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ArkivModel(ApplicationDbContext context)
        {
            _context = context;
        }
        [BindProperty]
        public BloggViewModel ViewModel { get; set; } = new();

        public async Task OnGetAsync(int showId, int id)
        {
            ViewModel.Bloggs = await _context.Blogg
                .Where(b => b.IsArchived && !b.Hidden && b.LaunchDate <= DateTime.Today)
                .ToListAsync();

            ViewModel.IsArchiveView = true;

            if (showId != 0)
            {
                ViewModel.Blogg = await _context.Blogg.FirstOrDefaultAsync(b => b.Id == showId && b.IsArchived && !b.Hidden);
            }

            ViewModel.Comments = await DAL.CommentAPIManager.GetAllCommentsAsync();
            //if (id != 0)
            //{
            //    ViewModel.Comment = await DAL.CommentAPIManager.GetCommentAsync(id);

            //}
        }

        public async Task<IActionResult> OnPostAsync(int deleteCommentId)
        {
            if (deleteCommentId != 0)
            {
                var comment = await DAL.CommentAPIManager.GetCommentAsync(deleteCommentId);

                if (comment != null)
                {
                    await DAL.CommentAPIManager.DeleteCommentAsync(deleteCommentId);
                    return RedirectToPage("./Arkiv", new { showId = comment.BloggId });
                }
            }
            // ? Lägg till denna kontroll
            if (!ModelState.IsValid)
            {
                // Ladda om innehållet igen så modellen har data för återvisning
                ViewModel.Bloggs = await _context.Blogg
                    .Where(b => b.IsArchived && !b.Hidden && b.LaunchDate <= DateTime.Today)
                    .ToListAsync();

                if (ViewModel.Comment?.BloggId != 0)
                {
                    ViewModel.Blogg = await _context.Blogg.FirstOrDefaultAsync(b =>
                        b.Id == ViewModel.Comment.BloggId &&
                        b.IsArchived && !b.Hidden);
                }

                ViewModel.Comments = await DAL.CommentAPIManager.GetAllCommentsAsync();

                return Page(); // återvisa formuläret med valideringsfel
            }

            await DAL.CommentAPIManager.SaveCommentAsync(ViewModel.Comment);
            return RedirectToPage("./Arkiv", new { showId = ViewModel.Comment?.BloggId, commentedId = ViewModel.Comment?.BloggId });
        }

    }
}
