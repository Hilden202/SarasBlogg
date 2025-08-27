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

        private static string MapTopRoleToCss(string? top) => top?.ToLower() switch
        {
            "superadmin" => "role-superadmin",
            "admin" => "role-admin",
            "superuser" => "role-superuser",
            "user" => "role-user",
            _ => ""
        };

        private async Task AttachImagesAsync(Blogg blogg)
            => blogg.Images = await _imageApi.GetImagesByBloggIdAsync(blogg.Id);

        public async Task<BloggViewModel> GetBloggViewModelAsync(bool isArchive, int showId = 0)
        {
            var vm = new BloggViewModel();

            // Svensk "nu"-tid för filtrering/sortering
            var nowSe = DateTime.UtcNow.ToSwedishTime();

            // Hämta alla (cache) och filtrera lokalt
            var all = await GetAllBloggsAsync(includeArchived: true);

            vm.Bloggs = all
                .Where(b => (isArchive ? b.IsArchived : !b.IsArchived)
                            && !b.Hidden
                            && b.LaunchDate.ToSwedishTime() <= nowSe)
                .OrderByDescending(b => b.LaunchDate.ToSwedishTime())
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

            vm.RoleCssByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Vi behöver kommentarer + top-roll för att räkna och sätta färger.
            // När vi visar en specifik blogg → hämta bara de kommentarerna.
            // När vi visar listning/startsida → hämta alla (för räkningen per blogg).
            if (vm.Blogg is not null && vm.Blogg.Id != 0)
            {
                var dtos = await _commentApi.GetByBloggWithRolesAsync(vm.Blogg.Id);

                vm.Comments = dtos.Select(d => new Comment
                {
                    Id = d.Id,
                    BloggId = d.BloggId,
                    Name = d.Name,
                    Content = d.Content ?? "",
                    CreatedAt = d.CreatedAt
                }).ToList();

                foreach (var d in dtos.Where(d => !string.IsNullOrWhiteSpace(d.Name)))
                {
                    var css = MapTopRoleToCss(d.TopRole);
                    if (!string.IsNullOrEmpty(css))
                        vm.RoleCssByName[d.Name] = css;
                }
            }
            else
            {
                var dtos = await _commentApi.GetAllCommentsWithRolesAsync();

                vm.Comments = dtos.Select(d => new Comment
                {
                    Id = d.Id,
                    BloggId = d.BloggId,
                    Name = d.Name,
                    Content = d.Content ?? "",
                    CreatedAt = d.CreatedAt
                }).ToList();

                // Färg för de namn som förekommer i listningen (ofarligt att fylla upp = liten dict)
                foreach (var d in dtos.Where(d => !string.IsNullOrWhiteSpace(d.Name)))
                {
                    var css = MapTopRoleToCss(d.TopRole);
                    if (!string.IsNullOrEmpty(css))
                        vm.RoleCssByName[d.Name] = css;
                }
            }

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

            // Svensk "nu"-tid för filtrering/sortering
            var nowSe = DateTime.UtcNow.ToSwedishTime();

            var filtered = all
                .Where(b => !b.Hidden
                            && (includeArchived || !b.IsArchived)
                            && b.LaunchDate.ToSwedishTime() <= nowSe)
                .OrderByDescending(b => b.LaunchDate.ToSwedishTime())
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
