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
        private const string CacheKeyAll = "blogg:list:all";

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

        /// <summary>Invalidera listcachen så att publika listor uppdateras direkt efter admin-ändringar.</summary>
        public void InvalidateBlogListCache() => _cache.Remove(CacheKeyAll);

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
            vm.VerifiedCommentIds ??= new HashSet<int>(); // säkra init

            if (vm.Blogg is not null && vm.Blogg.Id != 0)
            {
                // Endast kommentarer för visad blogg
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

                    if (!string.IsNullOrWhiteSpace(d.TopRole))
                        vm.VerifiedCommentIds.Add(d.Id);
                }
            }
            else
            {
                // Lista-läget: alla kommentarer för att kunna visa räknare / rollfärg i korten
                var dtos = await _commentApi.GetAllCommentsWithRolesAsync();

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

                    if (!string.IsNullOrWhiteSpace(d.TopRole))
                        vm.VerifiedCommentIds.Add(d.Id);
                }
            }

            return vm;
        }

        /// <summary>
        /// Hämtar alla bloggar (IMemoryCache ~45s), filtrerar & laddar bilder för det som returneras.
        /// Sätt bypassCache=true för att forcera färsk hämtning (t.ex. direkt efter admin-ändring).
        /// </summary>
        public async Task<List<Blogg>> GetAllBloggsAsync(bool includeArchived = false, bool bypassCache = false)
        {
            if (bypassCache)
            {
                try { return await FetchAndFilterAsync(includeArchived); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Bypass misslyckades, faller tillbaka på cache.");
                }
            }

            if (!_cache.TryGetValue(CacheKeyAll, out List<Blogg>? all))
            {
                try
                {
                    all = await _bloggApi.GetAllBloggsAsync(); // rå-lista utan filter
                    _cache.Set(CacheKeyAll, all, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(45)
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

            var filtered = FilterClientSide(all, includeArchived);

            // Attach:a bilder endast för de som faktiskt visas
            foreach (var b in filtered)
                if (b.Images == null) await AttachImagesAsync(b);

            return filtered;
        }

        private static List<Blogg> FilterClientSide(IEnumerable<Blogg> all, bool includeArchived)
        {
            var nowSe = DateTime.UtcNow.ToSwedishTime();
            return all
                .Where(b => !b.Hidden
                            && (includeArchived || !b.IsArchived)
                            && b.LaunchDate.ToSwedishTime() <= nowSe)
                .OrderByDescending(b => b.LaunchDate.ToSwedishTime())
                .ThenByDescending(b => b.Id)
                .ToList();
        }

        private async Task<List<Blogg>> FetchAndFilterAsync(bool includeArchived)
        {
            var all = await _bloggApi.GetAllBloggsAsync();
            return FilterClientSide(all ?? Enumerable.Empty<Blogg>(), includeArchived);
        }

        public async Task<string> SaveCommentAsync(Comment comment)
        {
            if (comment is null)
                return "Ogiltig kommentar.";

            // Normalisera fält så vi slipper NRE
            comment.Content ??= string.Empty;
            comment.Name ??= string.Empty;

            var forbidden = await _forbiddenWordApi.GetForbiddenPatternsAsync();

            foreach (var p in forbidden)
            {
                if (comment.Content.ContainsForbiddenWord(p))
                    return "Kommentaren innehåller otillåtet språk.";
                if (comment.Name.ContainsForbiddenWord(p))
                    return "Namnet innehåller otillåtet språk.";
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
