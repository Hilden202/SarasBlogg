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
            => blogg.Images = await _imageApi.GetImagesByBloggIdAsync(blogg.Id);

        public async Task<BloggViewModel> GetBloggViewModelAsync(bool isArchive, int showId = 0)
        {
            var vm = new BloggViewModel();

            // Hämta alla (cache) och filtrera lokalt
            var all = await GetAllBloggsAsync(includeArchived: true);

            vm.Bloggs = all
                .Where(b => (isArchive ? b.IsArchived : !b.IsArchived)
                            && !b.Hidden
                            && b.LaunchDate <= DateTime.Today)
                .OrderByDescending(b => b.LaunchDate)
                .ThenByDescending(b => b.Id)
                .ToList();

            // Säkerställ att bilder finns på de som ska visas
            foreach (var b in vm.Bloggs)
                if (b.Images == null) await AttachImagesAsync(b);

            vm.IsArchiveView = isArchive;

            if (showId != 0)
            {
                var blogg = vm.Bloggs.FirstOrDefault(b => b.Id == showId);
                if (blogg == null)
                {
                    blogg = await _bloggApi.GetBloggAsync(showId);
                    if (blogg != null)
                    {
                        if (blogg.Images == null) await AttachImagesAsync(blogg);
                        vm.Bloggs.Add(blogg);
                    }
                }
                vm.Blogg = blogg;
            }

            vm.Comments = await _commentApi.GetAllCommentsAsync();
            return vm;
        }

        /// <summary>Hämtar alla bloggar (cache 3 min), filtrerar & laddar bilder för det som returneras.</summary>
        public async Task<List<Blogg>> GetAllBloggsAsync(bool includeArchived = false)
        {
            const string cacheKey = "blogg:list:all";
            if (!_cache.TryGetValue(cacheKey, out List<Blogg>? all))
            {
                try
                {
                    all = await _bloggApi.GetAllBloggsAsync(); // hämta rå-listan utan filter
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

            var filtered = all
                .Where(b => !b.Hidden
                            && (includeArchived || !b.IsArchived)
                            && b.LaunchDate <= DateTime.Today)
                .OrderByDescending(b => b.LaunchDate)
                .ThenByDescending(b => b.Id)
                .ToList();

            // Viktigt: attach:a bilder för de som faktiskt ska visas (t.ex. startsidan)
            foreach (var b in filtered)
                if (b.Images == null) await AttachImagesAsync(b);

            return filtered;
        }

        public async Task<string> SaveCommentAsync(Comment comment)
        {
            var forbidden = await _forbiddenWordApi.GetForbiddenPatternsAsync();
            foreach (var p in forbidden)
            {
                if (comment.Content.ContainsForbiddenWord(p)) return "Kommentaren innehåller otillåtet språk.";
                if (comment.Name.ContainsForbiddenWord(p)) return "Namnet innehåller otillåtet språk.";
            }
            return await _commentApi.SaveCommentAsync(comment);
        }

        public Task DeleteCommentAsync(int id) => _commentApi.DeleteCommentAsync(id);
        public Task<Comment?> GetCommentAsync(int id) => _commentApi.GetCommentAsync(id);

        public async Task UpdateViewCountAsync(int bloggId)
        {
            var blogg = await _bloggApi.GetBloggAsync(bloggId);
            if (blogg != null)
            {
                blogg.ViewCount++;
                await _bloggApi.UpdateBloggAsync(blogg);
            }
        }
    }
}
