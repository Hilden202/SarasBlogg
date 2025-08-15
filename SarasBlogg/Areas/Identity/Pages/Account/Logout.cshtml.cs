// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SarasBlogg.DAL;
using SarasBlogg.Data;

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
            // Logga ut i API:t
            await _userApi.LogoutAsync();

            // Ta även bort den lokala Identity-cookie som sattes vid login
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            _logger.LogInformation("Användare loggade ut via API.");

            if (returnUrl != null)
                return LocalRedirect(returnUrl);

            return RedirectToPage();
        }

    }
}
