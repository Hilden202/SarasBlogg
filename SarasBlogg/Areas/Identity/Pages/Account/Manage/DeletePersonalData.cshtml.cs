#nullable disable
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SarasBlogg.DAL;
using SarasBlogg.Services;
using System.Security.Claims;

namespace SarasBlogg.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class DeletePersonalDataModel : PageModel
    {
        private readonly UserAPIManager _userApi;
        private readonly IAccessTokenStore _tokenStore;

        public DeletePersonalDataModel(UserAPIManager userApi, IAccessTokenStore tokenStore)
        {
            _userApi = userApi;
            _tokenStore = tokenStore;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [DataType(DataType.Password)]
            [Display(Name = "Lösenord")]
            public string Password { get; set; }
        }

        public bool RequirePassword { get; set; }

        public IActionResult OnGet()
        {
            RequirePassword = true;
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            var res = await _userApi.DeleteMeAsync(Input?.Password);
            if (res?.Succeeded != true)
            {
                RequirePassword = true;
                ModelState.AddModelError(string.Empty, res?.Message ?? "Kunde inte radera kontot.");
                return Page();
            }

            // 1) Sign-out our cookie scheme (same one you set up in Program.cs)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 2) Clear the current principal for *this* request so header text disappears immediately
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // 3) Aggressively expire both cookies (must match name + path)
            var expired = new CookieOptions { Path = "/", Expires = DateTimeOffset.UtcNow.AddDays(-1) };
            Response.Cookies.Append("SarasAuth", "", expired);
            Response.Cookies.Append("api_access_token", "", expired);
            Response.Cookies.Delete("SarasAuth", new CookieOptions { Path = "/" });
            Response.Cookies.Delete("api_access_token", new CookieOptions { Path = "/" });

            // (If you keep a token in memory)
            _tokenStore.Clear();

            TempData["StatusMessage"] = "Ditt konto är raderat och du har loggats ut.";

            // 4) Return a SignOutResult that also redirects home (extra safety)
            return SignOut(new AuthenticationProperties
            {
                RedirectUri = Url.Page("/Index", new { area = "" })
            }, CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
