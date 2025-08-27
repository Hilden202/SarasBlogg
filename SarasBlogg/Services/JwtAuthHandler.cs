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
            // Sätt ALDRIG Authorization om användaren inte är inloggad i webb-appen
            var isAuth = _http.HttpContext?.User?.Identity?.IsAuthenticated == true;

            if (isAuth && request.Headers.Authorization is null)
            {
                // Läs token per-request (store först, annars HttpOnly-cookie)
                var token = _store.AccessToken ?? _http.HttpContext?.Request?.Cookies["api_access_token"];
                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }

            return base.SendAsync(request, ct);
        }

    }
}
