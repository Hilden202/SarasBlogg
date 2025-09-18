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

        [BindProperty] public string? Website { get; set; } // honeypot
        [BindProperty] public string? FormIssuedAt { get; set; }
        [BindProperty] public string? FormToken { get; set; }

        private string Sign(string data)
        {
            var secret = _config["AntiSpam:Secret"] ?? "fallback-secret";
            using var h = new System.Security.Cryptography.HMACSHA256(
                System.Text.Encoding.UTF8.GetBytes(secret));
            var hash = h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash);
        }
        private bool Verify(string? data, string? sig)
            => !string.IsNullOrEmpty(data) && !string.IsNullOrEmpty(sig) && Sign(data!) == sig;

        public async Task OnGetAsync()
        {
            // Hämta alla meddelanden (vy ansvarar för ToSwedishTime vid render)
            ContactMes = await _contactManager.GetAllMessagesAsync();

            var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            FormIssuedAt = issuedAt;
            FormToken = Sign(issuedAt);
        }

        public async Task<IActionResult> OnPostAsync(Models.ContactMe contactMe, int deleteId)
        {
            // 1) RADERA — prio först, och endast superadmin
            if (deleteId != 0)
            {
                if (!User.IsInRole("superadmin"))
                    return Forbid(); // extra säkerhet

                await _contactManager.DeleteMessageAsync(deleteId);
                TempData["deleteMessage"] = "Meddelandet raderades.";
                return RedirectToPage("./Contact", new { contactId = "1" });
            }

            // 1.5)--- Anti-spam ---
            if (!string.IsNullOrWhiteSpace(Website))
            {
                TempData["addMessage"] = "Tack för ditt meddelande!";
                return RedirectToPage("./Contact");
            }
            if (!Verify(FormIssuedAt, FormToken))
            {
                TempData["addMessage"] = "Tack för ditt meddelande!";
                return RedirectToPage("./Contact");
            }
            if (long.TryParse(FormIssuedAt, out var ts))
            {
                var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(ts);
                if (age.TotalSeconds < 5)
                {
                    TempData["addMessage"] = "Tack för ditt meddelande!";
                    return RedirectToPage("./Contact");
                }
            }
            // blocka länkar/domäner
            string msg = contactMe?.Message ?? "";
            string subj = contactMe?.Subject ?? "";
            string email = contactMe?.Email ?? "";
            string[] blocked = { "searchregister.org", "searchindexer.pro" };
            if (blocked.Any(b => msg.Contains(b, StringComparison.OrdinalIgnoreCase) ||
                                 subj.Contains(b, StringComparison.OrdinalIgnoreCase) ||
                                 email.Contains(b, StringComparison.OrdinalIgnoreCase)) ||
                System.Text.RegularExpressions.Regex.IsMatch(msg, @"https?://|www\.", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                TempData["addMessage"] = "Tack för ditt meddelande!";
                return RedirectToPage("./Contact");
            }
            // --- /Anti-spam ---

            // 2) SKICKA — samma logik som du hade
            if (ModelState.IsValid)
            {
                if (contactMe.CreatedAt == default)
                {
                    contactMe.CreatedAt = DateTime.UtcNow;
                }
                else if (contactMe.CreatedAt.Kind == DateTimeKind.Unspecified)
                {
                    var seLocal = DateTime.SpecifyKind(contactMe.CreatedAt, DateTimeKind.Unspecified);
                    contactMe.CreatedAt = TimeZoneInfo.ConvertTimeToUtc(seLocal, TzSe);
                }
                else if (contactMe.CreatedAt.Kind == DateTimeKind.Local)
                {
                    contactMe.CreatedAt = contactMe.CreatedAt.ToUniversalTime();
                }

                await _contactManager.SaveMessageAsync(contactMe);
                _ = SendToFormspreeAsync(contactMe);

                TempData["addMessage"] = "Tack för ditt meddelande!";
                return RedirectToPage("./Contact", new { contactId = "1" });
            }

            // 3) Ogiltigt formulär: visa sidan med valideringsfel
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync(int deleteId)
        {
            if (!User.IsInRole("superadmin")) return Forbid();

            await _contactManager.DeleteMessageAsync(deleteId);
            TempData["deleteMessage"] = "Meddelandet raderades.";
            return RedirectToPage("./Contact", new { contactId = "1" });
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
