using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.DAL;

namespace SarasBlogg.Pages
{
    public class ContactModel : PageModel
    {
        private readonly ContactMeAPIManager _contactManager;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _config;

        public ContactModel(
            ContactMeAPIManager contactManager,
            IHttpClientFactory httpFactory,
            IConfiguration config)
        {
            _contactManager = contactManager;
            _httpFactory = httpFactory;
            _config = config;
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
                if (contactMe.CreatedAt != default)
                    contactMe.CreatedAt = DateTime.SpecifyKind(contactMe.CreatedAt, DateTimeKind.Utc);

                // 1) Spara som vanligt via ditt API
                await _contactManager.SaveMessageAsync(contactMe);

                // 2) Skicka vidare till Formspree (så Sara får mail direkt)
                _ = SendToFormspreeAsync(contactMe); // fire-and-forget

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

        private async Task<bool> SendToFormspreeAsync(Models.ContactMe m)
        {
            var endpoint = _config["Formspree:Endpoint"];
            if (string.IsNullOrWhiteSpace(endpoint)) return false;

            try
            {
                var client = _httpFactory.CreateClient("formspree");
                var payload = new Dictionary<string, string>
                {
                    ["name"] = m.Name ?? "",
                    ["email"] = m.Email ?? "",
                    ["message"] = m.Message ?? "",
                    ["subject"] = m.Subject ?? "",
                    ["_replyto"] = m.Email ?? "",
                    ["_subject"] = $"[SarasBlogg] {m.Subject}",
                    ["_language"] = "sv"
                };

                using var content = new FormUrlEncodedContent(payload);
                var resp = await client.PostAsync(endpoint, content);
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false; // valfritt: logga med ILogger
            }
        }
    }
}