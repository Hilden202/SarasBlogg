// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SarasBlogg.Data;
using SarasBlogg.DAL;
using SarasBlogg.DTOs;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace SarasBlogg.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserAPIManager _userApi;

        public EmailModel(UserManager<ApplicationUser> userManager, UserAPIManager userApi)
        {
            _userManager = userManager;   // behålls för att läsa nuvarande email/status
            _userApi = userApi;           // används för att starta/resa e-postflöden
        }

        public string Email { get; set; }
        public bool IsEmailConfirmed { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        // Håller den nya e-posten över redirect efter ChangeEmailStart
        [TempData]
        public string PendingNewEmail { get; set; }

        // Dev: klickbar bekräftelselänk från API (visas bara i Development)
        [TempData]
        public string DevConfirmUrl { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Ny e-postadress")]
            public string NewEmail { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var email = await _userManager.GetEmailAsync(user);
            Email = email;

            Input = new InputModel
            {
                NewEmail = email,
            };

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kunde inte ladda användare med ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);

            // Om vi precis initierat ett byte: visa den nya adressen i fältet igen
            if (!string.IsNullOrEmpty(PendingNewEmail) &&
            !string.Equals(PendingNewEmail, Email, StringComparison.OrdinalIgnoreCase))
            {
                Input.NewEmail = PendingNewEmail;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kunde inte ladda användare med ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var current = await _userManager.GetEmailAsync(user);
            if (string.Equals(Input.NewEmail, current, StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "Din e-postadress är oförändrad.";
                return RedirectToPage();
            }

            // 👉 API-anrop: starta byte av e-post (skickar mejl, och i dev exponerar ConfirmEmailUrl)
            var result = await _userApi.ChangeEmailStartAsync(Input.NewEmail);
            if (result?.Succeeded == true)
            {
                StatusMessage = "En bekräftelselänk för att ändra din e-postadress har skickats. Kontrollera din inkorg.";
                // Spara dev-länken separat så vi kan visa den snyggt i vyn
                DevConfirmUrl = string.IsNullOrWhiteSpace(result.ConfirmEmailUrl) ? "" : result.ConfirmEmailUrl;
                PendingNewEmail = Input.NewEmail; // <- behåll ny e-post över redirect
                return RedirectToPage();
            }

            ModelState.AddModelError(string.Empty, result?.Message ?? "Kunde inte initiera e-postbyte.");
            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Kunde inte ladda användare med ID '{_userManager.GetUserId(User)}'.");
            }

            // 👉 API-anrop: skicka om bekräftelse (neutral response avsiktligt)
            var current = await _userManager.GetEmailAsync(user);
            var candidate = string.IsNullOrWhiteSpace(PendingNewEmail) ? Input?.NewEmail : PendingNewEmail;
            var email = !string.IsNullOrWhiteSpace(candidate) &&
            !string.Equals(candidate, current, StringComparison.OrdinalIgnoreCase)
                        ? candidate
                        : current;
            await _userApi.ResendConfirmationAsync(email);
            StatusMessage = "Om adressen finns skickades en bekräftelselänk.";
            PendingNewEmail = Input?.NewEmail ?? PendingNewEmail; // behåll värdet i fältet
            return RedirectToPage();
        }
    }
}
