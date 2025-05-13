using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.Data;

namespace SarasBlogg.Pages
{
    public class ContactModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public ContactModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Models.ContactMe ContactMe { get; set; }
        public IList<Models.ContactMe> ContactMes { get; set; }
        public void OnGet()
        {
            ContactMes = _context.ContactMe.ToList();
        }

        public async Task<IActionResult> OnPostAsync(Models.ContactMe contactMe)
        {
            if (ModelState.IsValid)
            {
                _context.ContactMe.Add(contactMe);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Tack för ditt meddelande!";
                return RedirectToPage();
            }
            return Page();
        }
    }
}
