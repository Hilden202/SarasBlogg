using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.Data;
using SarasBlogg.ViewModels;

namespace SarasBlogg.Pages
{
    public class IndexModel : PageModel
    {
        //public ApplicationUser MyUser { get; set; }

        private readonly ApplicationDbContext _context;
        //private UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context/*, UserManager<ApplicationUser> userManager*/)
        {
            _context = context;
            //_userManager = userManager;
        }
        [BindProperty]
        public BloggViewModel ViewModel { get; set; } = new();


        public async Task OnGetAsync(int showId, int id)
        {

            ViewModel.Bloggs = await _context.Blogg
                                .Where(b => !b.IsArchived && !b.Hidden && b.LaunchDate <= DateTime.Today)
                                .ToListAsync();

            ViewModel.IsArchiveView = false;

            if (showId != 0)
            {
                ViewModel.Blogg = await _context.Blogg.FirstOrDefaultAsync(b => b.Id == showId);

            }

            ViewModel.Comments = await DAL.CommentAPIManager.GetAllCommentsAsync();
            //if (id != 0)
            //{
            //    ViewModel.Comment = await DAL.CommentAPIManager.GetCommentAsync(id);

            //}

            //MyUser = await _userManager.GetUserAsync(User);
        }

        public async Task<IActionResult> OnPostAsync(int deleteCommentId)
        {
            if (deleteCommentId != 0)
            {
                var comment = await DAL.CommentAPIManager.GetCommentAsync(deleteCommentId);

                if (comment != null)
                {
                    await DAL.CommentAPIManager.DeleteCommentAsync(deleteCommentId);
                    return RedirectToPage("./Index", new { showId = comment.BloggId });
                }

            }
            if (ModelState.IsValid)
            {
                if (ViewModel.Comment?.Id == null)
                {
                    await DAL.CommentAPIManager.SaveCommentAsync(ViewModel.Comment);
                    //}
                    //else
                    //{
                    //    await DAL.CommentAPIManager.UpdateCommentAsync(ViewModel.Comment);
                    //}
                }

            }
            return RedirectToPage("./Index", new { showId = ViewModel.Comment?.BloggId });

        }
    }
}