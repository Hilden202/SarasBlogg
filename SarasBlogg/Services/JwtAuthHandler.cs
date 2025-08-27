using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace SarasBlogg.Services
{
    public class JwtAuthHandler : DelegatingHandler
    {
        private readonly IAccessTokenStore _store;
        private readonly IHttpContextAccessor _http;

        public JwtAuthHandler(IAccessTokenStore store, IHttpContextAccessor http) // ← ändrat
        {
            _store = store;
            _http = http;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            // 1) Om Authorization redan satt (t.ex. manuellt) – låt den vara
            if (request.Headers.Authorization is null)
            {
                // 2) Försök först med token i minnet
                var token = _store.AccessToken;

                // 3) Om minnet saknar token – hämta från HttpOnly-cookie (om finns)
                if (string.IsNullOrWhiteSpace(token))
                {
                    token = _http.HttpContext?.Request?.Cookies["api_access_token"];
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        // synca tillbaka till minnet för kommande requests
                        _store.Set(token);
                    }
                }

                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }

            return base.SendAsync(request, ct);
        }
    }
}
