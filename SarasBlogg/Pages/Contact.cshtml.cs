using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.Data;
using SarasBlogg.Models;

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

        public async Task<IActionResult> OnPostAsync(Models.ContactMe contactMe, int deleteId)
        {
            if (ModelState.IsValid)
            {
                _context.ContactMe.Add(contactMe);
                await _context.SaveChangesAsync();
                TempData["addMessage"] = "Tack för ditt meddelande!";
                return RedirectToPage("./Contact", new { contactId = "1" });
            }
            if (deleteId != 0)
            {
                var contactToDelete = await _context.ContactMe.FindAsync(deleteId);
                if (contactToDelete != null)
                {
                    _context.ContactMe.Remove(contactToDelete);
                    await _context.SaveChangesAsync();
                    TempData["deleteMessage"] = "Meddelandet raderades.";
                }

                return RedirectToPage("./Contact", new { contactId = "1" });
            }
            return Page();
        }
    }
}
