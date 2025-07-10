using SarasBlogg.Data;
using SarasBlogg.ViewModels;
using SarasBlogg.Models;
using SarasBlogg.Extensions;
using SarasBlogg.DAL;

namespace SarasBlogg.Services
{
    public class BloggService
    {
        private readonly BloggAPIManager _bloggApi;

        private readonly CommentAPIManager _commentApi;

        private readonly ForbiddenWordAPIManager _forbiddenWordApi;

        public BloggService(BloggAPIManager bloggApi, CommentAPIManager commentApi, ForbiddenWordAPIManager forbiddenWordApi)
        {
            _bloggApi = bloggApi;
            _commentApi = commentApi;
            _forbiddenWordApi = forbiddenWordApi;
        }

        public async Task<ViewModels.BloggViewModel> GetBloggViewModelAsync(bool isArchive, int showId = 0)
        {
            var viewModel = new BloggViewModel();

            var allBloggs = await _bloggApi.GetAllBloggsAsync();

            viewModel.Bloggs = allBloggs
                .Where(b => (isArchive ? b.IsArchived : !b.IsArchived) && !b.Hidden && b.LaunchDate <= DateTime.Today)
                .ToList();

            viewModel.IsArchiveView = isArchive;

            if (showId != 0)
            {
                var blogg = await _bloggApi.GetBloggAsync(showId);

                if (blogg != null && (isArchive ? blogg.IsArchived : true) && !blogg.Hidden)
                {
                    viewModel.Blogg = blogg;
                }
            }

            viewModel.Comments = await _commentApi.GetAllCommentsAsync();

            return viewModel;
        }

        public async Task<string> SaveCommentAsync(Comment comment) // La till string för response både för API och regex
        {
            var forbiddenPatterns = await _forbiddenWordApi.GetForbiddenPatternsAsync();

            foreach (var pattern in forbiddenPatterns)
            {
                if (comment.Content.ContainsForbiddenWord(pattern))
                {
                    return "Kommentaren innehåller otillåtet språk.";
                }
                if (comment.Name.ContainsForbiddenWord(pattern))
                {
                    return "Namnet innehåller otillåtet språk.";
                }
            }
            return await _commentApi.SaveCommentAsync(comment);
        }

        public async Task DeleteCommentAsync(int commentId)
        {
            await _commentApi.DeleteCommentAsync(commentId);
        }

        public async Task<Comment?> GetCommentAsync(int commentId)
        {
            return await _commentApi.GetCommentAsync(commentId);
        }

        public async Task UpdateViewCountAsync(int bloggId)
        {
            var blogg = await _bloggApi.GetBloggAsync(bloggId);
            if (blogg != null)
            {
                blogg.ViewCount++;
                await _bloggApi.UpdateBloggAsync(blogg);
            }
        }

        public async Task<List<Blogg>> GetAllBloggsAsync()
        {
            var allBloggs = await _bloggApi.GetAllBloggsAsync();
            return allBloggs
                .Where(b => !b.Hidden && b.LaunchDate <= DateTime.Today)
                .ToList();
        }



    }
}
