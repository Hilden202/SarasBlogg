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
        private readonly BloggImageAPIManager _imageApi;

        public BloggService(
            BloggAPIManager bloggApi,
            CommentAPIManager commentApi,
            ForbiddenWordAPIManager forbiddenWordApi,
            BloggImageAPIManager imageApi)
        {
            _bloggApi = bloggApi;
            _commentApi = commentApi;
            _forbiddenWordApi = forbiddenWordApi;
            _imageApi = imageApi;
        }

        // 🔹 Hjälpmetod för att hämta bilder till en blogg
        private async Task AttachImagesAsync(Blogg blogg)
        {
            blogg.Images = await _imageApi.GetImagesByBloggIdAsync(blogg.Id);
        }

        public async Task<BloggViewModel> GetBloggViewModelAsync(bool isArchive, int showId = 0)
        {
            var viewModel = new BloggViewModel();

            var allBloggs = await _bloggApi.GetAllBloggsAsync();

            viewModel.Bloggs = allBloggs
                .Where(b => (isArchive ? b.IsArchived : !b.IsArchived) && !b.Hidden && b.LaunchDate <= DateTime.Today)
                .ToList();

            // 🔹 Hämta bilder för alla bloggar i listvyn
            foreach (var blogg in viewModel.Bloggs)
            {
                await AttachImagesAsync(blogg);
            }

            viewModel.IsArchiveView = isArchive;

            if (showId != 0)
            {
                var blogg = viewModel.Bloggs.FirstOrDefault(b => b.Id == showId);

                if (blogg == null)
                {
                    blogg = await _bloggApi.GetBloggAsync(showId);

                    if (blogg != null)
                    {
                        await AttachImagesAsync(blogg);
                        viewModel.Bloggs.Add(blogg);
                    }
                }

                viewModel.Blogg = blogg;
            }

            viewModel.Comments = await _commentApi.GetAllCommentsAsync();

            return viewModel;
        }

        public async Task<string> SaveCommentAsync(Comment comment)
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

        public async Task<List<Blogg>> GetAllBloggsAsync(bool includeArchived = false)
        {
            var allBloggs = await _bloggApi.GetAllBloggsAsync();
            var filtered = allBloggs
                .Where(b => !b.Hidden
                            && (includeArchived || !b.IsArchived)
                            && b.LaunchDate <= DateTime.Today)
                .ToList();

            foreach (var blogg in filtered)
            {
                await AttachImagesAsync(blogg);
            }

            return filtered;
        }
    }
}