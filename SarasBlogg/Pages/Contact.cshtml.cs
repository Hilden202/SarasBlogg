using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.DAL;

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
                // ?? Garantera att CreatedAt skickas som UTC
                if (contactMe.CreatedAt != default)
                {
                    contactMe.CreatedAt = DateTime.SpecifyKind(contactMe.CreatedAt, DateTimeKind.Utc);
                }

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
