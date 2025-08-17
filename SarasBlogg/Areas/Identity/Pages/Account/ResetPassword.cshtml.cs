using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SarasBlogg.Areas.Identity.Pages.Account
{
    // Renamed the class to avoid conflict with an existing definition
    [AllowAnonymous]
    public class ResetPasswordConfirmationPageModel : PageModel
    {
        public void OnGet() { }
    }
}
