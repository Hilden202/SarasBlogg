#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SarasBlogg.Data;
using Microsoft.AspNetCore.Authorization;
using SarasBlogg.DAL;
using SarasBlogg.DTOs;
using Microsoft.AspNetCore.Authentication;

namespace SarasBlogg.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class DeletePersonalDataModel : PageModel
    {
        private readonly UserAPIManager _userApi;

        public DeletePersonalDataModel(UserAPIManager userApi) => _userApi = userApi;

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [DataType(DataType.Password)]
            [Display(Name = "Lösenord")]
            public string Password { get; set; }
        }

        public bool RequirePassword { get; set; }

        public async Task<IActionResult> OnGet()
        {
            // Enkelt: visa lösenordsfältet alltid (API kräver det bara om kontot har lösenord)
            RequirePassword = true;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var res = await _userApi.DeleteMeAsync(Input?.Password);
            if (res?.Succeeded == true)
            {
                // 1) Logga ut från Identity-schemat (och ev. externa/tvåfaktor)
                await HttpContext.SignOutAsync(Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme);
                await HttpContext.SignOutAsync(Microsoft.AspNetCore.Identity.IdentityConstants.ExternalScheme);
                await HttpContext.SignOutAsync(Microsoft.AspNetCore.Identity.IdentityConstants.TwoFactorUserIdScheme);

                // 2) Ta bort standard-cookies (justera om du har egna namn)
                HttpContext.Response.Cookies.Delete(".AspNetCore.Identity.Application"); // huvudcookie
                HttpContext.Response.Cookies.Delete(".AspNetCore.ExternalCookie");
                HttpContext.Response.Cookies.Delete(".AspNetCore.TwoFactorUserId");
                // Om du även sätter API-JWT i cookies:
                HttpContext.Response.Cookies.Delete("access_token");
                HttpContext.Response.Cookies.Delete("refresh_token");

                TempData["StatusMessage"] = "Ditt konto är raderat och du har loggats ut.";
                return RedirectToPage("/Index");
            }

            RequirePassword = true;
            ModelState.AddModelError(string.Empty, res?.Message ?? "Kunde inte radera kontot.");
            return Page();
        }
    }
}
