// Areas/Identity/Pages/Account/RegisterConfirmation.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SarasBlogg.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        public string Email { get; set; } = "";
        public bool DisplayConfirmAccountLink { get; set; }
        public string? EmailConfirmationUrl { get; set; }

        public IActionResult OnGet(string email, string? confirmUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToPage("/Index");

            Email = email;

            if (!string.IsNullOrWhiteSpace(confirmUrl))
            {
                DisplayConfirmAccountLink = true;
                EmailConfirmationUrl = confirmUrl;
            }
            else
            {
                DisplayConfirmAccountLink = false;
            }

            return Page();
        }
    }
}
