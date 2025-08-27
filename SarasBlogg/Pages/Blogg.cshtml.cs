using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.Services;
using SarasBlogg.ViewModels;
using SarasBlogg.DAL;
using Humanizer;
using SarasBlogg.Extensions; // för ev. ToSwedishTime i framtiden

namespace SarasBlogg.Pages
{
    public class BloggModel : PageModel
    {
        private readonly BloggService _bloggService;
        private readonly UserAPIManager _userApi;

        public BloggModel(BloggService bloggService, UserAPIManager userApi)
        {
            _bloggService = bloggService;
            _userApi = userApi;
        }

        [BindProperty]
        public BloggViewModel ViewModel { get; set; } = new();

        // För inloggad skribent (formuläret)
        //public string RoleSymbol => GetRoleSymbol();
        public string RoleCss => GetRoleCss();

        private bool IsAuth => User?.Identity?.IsAuthenticated == true;
        private string CurrentUserName => IsAuth ? (User?.Identity?.Name ?? "") : "";

        // Ikoner (unicode)
        //private string GetRoleSymbol()
        //{
        //    if (User.IsInRole("superadmin")) return "\U0001F451"; // 👑
        //    if (User.IsInRole("admin")) return "\u2B50";          // ⭐
        //    if (User.IsInRole("superuser")) return "\u26A1";      // ⚡
        //    if (User.IsInRole("user")) return "\U0001F338";       // 🌸
        //    return "";
        //}

        private string GetRoleCss()
        {
            if (User.IsInRole("superadmin")) return "role-superadmin";
            if (User.IsInRole("admin")) return "role-admin";
            if (User.IsInRole("superuser")) return "role-superuser";
            if (User.IsInRole("user")) return "role-user";
            return string.Empty;
        }

        // Hjälp för att visa färg/ikon för ALLA kommentarer (även för utloggade)
        private static readonly Dictionary<string, int> Rank = new(StringComparer.OrdinalIgnoreCase)
        {
            ["superadmin"] = 0,
            ["admin"] = 1,
            ["superuser"] = 2,
            ["user"] = 3
        };

        private static string GetTopRole(IEnumerable<string> roles) =>
            roles.OrderBy(r => Rank.TryGetValue(r, out var i) ? i : 999).FirstOrDefault() ?? "";

        //private static (string css, string sym) MapTopRole(string? top) =>
        //    top?.ToLower() switch
        //    {
        //        "superadmin" => ("role-superadmin", "\U0001F451"), // 👑
        //        "admin" => ("role-admin", "\u2B50"),               // ⭐
        //        "superuser" => ("role-superuser", "\u26A1"),       // ⚡
        //        "user" => ("role-user", "\U0001F338"),             // 🌸
        //        _ => ("", "")
        //    };
        private static string MapTopRoleToCss(string? top) => top?.ToLower() switch
        {
            "superadmin" => "role-superadmin",
            "admin" => "role-admin",
            "superuser" => "role-superuser",
            "user" => "role-user",
            _ => ""
        };


        private async Task HydrateRoleLookupsForCurrentPostAsync()
        {
            var postId = ViewModel.Blogg?.Id ?? 0;
            if (postId == 0 || ViewModel.Comments == null) return;

            // Hämta alla användare en gång
            var allUsers = await _userApi.GetAllUsersAsync();
            if (allUsers == null) return;

            // Bygg uppslag per UserName (case-insensitive)
            var byUserName = allUsers
                .Where(u => !string.IsNullOrWhiteSpace(u.UserName))
                .GroupBy(u => u.UserName!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // Samla alla unika namn i denna posts kommentarer
            var names = ViewModel.Comments
                .Where(c => c.BloggId == postId && !string.IsNullOrWhiteSpace(c.Name))
                .Select(c => c.Name!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var name in names)
            {
                if (!byUserName.TryGetValue(name, out var user) || user.Roles == null || user.Roles.Count == 0)
                    continue;

                var top = GetTopRole(user.Roles);
                //var (css, sym) = MapTopRole(top);

                //if (!string.IsNullOrEmpty(css))
                //    ViewModel.RoleCssByName[name] = css;

                //if (!string.IsNullOrEmpty(sym))
                //    ViewModel.RoleSymbolByName[name] = sym;
                var css = MapTopRoleToCss(top);
                if (!string.IsNullOrEmpty(css))
                    ViewModel.RoleCssByName[name] = css;

            }
        }

        public async Task OnGetAsync(int showId, int id)
        {
            if (showId != 0)
                await _bloggService.UpdateViewCountAsync(showId);

            ViewModel = await _bloggService.GetBloggViewModelAsync(false, showId);

            // för formuläret (inloggad skribent)
            //ViewModel.RoleSymbol = GetRoleSymbol();
            ViewModel.RoleCss = GetRoleCss();

        }

        public async Task<IActionResult> OnPostAsync(int deleteCommentId)
        {
            // 1) Delete
            if (deleteCommentId != 0)
            {
                var existing = await _bloggService.GetCommentAsync(deleteCommentId);
                if (existing != null)
                {
                    await _bloggService.DeleteCommentAsync(deleteCommentId);
                    return RedirectToPage(pageName: null, pageHandler: null,
                        routeValues: new { showId = existing.BloggId }, fragment: "comments");
                }
            }

            // 2) Tvinga namn = inloggat UserName
            if (IsAuth && ViewModel?.Comment != null)
            {
                ViewModel.Comment.Name = CurrentUserName;
                ModelState.Remove("ViewModel.Comment.Name");
                ModelState.Remove("Comment.Name");
            }

            // 3) CreatedAt i UTC för nya kommentarer (exakt tidpunkt)
            if (ViewModel?.Comment != null && ViewModel.Comment.Id == null)
                ViewModel.Comment.CreatedAt = DateTime.UtcNow; // Kind=Utc

            // 4) Validera + spara
            if (ModelState.IsValid && ViewModel?.Comment?.Id == null)
            {
                string errorMessage = await _bloggService.SaveCommentAsync(ViewModel.Comment);
                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    ModelState.AddModelError("Comment.Content", errorMessage);

                    ViewModel = await _bloggService.GetBloggViewModelAsync(false, ViewModel.Comment?.BloggId ?? 0);

                    // för formuläret
                    //ViewModel.RoleSymbol = GetRoleSymbol();
                    ViewModel.RoleCss = GetRoleCss();

                    return Page();
                }
            }

            // 5) Tillbaka till samma blogg
            return RedirectToPage(pageName: null, pageHandler: null,
                routeValues: new { showId = ViewModel?.Comment?.BloggId }, fragment: "comments");
        }
    }
}
