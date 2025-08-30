// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DAL;
using SarasBlogg.Services;

namespace SarasBlogg.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly UserAPIManager _userApi;
        private readonly ILogger<LoginModel> _logger;
        private readonly IAccessTokenStore _tokenStore;

        public LoginModel(UserAPIManager userApi, ILogger<LoginModel> logger, IAccessTokenStore tokenStore)
        {
            _userApi = userApi;
            _logger = logger;
            _tokenStore = tokenStore;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Användarnamn eller e-post är obligatoriskt.")]
            [Display(Name = "E-post eller användarnamn")]
            public string UserNameOrEmail { get; set; } = "";

            [Required(ErrorMessage = "Lösenord är obligatoriskt.")]
            [DataType(DataType.Password)]
            [Display(Name = "Lösenord")]
            public string Password { get; set; } = "";

            [Display(Name = "Kom ihåg mig?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                ModelState.AddModelError(string.Empty, ErrorMessage);

            ReturnUrl ??= returnUrl ??= Url.Content("~/");

            // Ingen lokal Identity längre – inget att göra här
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (!ModelState.IsValid) return Page();

            // 1) Logga in via API
            var login = await _userApi.LoginAsync(Input.UserNameOrEmail, Input.Password, Input.RememberMe);
            if (login is null)
            {
                ModelState.AddModelError(string.Empty, "Ogiltigt inloggningsförsök.");
                return Page();
            }

            // 2) Läs JWT-claims
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(login.AccessToken);

            // 3) Skapa cookie i vårt generiska cookie-scheme
            var identity = new ClaimsIdentity(jwt.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var props = new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe,
                ExpiresUtc = login.AccessTokenExpiresUtc
            };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
            _tokenStore.Set(login.AccessToken);

            // 4) 
            Response.Cookies.Append("api_access_token", login.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None, // viktigt vid eventuella cross-site flöden
                Path = "/",               // KRITISKT: gör cookien giltig för hela sajten
                Expires = login.AccessTokenExpiresUtc
            });


            _logger.LogInformation("User logged in via API.");
            return LocalRedirect(returnUrl ?? Url.Content("~/"));
        }
    }
}
