using SarasBlogg.ViewModels;
using SarasBlogg.Models;
using SarasBlogg.Extensions;
using SarasBlogg.DAL;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SarasBlogg.Services
{
    public class BloggService
    {
        private readonly BloggAPIManager _bloggApi;
        private readonly CommentAPIManager _commentApi;
        private readonly ForbiddenWordAPIManager _forbiddenWordApi;
        private readonly BloggImageAPIManager _imageApi;
        private readonly IMemoryCache _cache;
        private readonly ILogger<BloggService> _logger;

        public BloggService(
            BloggAPIManager bloggApi,
            CommentAPIManager commentApi,
            IMemoryCache cache,
            ForbiddenWordAPIManager forbiddenWordApi,
            BloggImageAPIManager imageApi,
            ILogger<BloggService> logger)
        {
            _bloggApi = bloggApi;
            _commentApi = commentApi;
            _cache = cache;
            _forbiddenWordApi = forbiddenWordApi;
            _imageApi = imageApi;
            _logger = logger;
        }

        private async Task AttachImagesAsync(Blogg blogg)
        {
            blogg.Images = await _imageApi.GetImagesByBloggIdAsync(blogg.Id);
        }

        public async Task<BloggViewModel> GetBloggViewModelAsync(bool isArchive, int showId = 0)
        {
            var viewModel = new BloggViewModel();

            // Hämta ALLA (cacheat) och filtrera lokalt
            var allBloggs = await GetAllBloggsAsync(includeArchived: true);

            viewModel.Bloggs = allBloggs
                .Where(b => (isArchive ? b.IsArchived : !b.IsArchived)
                            && !b.Hidden
                            && b.LaunchDate <= DateTime.Today)
                .ToList();

            foreach (var blogg in viewModel.Bloggs)
                await AttachImagesAsync(blogg);

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
                    return "Kommentaren innehåller otillåtet språk.";
                if (comment.Name.ContainsForbiddenWord(pattern))
                    return "Namnet innehåller otillåtet språk.";
            }
            return await _commentApi.SaveCommentAsync(comment);
        }

        public Task DeleteCommentAsync(int commentId) => _commentApi.DeleteCommentAsync(commentId);

        public Task<Comment?> GetCommentAsync(int commentId) => _commentApi.GetCommentAsync(commentId);

        public async Task UpdateViewCountAsync(int bloggId)
        {
            var blogg = await _bloggApi.GetBloggAsync(bloggId);
            if (blogg != null)
            {
                blogg.ViewCount++;
                await _bloggApi.UpdateBloggAsync(blogg);
            }
        }

        /// <summary>
        /// Hämtar ALLA bloggar (cacheas i 3 min) och filtrerar på includeArchived.
        /// </summary>
        public async Task<List<Blogg>> GetAllBloggsAsync(bool includeArchived)
        {
            const string cacheKey = "blogg:list:all";

            List<Blogg>? all;

            if (!_cache.TryGetValue(cacheKey, out all))
            {
                try
                {
                    // ❗ Anropa API:t UTAN bool-parameter (det var orsaken till "rött")
                    all = await _bloggApi.GetAllBloggsAsync();

                    _cache.Set(cacheKey, all, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
                    });
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "API-fel – visar ev. cache.");
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogWarning(ex, "API-timeout – visar ev. cache.");
                }
            }

            all ??= new List<Blogg>();

            // Returnera enligt parametern
            return includeArchived ? all : all.Where(b => !b.IsArchived).ToList();
        }
    }
}
