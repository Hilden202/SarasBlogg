#nullable enable

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DAL;
using SarasBlogg.DTOs;

namespace SarasBlogg.Areas.Identity.Pages.Account.Manage
{
    public class SetPasswordModel : PageModel
    {
        private readonly UserAPIManager _userApi;
        public SetPasswordModel(UserAPIManager userApi) => _userApi = userApi;

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(100, ErrorMessage = "{0} måste vara minst {2} och högst {1} tecken långt.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Nytt lösenord")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Bekräfta nytt lösenord")]
            [Compare("NewPassword", ErrorMessage = "Det nya lösenordet och bekräftelsen matchar inte.")]
            public string ConfirmPassword { get; set; }
        }

        public IActionResult OnGet() => Page();

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

            var result = await _userApi.SetPasswordAsync(Input.NewPassword);
            if (result?.Succeeded == true)
            {
                StatusMessage = "Ditt lösenord har satts.";
                return RedirectToPage();
            }

            ModelState.AddModelError(string.Empty, result?.Message ?? "Något gick fel.");
            return Page();
        }
    }
}
