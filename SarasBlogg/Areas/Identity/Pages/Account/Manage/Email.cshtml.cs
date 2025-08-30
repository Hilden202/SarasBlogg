#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DAL;

namespace SarasBlogg.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly UserAPIManager _userApi;
        public EmailModel(UserAPIManager userApi) => _userApi = userApi;

        public string Email { get; set; } = "";
        public bool ShowResendLink { get; set; }   // <— ny
        public bool IsEmailConfirmed { get; set; }

        [TempData] public string StatusMessage { get; set; } = "";
        [TempData] public string? PendingNewEmail { get; set; }
        [TempData] public string? DevConfirmUrl { get; set; }

        [BindProperty] public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required, EmailAddress]
            [Display(Name = "Ny e-postadress")]
            public string NewEmail { get; set; } = "";
        }

        private async Task LoadAsync()
        {
            var me = await _userApi.GetMeAsync();
            Email = me?.Email ?? "";
            IsEmailConfirmed = me?.EmailConfirmed ?? false;

            Input ??= new InputModel();
            Input.NewEmail = string.IsNullOrWhiteSpace(PendingNewEmail) ? Email : PendingNewEmail!;
            ShowResendLink = !string.IsNullOrWhiteSpace(PendingNewEmail) &&
                             !string.Equals(PendingNewEmail, Email, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadAsync();
                return Page();
            }

            var me = await _userApi.GetMeAsync();
            var current = me?.Email ?? "";
            if (string.Equals(Input.NewEmail, current, StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "Din e-postadress är oförändrad.";
                return RedirectToPage();
            }

            var result = await _userApi.ChangeEmailStartAsync(Input.NewEmail);
            if (result?.Succeeded == true)
            {
                StatusMessage = "En bekräftelselänk har skickats. Kontrollera din inkorg.";
                DevConfirmUrl = string.IsNullOrWhiteSpace(result.ConfirmEmailUrl) ? "" : result.ConfirmEmailUrl;
                PendingNewEmail = Input.NewEmail;     // <- triggar ShowResendLink efter redirect
                return RedirectToPage();
            }

            ModelState.AddModelError(string.Empty, result?.Message ?? "Kunde inte initiera e-postbyte.");
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var me = await _userApi.GetMeAsync();
            var current = me?.Email ?? "";
            var candidate = string.IsNullOrWhiteSpace(PendingNewEmail) ? Input?.NewEmail : PendingNewEmail;

            var emailToUse = !string.IsNullOrWhiteSpace(candidate) &&
                             !string.Equals(candidate, current, StringComparison.OrdinalIgnoreCase)
                               ? candidate!
                               : current;

            await _userApi.ResendConfirmationAsync(emailToUse);
            StatusMessage = "Om adressen finns skickades en bekräftelselänk.";
            // Behåll PendingNewEmail så länken fortsätter visas tills du bekräftat bytet.
            return RedirectToPage();
        }
    }
}
