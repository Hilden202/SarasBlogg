// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SarasBlogg.Data;
using SarasBlogg.DAL;
using SarasBlogg.DTOs;
using Humanizer;

namespace SarasBlogg.Areas.Identity.Pages.Account
{
    public class ConfirmEmailChangeModel : PageModel
    {
        private readonly UserAPIManager _userApi;

        public ConfirmEmailChangeModel(UserAPIManager userApi) => _userApi = userApi;

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string email, string code)
        {
            if (userId == null || email == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            // 👉 API-anrop: bekräfta e-postbyte
            var dto = new ChangeEmailConfirmDto { UserId = userId, Code = code };
            var res = await _userApi.ChangeEmailConfirmAsync(userId, code, email);
            if (res?.Succeeded == true)
            {
                StatusMessage = "Tack för att du bekräftade ändringen av din e-postadress.";
                return Page();
            }

            StatusMessage = res?.Message ?? "Fel vid ändring av e-postadress.";
            return Page();
        }
    }
}
