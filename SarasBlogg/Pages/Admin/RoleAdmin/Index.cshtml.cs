using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DTOs;
using SarasBlogg.DAL;
using Microsoft.AspNetCore.Mvc;

namespace SarasBlogg.Pages.Admin.RoleAdmin
{
    public class IndexModel : PageModel
    {
        public List<UserDto> Users { get; set; } = new();
        public List<string> Roles { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string RoleName { get; set; }
        [BindProperty(SupportsGet = true)] public string AddUserId { get; set; }
        [BindProperty(SupportsGet = true)] public string RemoveUserId { get; set; }
        [BindProperty] public string DeleteUserId { get; set; }
        [BindProperty] public string DeleteRoleName { get; set; }
        [BindProperty] public string TargetUserId { get; set; } = "";
        [BindProperty] public string NewUserName { get; set; } = "";

        private readonly UserAPIManager _userApiManager;
        public IndexModel(UserAPIManager userApiManager) => _userApiManager = userApiManager;

        public async Task OnGetAsync()
        {
            if (!string.IsNullOrEmpty(AddUserId))
                await _userApiManager.AddUserToRoleAsync(AddUserId, RoleName);

            if (!string.IsNullOrEmpty(RemoveUserId))
                await _userApiManager.RemoveUserFromRoleAsync(RemoveUserId, RoleName);

            // Kolumnordning (sidleds)
            var rankColumns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["user"] = 0,
                ["superuser"] = 1,
                ["admin"] = 2,
                ["superadmin"] = 3
            };

            Roles = (await _userApiManager.GetAllRolesAsync())
                .OrderBy(r => rankColumns.TryGetValue(r, out var i) ? i : 999)
                .ThenBy(r => r)
                .ToList();

            var users = await _userApiManager.GetAllUsersAsync();

            // Radordning (lodrätt) – topproll: superadmin (0) bäst
            var rankUsers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["superadmin"] = 0,
                ["admin"] = 1,
                ["superuser"] = 2,
                ["user"] = 3
            };

            int UserTopRank(IList<string>? roles) =>
                roles?.Select(r => rankUsers.TryGetValue(r, out var i) ? i : 999)
                     .DefaultIfEmpty(999)
                     .Min() ?? 999;

            int RoleCountDistinct(IList<string>? roles) =>
                roles?.Distinct(StringComparer.OrdinalIgnoreCase).Count() ?? 0;

            bool IsSystemAdmin(string? email) =>
                string.Equals(email ?? "", "admin@sarasblogg.se", StringComparison.OrdinalIgnoreCase);

            Users = users
                .OrderBy(u => IsSystemAdmin(u.Email) ? 0 : 1)                 // systemkontot först
                .ThenBy(u => UserTopRank(u.Roles))                              // sedan på topproll (superadmin vinner)
                .ThenByDescending(u => RoleCountDistinct(u.Roles))              // inom samma topproll: fler roller vinner
                .ThenBy(u => u.UserName ?? u.Email ?? string.Empty)             // stabilitet
                .ToList();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!string.IsNullOrWhiteSpace(RoleName))
                await _userApiManager.CreateRoleAsync(RoleName);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync()
        {
            var user = await _userApiManager.GetUserByIdAsync(DeleteUserId);
            if (user != null && (user.Email ?? "").ToLower() != "admin@sarasblogg.se")
                await _userApiManager.DeleteUserAsync(DeleteUserId);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteRoleAsync()
        {
            if (!string.IsNullOrWhiteSpace(DeleteRoleName) && DeleteRoleName.ToLower() != "superadmin")
                await _userApiManager.DeleteRoleAsync(DeleteRoleName);

            return RedirectToPage();
        }

        // NYTT: byt användarnamn
        public async Task<IActionResult> OnPostChangeUserNameAsync()
        {
            if (string.IsNullOrWhiteSpace(TargetUserId) || string.IsNullOrWhiteSpace(NewUserName))
                return RedirectToPage();

            // UI-skydd (API:n bör ändå kräva Superadmin)
            if (!User.IsInRole("superadmin"))
                return Forbid();

            var result = await _userApiManager.ChangeUserNameAsync(TargetUserId, NewUserName);
            return RedirectToPage();
        }
    }
}
