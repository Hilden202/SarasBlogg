using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SarasBlogg.Services
{
    public class JwtAuthHandler : DelegatingHandler
    {
        private readonly IAccessTokenStore _store;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<JwtAuthHandler> _logger;

        public JwtAuthHandler(IAccessTokenStore store, IHttpContextAccessor http, ILogger<JwtAuthHandler> logger)
        {
            _store = store;
            _http = http;
            _logger = logger;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            // Skicka bara Authorization om användaren är inloggad i webb-appen
            var isAuth = _http.HttpContext?.User?.Identity?.IsAuthenticated == true;

            if (isAuth && request.Headers.Authorization is null)
            {
                // 1) Försök med minnet (sätts vid login i samma request)
                var token = _store.AccessToken;

                // 2) Fallback: läs HttpOnly-kakan (per-user, överlev. sidladdningar)
                if (string.IsNullOrWhiteSpace(token))
                {
                    token = _http.HttpContext?.Request?.Cookies["api_access_token"];
                }

                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    _logger.LogDebug("JwtAuthHandler: Authorization header attached for {Method} {Url}", request.Method, request.RequestUri);
                }
                else
                {
                    _logger.LogDebug("JwtAuthHandler: No token found (store & cookie empty). Request goes anonymous: {Method} {Url}", request.Method, request.RequestUri);
                }
            }
            else
            {
                _logger.LogDebug("JwtAuthHandler: Skipped attaching Authorization. isAuth={IsAuth}, hasAuthHeader={HasAuth}",
                    isAuth, request.Headers.Authorization != null);
            }

            return base.SendAsync(request, ct);
        }
    }
}
