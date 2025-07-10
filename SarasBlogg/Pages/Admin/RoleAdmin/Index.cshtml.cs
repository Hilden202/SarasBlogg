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

        [BindProperty(SupportsGet = true)]
        public string RoleName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string AddUserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string RemoveUserId { get; set; }

        [BindProperty]
        public string DeleteUserId { get; set; }

        [BindProperty]
        public string DeleteRoleName { get; set; }

        private readonly UserAPIManager _userApiManager;
        public IndexModel(UserAPIManager userApiManager)
        {
            _userApiManager = userApiManager;
        }

        public async Task OnGetAsync()
        {
            if (!string.IsNullOrEmpty(AddUserId))
                await _userApiManager.AddUserToRoleAsync(AddUserId, RoleName);

            if (!string.IsNullOrEmpty(RemoveUserId))
                await _userApiManager.RemoveUserFromRoleAsync(RemoveUserId, RoleName);

            var customOrder = new[] { "user", "superuser", "admin", "superadmin" };
            Roles = (await _userApiManager.GetAllRolesAsync())
                .OrderBy(r => Array.IndexOf(customOrder, r.ToLower()))
                .ThenBy(r => r)
                .ToList();
            Users = (await _userApiManager.GetAllUsersAsync())
                .OrderByDescending(u => u.Email.ToLower() == "admin@sarasblogg.se")
                .ThenBy(u => u.Name)
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

            if (user != null && user.Email.ToLower() != "admin@sarasblogg.se")
            {
                await _userApiManager.DeleteUserAsync(DeleteUserId);
            }

            return RedirectToPage();
        }


        public async Task<IActionResult> OnPostDeleteRoleAsync()
        {
            if (!string.IsNullOrWhiteSpace(DeleteRoleName) && DeleteRoleName.ToLower() != "superadmin")
            {
                await _userApiManager.DeleteRoleAsync(DeleteRoleName);
            }

            return RedirectToPage();
        }

    }
}
