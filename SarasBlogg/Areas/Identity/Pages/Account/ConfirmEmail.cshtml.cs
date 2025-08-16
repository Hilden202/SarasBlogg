#nullable enable
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DAL;

namespace SarasBlogg.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserAPIManager _userApi;
        public ConfirmEmailModel(UserAPIManager userApi) => _userApi = userApi;

        // 🔽 Inte TempData längre
        public string Message { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
                return RedirectToPage("/Index");

            var result = await _userApi.ConfirmEmailAsync(userId, code);

            Message = result?.Succeeded == true
                ? "Din e-post är bekräftad. Du kan nu logga in."
                : "Ett fel uppstod vid bekräftelsen av din e-postadress."
                  + (string.IsNullOrWhiteSpace(result?.Message) ? "" : $" ({result!.Message})");

            return Page();
        }
    }
}
