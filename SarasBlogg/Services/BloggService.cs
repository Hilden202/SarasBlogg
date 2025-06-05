using Microsoft.EntityFrameworkCore;
using SarasBlogg.Data;
using SarasBlogg.ViewModels;
using SarasBlogg.Models;

namespace SarasBlogg.Services
{
    public class BloggService
    {
        private readonly ApplicationDbContext _context;

        public BloggService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ViewModels.BloggViewModel> GetBloggViewModelAsync(bool isArchive, int showId = 0)
        {
            var viewModel = new BloggViewModel();

            viewModel.Bloggs = await _context.Blogg
                .Where(b => (isArchive ? b.IsArchived : !b.IsArchived) && !b.Hidden && b.LaunchDate <= DateTime.Today)
                .ToListAsync();

            viewModel.IsArchiveView = isArchive;

            if (showId != 0)
            {
                viewModel.Blogg = await _context.Blogg
                    .FirstOrDefaultAsync(b => b.Id == showId && (isArchive ? b.IsArchived : true) && !b.Hidden);
            }

            viewModel.Comments = await DAL.CommentAPIManager.GetAllCommentsAsync();

            return viewModel;
        }

        public async Task SaveCommentAsync(Comment comment)
        {
            await DAL.CommentAPIManager.SaveCommentAsync(comment);
        }

        public async Task DeleteCommentAsync(int commentId)
        {
            await DAL.CommentAPIManager.DeleteCommentAsync(commentId);
        }

        public async Task<Comment?> GetCommentAsync(int commentId)
        {
            return await DAL.CommentAPIManager.GetCommentAsync(commentId);
        }

        public async Task UpdateViewCountAsync(int bloggId)
        {
            var blogg = await _context.Blogg.FindAsync(bloggId);
            if (blogg != null)
            {
                blogg.ViewCount++;
                await _context.SaveChangesAsync();
            }
        }

    }
}
