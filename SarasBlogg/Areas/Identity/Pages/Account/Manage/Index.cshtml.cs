using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DAL;

namespace SarasBlogg.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserAPIManager _userApi;

        public IndexModel(UserAPIManager userApi)
        {
            _userApi = userApi;
        }

        public string Username { get; set; } = "";

        [TempData]
        public string StatusMessage { get; set; } = "";

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Phone]
            [Display(Name = "Telefonnummer")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "Namn")]
            public string? Name { get; set; }

            [Display(Name = "Födelseår")]
            public int? BirthYear { get; set; }

            [Display(Name = "Mejla mig vid nya blogginlägg")]
            public bool NotifyOnNewPost { get; set; }
        }



        private async Task LoadAsync()
        {
            var me = await _userApi.GetMeAsync();   // <- HÄMTA FRÅN API
            Username = me?.UserName ?? User.Identity?.Name ?? "";

            Input = new InputModel
            {
                PhoneNumber = me?.PhoneNumber,
                Name = me?.Name,
                BirthYear = me?.BirthYear,
                NotifyOnNewPost = me?.NotifyOnNewPost ?? false
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var res = await _userApi.UpdateMyProfileAsync(
                Input.PhoneNumber, Input.Name, Input.BirthYear, notifyOnNewPost: Input.NotifyOnNewPost);

            StatusMessage = res?.Message ?? (res?.Succeeded == true
                ? "Din profil har uppdaterats."
                : "Kunde inte uppdatera profilen.");

            // Ladda om från API så sidan visar nya värden
            return RedirectToPage();
        }
    }
}
