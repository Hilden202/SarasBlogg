using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.DAL;
using SarasBlogg.Extensions; // för visning i vyer (ToSwedishTime)

namespace SarasBlogg.Pages
{
    public class ContactModel : PageModel
    {
        private readonly ContactMeAPIManager _contactManager;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _config;

        private static readonly TimeZoneInfo TzSe = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

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
            // Hämta alla meddelanden (vy ansvarar för ToSwedishTime vid render)
            ContactMes = await _contactManager.GetAllMessagesAsync();
        }

        public async Task<IActionResult> OnPostAsync(Models.ContactMe contactMe, int deleteId)
        {
            if (ModelState.IsValid)
            {
                // Normalisera CreatedAt till UTC (exakt tid).
                // 1) Saknas tid -> sätt nu i UTC.
                if (contactMe.CreatedAt == default)
                {
                    contactMe.CreatedAt = DateTime.UtcNow;
                }
                else
                {
                    // 2) Om tidszonsinfo saknas: tolka som svensk lokal tid och konvertera till UTC.
                    if (contactMe.CreatedAt.Kind == DateTimeKind.Unspecified)
                    {
                        var seLocal = DateTime.SpecifyKind(contactMe.CreatedAt, DateTimeKind.Unspecified);
                        contactMe.CreatedAt = TimeZoneInfo.ConvertTimeToUtc(seLocal, TzSe);
                    }
                    // 3) Om Local: konvertera till UTC.
                    else if (contactMe.CreatedAt.Kind == DateTimeKind.Local)
                    {
                        contactMe.CreatedAt = contactMe.CreatedAt.ToUniversalTime();
                    }
                    // 4) Om redan Utc: använd som är.
                }

                // 1) Spara via API
                await _contactManager.SaveMessageAsync(contactMe);

                // 2) Fire-and-forget till Formspree (mailnotis)
                _ = SendToFormspreeAsync(contactMe);

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
