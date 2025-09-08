using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.Services;
using SarasBlogg.ViewModels;
using SarasBlogg.DAL;
using System.Security.Claims;
using SarasBlogg.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Humanizer;

namespace SarasBlogg.Pages.Shared
{
    public abstract class BloggBasePageModel : PageModel
    {
        protected readonly BloggService _bloggService;
        protected readonly UserAPIManager _userApi;
        private readonly bool _isArchive;
        private LikeAPIManager LikeApi => HttpContext.RequestServices.GetRequiredService<LikeAPIManager>();

        protected BloggBasePageModel(BloggService bloggService, UserAPIManager userApi, bool isArchive)
        {
            _bloggService = bloggService;
            _userApi = userApi;
            _isArchive = isArchive;
        }

        [BindProperty]
        public BloggViewModel ViewModel { get; set; } = new();

        // Likes för aktuell post
        public int LikeCount { get; private set; }
        public bool IsLiked { get; private set; }

        // För inloggad skribent (formulär)
        public string RoleCss => GetRoleCss(User);

        // --- Rollrank + mapping ---
        private static readonly Dictionary<string, int> Rank = new(StringComparer.OrdinalIgnoreCase)
        {
            ["superadmin"] = 0,
            ["admin"] = 1,
            ["superuser"] = 2,
            ["user"] = 3
        };
        private static string GetTopRole(IEnumerable<string> roles)
            => roles.OrderBy(r => Rank.TryGetValue(r, out var i) ? i : 999).FirstOrDefault() ?? "";
        protected static string MapTopRoleToCss(string? top) => top?.ToLower() switch
        {
            "superadmin" => "role-superadmin",
            "admin" => "role-admin",
            "superuser" => "role-superuser",
            "user" => "role-user",
            _ => ""
        };
        protected static string GetRoleCss(ClaimsPrincipal user)
        {
            if (user.IsInRole("superadmin")) return "role-superadmin";
            if (user.IsInRole("admin")) return "role-admin";
            if (user.IsInRole("superuser")) return "role-superuser";
            if (user.IsInRole("user")) return "role-user";
            return string.Empty;
        }

        /// <summary>Fyller RoleCssByName för visad post baserat på TopRole från API:t.</summary>
        protected async Task HydrateRoleLookupsForCurrentPostAsync(BloggViewModel vm)
        {
            var postId = vm?.Blogg?.Id ?? 0;
            if (postId == 0 || vm?.Comments == null) return;

            var allUsers = await _userApi.GetAllUsersAsync();
            if (allUsers == null) return;

            var byUserName = allUsers
                .Where(u => !string.IsNullOrWhiteSpace(u.UserName))
                .GroupBy(u => u.UserName!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var names = vm.Comments
                .Where(c => c.BloggId == postId && !string.IsNullOrWhiteSpace(c.Name))
                .Select(c => c.Name!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var name in names)
            {
                if (!byUserName.TryGetValue(name, out var user) || user.Roles is null || user.Roles.Count == 0)
                    continue;
                var top = GetTopRole(user.Roles);
                var css = MapTopRoleToCss(top);
                if (!string.IsNullOrEmpty(css))
                    vm.RoleCssByName[name] = css;
            }
        }

        protected bool IsAuth => User?.Identity?.IsAuthenticated == true;
        protected string CurrentUserName => IsAuth ? (User?.Identity?.Name ?? "") : "";
        protected string CurrentUserEmail =>
            IsAuth
                ? (User.FindFirst(ClaimTypes.Email)?.Value
                   ?? User.FindFirst("email")?.Value
                   ?? "")
                : "";

        protected static bool HasAdminLikeRole(IEnumerable<string> roles) =>
            roles.Any(r => string.Equals(r, "superadmin", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(r, "admin", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(r, "superuser", StringComparison.OrdinalIgnoreCase));

        // PROD-fallback: hämta email/roller via API om cookies saknar claims
        protected async Task<(string? Email, List<string> Roles)> GetApiUserByNameAsync(string userNameOrEmail)
        {
            try
            {
                var all = await _userApi.GetAllUsersAsync();
                var u = all?.FirstOrDefault(x =>
                    (!string.IsNullOrWhiteSpace(x.UserName) && !string.IsNullOrWhiteSpace(userNameOrEmail) &&
                     string.Equals(x.UserName, userNameOrEmail, StringComparison.OrdinalIgnoreCase))
                    ||
                    (!string.IsNullOrWhiteSpace(x.Email) && !string.IsNullOrWhiteSpace(userNameOrEmail) &&
                     string.Equals(x.Email, userNameOrEmail, StringComparison.OrdinalIgnoreCase))
                );

                return (u?.Email, (u?.Roles ?? Enumerable.Empty<string>()).ToList());
            }
            catch
            {
                return (null, new List<string>());
            }
        }

        public async Task OnGetCoreAsync(int showId, int id, bool openComments)
        {
            if (showId != 0)
                await _bloggService.UpdateViewCountAsync(showId);

            ViewModel = await _bloggService.GetBloggViewModelAsync(_isArchive, showId);

            await HydrateRoleLookupsForCurrentPostAsync(ViewModel);

            ViewData["OpenComments"] = openComments;

            // Ladda likes för visad post (endast detaljvy)
            var currentId = ViewModel?.Blogg?.Id ?? showId;
            if (currentId > 0)
            {
                var dto = await LikeApi.GetAsync(currentId);
                LikeCount = dto?.Count ?? 0;
                IsLiked = dto?.Liked ?? false;

                // Gör värdena tillgängliga för _blogglist.cshtml (som har BloggViewModel som modell)
                ViewData["LikeCount"] = LikeCount;
                ViewData["IsLiked"] = IsLiked;
            }
        }

        public async Task<IActionResult> OnPostCoreAsync(int deleteCommentId)
        {
            // 1) Delete (admin eller ägare via namn/e-post)
            if (deleteCommentId != 0)
            {
                var existing = await _bloggService.GetCommentAsync(deleteCommentId);
                if (existing != null)
                {
                    var isAdmin = User.IsInRole("superadmin") || User.IsInRole("admin") || User.IsInRole("superuser");
                    if (!isAdmin && IsAuth && !string.IsNullOrWhiteSpace(CurrentUserName))
                    {
                        var (_, roles) = await GetApiUserByNameAsync(CurrentUserName);
                        if (HasAdminLikeRole(roles)) isAdmin = true;
                    }

                    var isOwner =
                        (!string.IsNullOrWhiteSpace(existing.Name) && !string.IsNullOrWhiteSpace(CurrentUserName) &&
                         string.Equals(existing.Name, CurrentUserName, StringComparison.OrdinalIgnoreCase))
                        ||
                        (!string.IsNullOrWhiteSpace(existing.Email) && !string.IsNullOrWhiteSpace(CurrentUserEmail) &&
                         string.Equals(existing.Email, CurrentUserEmail, StringComparison.OrdinalIgnoreCase));

                    if (!isAdmin && !isOwner)
                    {
                        TempData["Error"] = "Du får inte ta bort denna kommentar.";
                        return RedirectToPage(pageName: null, pageHandler: null,
                            routeValues: new { showId = existing.BloggId, openComments = true }, fragment: "comments");
                    }

                    await _bloggService.DeleteCommentAsync(deleteCommentId);
                    TempData["Info"] = "Kommentar borttagen.";
                    return RedirectToPage(pageName: null, pageHandler: null,
                        routeValues: new { showId = existing.BloggId, openComments = true }, fragment: "comments");
                }
            }

            // 2) Inloggad? Tvinga namn+ev. e-post
            if (IsAuth && ViewModel?.Comment != null)
            {
                ViewModel.Comment.Name = CurrentUserName;

                var email = CurrentUserEmail;
                if (string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(CurrentUserName))
                {
                    var (apiEmail, _) = await GetApiUserByNameAsync(CurrentUserName);
                    email = apiEmail ?? "";
                }
                ViewModel.Comment.Email = email;

                // rensa ModelState så overrides går igenom
                ModelState.Remove("ViewModel.Comment.Name");
                ModelState.Remove("Comment.Name");
                ModelState.Remove("ViewModel.Comment.Email");
                ModelState.Remove("Comment.Email");
            }

            // 3) UTC-tid för nya kommentarer
            if (ViewModel?.Comment != null && ViewModel.Comment.Id == null)
                ViewModel.Comment.CreatedAt = DateTime.UtcNow;

            // 3b) Anonymt namn: normalisera bara (valfritt)
            if (!IsAuth && ViewModel?.Comment != null && ViewModel.Comment.Id == null)
            {
                var proposed = (ViewModel.Comment.Name ?? "").Trim();
                if (string.IsNullOrWhiteSpace(proposed)) proposed = "Gäst";
                ViewModel.Comment.Name = proposed;
                ModelState.Remove("ViewModel.Comment.Name");
                ModelState.Remove("Comment.Name");
            }

            // 4) Validera + spara
            if (ModelState.IsValid && ViewModel?.Comment?.Id == null)
            {
                string errorMessage = await _bloggService.SaveCommentAsync(ViewModel.Comment);
                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    ModelState.AddModelError("Comment.Content", errorMessage);

                    ViewModel = await _bloggService.GetBloggViewModelAsync(_isArchive, ViewModel.Comment?.BloggId ?? 0);

                    await HydrateRoleLookupsForCurrentPostAsync(ViewModel);

                    ViewData["OpenComments"] = true;
                    return Page();
                }
            }

            // 5) Tillbaka till samma inlägg
            return RedirectToPage(pageName: null, pageHandler: null,
                routeValues: new { showId = ViewModel?.Comment?.BloggId, openComments = true }, fragment: "comments");

            // 🔄 Toggle Like

        }
        public async Task<IActionResult> OnPostLikeToggleCoreAsync(int showId)
        {
            if (showId <= 0)
                return RedirectToPage(pageName: null, pageHandler: null, routeValues: new { showId }, fragment: "likes");

            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                TempData["Error"] = "Logga in för att gilla.";
                return RedirectToPage(pageName: null, pageHandler: null, routeValues: new { showId }, fragment: "likes");
            }

            var current = await LikeApi.GetAsync(showId);

            LikeDto? dto = current?.Liked == true
                ? await LikeApi.RemoveAsync(showId)
                : await LikeApi.AddAsync(showId, "_"); // "_" räcker; servern tar userId från JWT-claims

            LikeCount = dto?.Count ?? current?.Count ?? 0;
            IsLiked = dto?.Liked ?? current?.Liked ?? false;

            // Skicka med till vyn (partialen har BloggViewModel, så den läser ViewData)
            ViewData["LikeCount"] = LikeCount;
            ViewData["IsLiked"] = IsLiked;

            return RedirectToPage(pageName: null, pageHandler: null, routeValues: new { showId }, fragment: "likes");
        }
    }
}