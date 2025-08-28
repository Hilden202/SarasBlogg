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
            // Sätt ALDRIG Authorization om användaren inte är inloggad i webb-appen
            var isAuth = _http.HttpContext?.User?.Identity?.IsAuthenticated == true;

            if (isAuth && request.Headers.Authorization is null)
            {
                // Läs token per-request (store först, annars HttpOnly-cookie)
                var cookieToken = _http.HttpContext?.Request?.Cookies["api_access_token"];
                var token = _store.AccessToken ?? cookieToken;

                _logger.LogInformation(
                    "JwtAuthHandler: isAuth={IsAuth}, hasStore={HasStore}, hasCookie={HasCookie}, req={Method} {Url}",
                    isAuth, !string.IsNullOrWhiteSpace(_store.AccessToken), !string.IsNullOrWhiteSpace(cookieToken),
                    request.Method, request.RequestUri
                );

                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    _logger.LogInformation("JwtAuthHandler: Authorization header attached.");
                }
                else
                {
                    _logger.LogWarning("JwtAuthHandler: No token found (store & cookie empty). Request will be anonymous.");
                }
            }
            else
            {
                _logger.LogInformation(
                    "JwtAuthHandler: Skipped attaching Authorization. isAuth={IsAuth}, alreadyHasAuthHeader={HasAuth}, req={Method} {Url}",
                    isAuth, request.Headers.Authorization != null, request.Method, request.RequestUri
                );
            }

            return base.SendAsync(request, ct);
        }
    }
}
