#nullable enable
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DAL;
using SarasBlogg.DTOs;
using SarasBlogg.Areas.Identity.Pages.Account.Manage;

namespace SarasBlogg.Areas.Identity.Pages.Account
{
    // [Authorize(Policy = "SkaVaraSuperAdmin")] // behåll om du endast vill låta superadmin skapa konton
    public class RegisterModel : PageModel
    {
        private readonly UserAPIManager _userApi;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(UserAPIManager userApi, ILogger<RegisterModel> logger)
        {
            _userApi = userApi;
            _logger = logger;
        }

        [BindProperty] public InputModel Input { get; set; } = new();

        public string? DevConfirmLink { get; set; } // visas om API:t skickar länk i dev
        public bool IsManageContext { get; private set; }

        public class InputModel
        {
            // OBLIGATORISKT (matchar API:t)
            [Required(ErrorMessage = "Användarnamn är obligatoriskt.")]
            [Display(Name = "Användarnamn *")]
            public string UserName { get; set; } = "";

            [Required(ErrorMessage = "E-post är obligatoriskt.")]
            [EmailAddress(ErrorMessage = "Ogiltig e-postadress.")]
            [Display(Name = "E-post *")]
            public string Email { get; set; } = "";

            [Required(ErrorMessage = "Lösenord är obligatoriskt.")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "{0} måste vara minst {2} tecken.")]
            [DataType(DataType.Password)]
            [Display(Name = "Lösenord *")]
            public string Password { get; set; } = "";

            [DataType(DataType.Password)]
            [Display(Name = "Bekräfta lösenord *")]
            [Compare(nameof(Password), ErrorMessage = "Lösenorden matchar inte.")]
            public string ConfirmPassword { get; set; } = "";

            // FRIVILLIGT (sparas senare via profil)
            [Display(Name = "Namn")]
            public string? Name { get; set; }

            [Display(Name = "Födelseår")]
            public int? BirthYear { get; set; }

            [Display(Name = "Mejla mig när nya blogginlägg publiceras")]
            public bool SubscribeNewPosts { get; set; }

        }

        public void OnGet()
        {
            IsManageContext = Request?.Query.ContainsKey("manage") == true;
            if (IsManageContext)
            {
                ViewData["ActivePage"] = ManageNavPages.Register;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            IsManageContext = Request?.Query.ContainsKey("manage") == true;
            if (IsManageContext)
            {
                ViewData["ActivePage"] = ManageNavPages.Register;
            }

            if (!ModelState.IsValid) return Page();

            var result = await _userApi.RegisterAsync(
                Input.UserName, Input.Email, Input.Password, Input.Name, Input.BirthYear,
                subscribeNewPosts: Input.SubscribeNewPosts);

            if (result is null)
            {
                ModelState.AddModelError(string.Empty, "Kunde inte nå API:t. Försök igen.");
                return Page();
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Registreringen misslyckades.");
                return Page();
            }

            // Dev-läge: API returnerar ConfirmEmailUrl om ExposeConfirmLinkInResponse = true
            if (!string.IsNullOrEmpty(result.ConfirmEmailUrl))
            {
                DevConfirmLink = result.ConfirmEmailUrl;
                TempData["RegisterInfo"] = "Kontot skapades. Klicka på länken nedan för att bekräfta e-post.";
                return Page();
            }

            // Prod-läge: visa info och skicka till bekräftelsesida
            TempData["RegisterInfo"] = "Kontot skapades. Kolla din e-post för att bekräfta.";
            return RedirectToPage("./RegisterConfirmation");
        }
    }
}
