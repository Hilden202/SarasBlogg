using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SarasBlogg.Data;

namespace SarasBlogg.Services
{
    public class WarmupService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<WarmupService> _logger;
        private readonly IConfiguration _config;

        public WarmupService(IServiceProvider sp,
                             IHttpClientFactory httpFactory,
                             ILogger<WarmupService> logger,
                             IConfiguration config)
        {
            _sp = sp;
            _httpFactory = httpFactory;
            _logger = logger;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // liten fördröjning så Kestrel hinner lyfta
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

            try
            {
                // 1) Väcka DB (SELECT 1)
                using (var scope = _sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await db.Database.OpenConnectionAsync(stoppingToken);
                    await db.Database.ExecuteSqlRawAsync("SELECT 1;", cancellationToken: stoppingToken);
                    await db.Database.CloseConnectionAsync();
                }
                _logger.LogInformation("Warmup: DB connection ok.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Warmup: DB connection failed (ignored).");
            }

            try
            {
                var apiBase = _config["ApiSettings:BaseAddress"] ?? "https://sarasbloggapi.onrender.com/";
                var client = _httpFactory.CreateClient("Warmup"); // eller nameof(WarmupService) om du inte namnsatte klienten
                client.BaseAddress = new Uri(apiBase);
                client.Timeout = TimeSpan.FromSeconds(10);

                var pathsToTry = new[] { "healthz" };

                foreach (var path in pathsToTry)
                {
                    try
                    {
                        // 1) Försök HEAD (snabbare om stöds)
                        var head = new HttpRequestMessage(HttpMethod.Head, path);
                        var headResp = await client.SendAsync(head, stoppingToken);

                        if (headResp.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Warmup: API HEAD {Path} -> {Status}", path, (int)headResp.StatusCode);
                            break;
                        }

                        // 2) Fallback till GET om HEAD ej gav 2xx
                        var getResp = await client.GetAsync(path, stoppingToken);
                        _logger.LogInformation("Warmup: API GET {Path} -> {Status}", path, (int)getResp.StatusCode);
                        if (getResp.IsSuccessStatusCode) break;
                    }
                    catch (Exception exPath)
                    {
                        _logger.LogDebug(exPath, "Warmup: API request {Path} failed (ignored).", path);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Warmup: API wakeup failed (ignored).");
            }
        }
    }
}
