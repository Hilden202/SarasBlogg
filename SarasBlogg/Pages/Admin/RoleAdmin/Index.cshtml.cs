using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using SarasBlogg.DAL;
using SarasBlogg.DTOs;

namespace SarasBlogg.Pages.Admin.RoleAdmin
{
    public class IndexModel : PageModel
    {
        public List<UserDto> Users { get; set; } = new();
        public List<string> Roles { get; set; } = new();

        // Roller fr�n API f�r den inloggade
        public List<string> ApiRoles { get; private set; } = new();
        public bool IsApiAdminOrSuper { get; private set; }
        public bool IsApiSuperadmin { get; private set; }
        public bool IsSystemAdmin(string? email)
           => string.Equals(email ?? "", "admin@sarasblogg.se", StringComparison.OrdinalIgnoreCase);

        [BindProperty(SupportsGet = true)] public string RoleName { get; set; }
        [BindProperty(SupportsGet = true)] public string AddUserId { get; set; }
        [BindProperty(SupportsGet = true)] public string RemoveUserId { get; set; }
        [BindProperty] public string DeleteUserId { get; set; }
        [BindProperty] public string DeleteRoleName { get; set; }
        [BindProperty] public string TargetUserId { get; set; } = "";
        [BindProperty] public string NewUserName { get; set; } = "";

        private readonly UserAPIManager _userApiManager;
        public IndexModel(UserAPIManager userApiManager) => _userApiManager = userApiManager;

        private async Task<bool> IsApiSuperadminAsync()
        {
            var me = await _userApiManager.GetMeAsync();
            return me?.Roles?.Contains("superadmin", StringComparer.OrdinalIgnoreCase) == true;
        }
        public async Task OnGetAsync()
        {
            // 1) H�mta f�rska roller f�r den inloggade fr�n API:t
            var me = await _userApiManager.GetMeAsync();
            ApiRoles = me?.Roles?.ToList() ?? new List<string>();
            IsApiSuperadmin = ApiRoles.Contains("superadmin", StringComparer.OrdinalIgnoreCase);
            IsApiAdminOrSuper = IsApiSuperadmin ||
                                ApiRoles.Contains("admin", StringComparer.OrdinalIgnoreCase);

            // 2) Hantera ev. add/remove l�nkar (endast superadmin f�r trigga)
            if (IsApiSuperadmin)
            {
                if (!string.IsNullOrEmpty(AddUserId) && !string.IsNullOrEmpty(RoleName))
                    await _userApiManager.AddUserToRoleAsync(AddUserId, RoleName);

                if (!string.IsNullOrEmpty(RemoveUserId) && !string.IsNullOrEmpty(RoleName))
                    await _userApiManager.RemoveUserFromRoleAsync(RemoveUserId, RoleName);
            }

            // 3) Ladda roller (kolumnordning)
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

            // 4) Ladda anv�ndare (radordning)
            var users = await _userApiManager.GetAllUsersAsync();

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

            bool IsSystemAdminEmail(string? email) =>
                string.Equals(email ?? "", "admin@sarasblogg.se", StringComparison.OrdinalIgnoreCase);

            Users = users
                .OrderBy(u => IsSystemAdminEmail(u.Email) ? 0 : 1)             // systemkontot f�rst
                .ThenBy(u => UserTopRank(u.Roles))                              // sedan p� topproll
                .ThenByDescending(u => RoleCountDistinct(u.Roles))              // fler roller vinner
                .ThenBy(u => u.UserName ?? u.Email ?? string.Empty)             // stabilitet
                .ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (await IsApiSuperadminAsync() && !string.IsNullOrWhiteSpace(RoleName))
                await _userApiManager.CreateRoleAsync(RoleName);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync()
        {
            if (await IsApiSuperadminAsync() && !string.IsNullOrWhiteSpace(DeleteUserId))
            {
                var user = await _userApiManager.GetUserByIdAsync(DeleteUserId);
                if (user != null && !IsSystemAdminEmail(user.Email))
                    await _userApiManager.DeleteUserAsync(DeleteUserId);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteRoleAsync()
        {
            if (await IsApiSuperadminAsync() &&
                !string.IsNullOrWhiteSpace(DeleteRoleName) &&
                !string.Equals(DeleteRoleName, "superadmin", StringComparison.OrdinalIgnoreCase))
            {
                await _userApiManager.DeleteRoleAsync(DeleteRoleName);
            }

            return RedirectToPage();
        }

        // Byt anv�ndarnamn (endast superadmin)
        public async Task<IActionResult> OnPostChangeUserNameAsync()
        {
            if (string.IsNullOrWhiteSpace(TargetUserId) || string.IsNullOrWhiteSpace(NewUserName))
                return RedirectToPage();

            if (!await IsApiSuperadminAsync())
                return Forbid();

            var user = await _userApiManager.GetUserByIdAsync(TargetUserId);
            if (user != null && IsSystemAdminEmail(user.Email))
                return Forbid();

            await _userApiManager.ChangeUserNameAsync(TargetUserId, NewUserName);
            return RedirectToPage();
        }
        
        public async Task<IActionResult> OnPostSendResetLinkAsync(string TargetUserEmail)
        {
            // ✅ Bara superadmin får göra detta
            if (!await IsApiSuperadminAsync())
                return Forbid();

            // 🚫 Systemkontot får aldrig hanteras
            if (IsSystemAdmin(TargetUserEmail))
                return RedirectToPage();

            try
            {
                var result = await _userApiManager.SendResetLinkAsync(TargetUserEmail);

                // 🔗 DEV-läge eller fallback: länken returneras i ConfirmEmailUrl
                if (!string.IsNullOrEmpty(result?.ConfirmEmailUrl))
                    TempData["ManualResetLink"] = result.ConfirmEmailUrl;
                else if (result?.Succeeded == true)
                    TempData["SuccessMessage"] = result.Message;
                else
                    TempData["ErrorMessage"] = result?.Message ?? "Fel vid försök att skicka återställningslänk.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Fel vid försök att skicka återställningslänk till {TargetUserEmail}: {ex.Message}";
            }

            // Ladda om sidan för att visa ev. TempData-meddelanden
            return RedirectToPage();
        }

        private static bool IsSystemAdminEmail(string? email) =>
            string.Equals(email ?? "", "admin@sarasblogg.se", StringComparison.OrdinalIgnoreCase);
    }
}
