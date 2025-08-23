using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DAL; // där du har din UserAPIManager
using SarasBlogg.DTOs;
using System.Threading.Tasks;

namespace SarasBlogg.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserAPIManager _userApi;

        public ForgotPasswordModel(UserAPIManager userApi)
        {
            _userApi = userApi;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "E-postadress är obligatorisk.")]
            [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Ogiltig e-postadress.")]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var result = await _userApi.ForgotPasswordAsync(Input.Email);
            if (!string.IsNullOrWhiteSpace(result?.ConfirmEmailUrl))
            {
                TempData["DevResetLink"] = result!.ConfirmEmailUrl;
            }

            // alltid redirecta till en "CheckEmail" eller "ForgotPasswordConfirmation" 
            return RedirectToPage("./ForgotPasswordConfirmation");
        }
    }
}
