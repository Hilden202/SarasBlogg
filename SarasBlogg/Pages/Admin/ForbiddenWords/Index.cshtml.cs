using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SarasBlogg.DAL;
using SarasBlogg.Extensions;
using SarasBlogg.Models;

namespace SarasBlogg.Pages.Admin.ForbiddenWords
{
    [Authorize(Roles = "superuser, admin, superadmin")]
    public class IndexModel : PageModel
    {
        private readonly ForbiddenWordAPIManager _forbiddenWordApi;

        public IndexModel(ForbiddenWordAPIManager forbiddenWordApi)
        {
            _forbiddenWordApi = forbiddenWordApi;
        }

        public List<ForbiddenWord> ForbiddenWords { get; set; } = new();

        [BindProperty]
        public string NewWord { get; set; }

        public async Task OnGetAsync()
        {
            ForbiddenWords = await _forbiddenWordApi.GetAllAsync();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (!string.IsNullOrWhiteSpace(NewWord))
            {
                var word = new ForbiddenWord
                {
                    WordPattern = NewWord.ToRegexPattern() // använder extensionmetoden
                };

                await _forbiddenWordApi.SaveAsync(word);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _forbiddenWordApi.DeleteAsync(id);
            return RedirectToPage();
        }
    }
}
