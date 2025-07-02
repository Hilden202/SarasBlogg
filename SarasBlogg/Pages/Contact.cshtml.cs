using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.DAL;
using SarasBlogg.Data;
using SarasBlogg.Models;

namespace SarasBlogg.Pages
{
    public class ContactModel : PageModel
    {
        private readonly ContactMeAPIManager _contactManager;
        public ContactModel(ContactMeAPIManager contactManager)
        {
            _contactManager = contactManager;
        }


        [BindProperty]
        public Models.ContactMe ContactMe { get; set; }
        public IList<Models.ContactMe> ContactMes { get; set; }
        public async Task OnGetAsync()
        {
            ContactMes = await _contactManager.GetAllMessagesAsync();
        }

        public async Task<IActionResult> OnPostAsync(Models.ContactMe contactMe, int deleteId)
        {
            if (ModelState.IsValid)
            {
                await _contactManager.SaveMessageAsync(contactMe);
                TempData["addMessage"] = "Tack för ditt meddelande!";
                return RedirectToPage("./Contact", new { contactId = "1" });
            }
            if (deleteId != 0)
            {
                await _contactManager.DeleteMessageAsync(deleteId);
                TempData["deleteMessage"] = "Meddelandet raderades.";
                return RedirectToPage("./Contact", new { contactId = "1" });
            }
            return Page();
        }
    }
}
