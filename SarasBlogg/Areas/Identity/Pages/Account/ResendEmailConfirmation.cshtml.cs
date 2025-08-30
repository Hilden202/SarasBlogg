#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DAL;
using SarasBlogg.DTOs;

namespace SarasBlogg.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserAPIManager _userApi;

        public ResendEmailConfirmationModel(UserAPIManager userApi) => _userApi = userApi;

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "E-postadress är obligatorisk.")]
            [EmailAddress(ErrorMessage = "Ogiltig e-postadress.")]
            [Display(Name = "E-postadress")]
            public string Email { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // 👉 API: neutral respons oavsett om användaren finns/är bekräftad
            var res = await _userApi.ResendConfirmationAsync(Input.Email);
            var msg = res?.Message ?? "Om adressen finns skickades en bekräftelselänk.";
            ModelState.AddModelError(string.Empty, msg);
            return Page();
        }
    }
}
