#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using SarasBlogg.DAL;

namespace SarasBlogg.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class DownloadPersonalDataModel : PageModel
    {
        private readonly UserAPIManager _userApi;

        public DownloadPersonalDataModel(UserAPIManager userApi) => _userApi = userApi;

        public IActionResult OnGet() => NotFound(); // endast POST

        public async Task<IActionResult> OnPostAsync()
        {
            var (bytes, filename, contentType) = await _userApi.DownloadMyPersonalDataAsync();
            if (bytes is null) return BadRequest();
            return File(bytes, contentType, filename);
        }
    }
}
