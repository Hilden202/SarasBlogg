using System.Diagnostics;

namespace SarasBlogg.Services
{
    public sealed class HttpClientLoggingHandler : DelegatingHandler
    {
        private readonly ILogger<HttpClientLoggingHandler> _logger;
        public HttpClientLoggingHandler(ILogger<HttpClientLoggingHandler> logger) => _logger = logger;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var resp = await base.SendAsync(request, ct);
                sw.Stop();

                // Logga status + tid
                _logger.LogInformation("HTTP {Method} {Url} -> {Status} in {Ms} ms",
                    request.Method, request.RequestUri, (int)resp.StatusCode, sw.ElapsedMilliseconds);

                // 🔹 Lägg till detta: logga Retry-After vid 429
                if ((int)resp.StatusCode == 429)
                {
                    var ra = resp.Headers?.RetryAfter;
                    var wait = ra?.Delta?.ToString() ?? ra?.Date?.ToString() ?? "n/a";
                    _logger.LogWarning("HTTP 429 received. Retry-After: {RetryAfter}", wait);
                }

                return resp;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogWarning(ex, "HTTP {Method} {Url} failed after {Ms} ms",
                    request.Method, request.RequestUri, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
