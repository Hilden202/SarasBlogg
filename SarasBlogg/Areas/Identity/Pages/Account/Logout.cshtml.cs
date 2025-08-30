// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SarasBlogg.DAL;
using SarasBlogg.Services;

namespace SarasBlogg.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly UserAPIManager _userApi;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(UserAPIManager userApi, ILogger<LogoutModel> logger)
        {
            _userApi = userApi;
            _logger = logger;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            // 1) Logga ut i API:t
            await _userApi.LogoutAsync();

            // 2) Logga ut från vårt cookie-schema (inte Identity)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 3) Rensa bearer-token från HttpClient-handlern (IAccessTokenStore)
            HttpContext.RequestServices.GetRequiredService<IAccessTokenStore>().Clear();

            // 4) Rensa även HttpOnly-cookie-kopia av token (med samma Path/Domain som sattes vid login)
            Response.Cookies.Delete("api_access_token", new CookieOptions
            {
                Path = "/",
                Secure = true,
                SameSite = SameSiteMode.None
            });

            // refresh-token:
            Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/", Secure = true, SameSite = SameSiteMode.None });
            
            _logger.LogInformation("Användare loggade ut via API.");

            return returnUrl is not null ? LocalRedirect(returnUrl) : Redirect("~/");
        }

    }
}
