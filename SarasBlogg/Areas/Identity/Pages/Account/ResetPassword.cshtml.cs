using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DAL;
using SarasBlogg.DTOs;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace SarasBlogg.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResetPasswordModel : PageModel
    {
        private readonly UserAPIManager _userApi;

        public ResetPasswordModel(UserAPIManager userApi)
        {
            _userApi = userApi;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
            public string ConfirmPassword { get; set; } = "";
            public string Code { get; set; } = "";
            public string UserId { get; set; } = "";
        }

        public IActionResult OnGet(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return BadRequest("UserId och token måste finnas i länken.");

            Input = new InputModel
            {
                UserId = userId,
                Code = token // token är redan Base64-url encoded från API
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            if (Input.Password != Input.ConfirmPassword)
            {
                ModelState.AddModelError("", "Lösenorden matchar inte.");
                return Page();
            }

            var result = await _userApi.ResetPasswordAsync(Input.UserId, Input.Code, Input.Password);

            if (result?.Succeeded == true)
                return RedirectToPage("./ResetPasswordConfirmation");

            ModelState.AddModelError("", result?.Message ?? "Något gick fel.");
            return Page();
        }
    }
}
