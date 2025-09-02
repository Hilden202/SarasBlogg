using Microsoft.AspNetCore.Mvc;
using SarasBlogg.Services;
using SarasBlogg.DAL;
using SarasBlogg.Pages.Shared;

namespace SarasBlogg.Pages
{
    public class BloggModel : BloggBasePageModel
    {
        public BloggModel(BloggService bloggService, UserAPIManager userApi)
            : base(bloggService, userApi, isArchive: false) { }

        public Task OnGetAsync(int showId, int id, bool openComments = false)
            => OnGetCoreAsync(showId, id, openComments);

        public Task<IActionResult> OnPostAsync(int deleteCommentId)
            => OnPostCoreAsync(deleteCommentId);
    }
}
