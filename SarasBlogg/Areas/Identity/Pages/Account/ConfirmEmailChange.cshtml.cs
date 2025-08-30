#nullable disable

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DAL;

namespace SarasBlogg.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ConfirmEmailChangeModel : PageModel
    {
        private readonly UserAPIManager _userApi;

        public ConfirmEmailChangeModel(UserAPIManager userApi) => _userApi = userApi;

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string email, string code)
        {
            if (userId == null || email == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var res = await _userApi.ChangeEmailConfirmAsync(userId, code, email);
            if (res?.Succeeded == true)
            {
                StatusMessage = "Tack för att du bekräftade ändringen av din e-postadress.";
                return Page();
            }

            StatusMessage = res?.Message ?? "Fel vid ändring av e-postadress.";
            return Page();
        }
    }
}
