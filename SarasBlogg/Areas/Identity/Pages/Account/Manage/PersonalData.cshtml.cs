using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using SarasBlogg.DAL;
using SarasBlogg.DTOs;

namespace SarasBlogg.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class PersonalDataModel : PageModel
    {
        private readonly UserAPIManager _userApi;
        public PersonalDataDto? Personal { get; private set; }

        public PersonalDataModel(UserAPIManager userApi) => _userApi = userApi;

        public async Task<IActionResult> OnGet()
        {
            Personal = await _userApi.GetMyPersonalDataAsync();
            return Page();
        }
    }
}
