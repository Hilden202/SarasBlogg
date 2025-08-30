#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SarasBlogg.DAL;
using SarasBlogg.DTOs;

namespace SarasBlogg.Areas.Identity.Pages.Account.Manage
{
    public class ChangePasswordModel : PageModel
    {
        private readonly ILogger<ChangePasswordModel> _logger;
        private readonly UserAPIManager _userApi;

        public ChangePasswordModel(
             UserAPIManager userApi,
             ILogger<ChangePasswordModel> logger)
        {
            _userApi = userApi;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Nuvarande lösenord")]
            public string CurrentPassword { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "{0} måste vara minst {2} och max {1} tecken långt.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Nytt lösenord")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Bekräfta nytt lösenord")]
            [Compare("NewPassword", ErrorMessage = "Det nya lösenordet och bekräftelsen matchar inte.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // (Ev. redirect till SetPassword hanteras i separat steg när SetPassword är API:at.)
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Input.NewPassword != Input.ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Det nya lösenordet och bekräftelsen matchar inte.");
                return Page();
            }

            var result = await _userApi.ChangePasswordAsync(Input.CurrentPassword, Input.NewPassword);
            if (result?.Succeeded == true)
            {
                _logger.LogInformation("Användaren har bytt lösenord via API.");
                StatusMessage = "Ditt lösenord har ändrats.";
                return RedirectToPage();
            }

            ModelState.AddModelError(string.Empty, result?.Message ?? "Något gick fel.");
            return Page();
        }
    }
}
