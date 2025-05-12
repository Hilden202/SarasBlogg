using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.Data;

namespace SarasBlogg.Pages
{
    public class PrivacyModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PrivacyModel(ApplicationDbContext context)
        {
            _context = context;
        }
        [BindProperty]
        public Models.AboutMe AboutMe { get; set; }

        [BindProperty]
        public IFormFile AboutMeImage { get; set; }
        public async Task OnGetAsync()
        {
            AboutMe = await _context.AboutMe.FirstOrDefaultAsync();
        }
    }

}
