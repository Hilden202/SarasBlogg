﻿using System.Net.Http;
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

            // 1) Väcka DB (SELECT 1)
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.OpenConnectionAsync(stoppingToken);
                await db.Database.ExecuteSqlRawAsync("SELECT 1;", cancellationToken: stoppingToken);
                await db.Database.CloseConnectionAsync();
                _logger.LogInformation("Warmup: DB connection ok.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Warmup: DB connection failed (ignored).");
            }

            // 2) Väcka API
            try
            {
                var apiBase = _config["ApiSettings:BaseAddress"] ?? "https://sarasbloggapi.onrender.com/";
                if (!apiBase.EndsWith("/")) apiBase += "/";
                var client = _httpFactory.CreateClient("Warmup");
                client.BaseAddress = new Uri(apiBase);
                client.Timeout = TimeSpan.FromSeconds(45); // kallstart kan ta tid

                // API har "/" och "/health", testa båda
                var pathsToTry = new[] { "/", "/health" };

                foreach (var path in pathsToTry)
                {
                    var attempts = 0;
                    var delay = TimeSpan.FromSeconds(2);

                    while (attempts < 3 && !stoppingToken.IsCancellationRequested)
                    {
                        attempts++;
                        try
                        {
                            // HEAD först
                            var head = new HttpRequestMessage(HttpMethod.Head, path);
                            var headResp = await client.SendAsync(head, stoppingToken);
                            if (headResp.IsSuccessStatusCode)
                            {
                                _logger.LogInformation("Warmup: API HEAD {Path} -> {Status}", path, (int)headResp.StatusCode);
                                return;
                            }

                            // fallback GET
                            var getResp = await client.GetAsync(path, stoppingToken);
                            if (getResp.IsSuccessStatusCode)
                            {
                                _logger.LogInformation("Warmup: API GET {Path} -> {Status}", path, (int)getResp.StatusCode);
                                return;
                            }
                        }
                        catch (Exception exPath)
                        {
                            _logger.LogDebug(exPath, "Warmup: API request {Path} failed (attempt {Attempt}).", path, attempts);
                        }

                        await Task.Delay(delay, stoppingToken);
                        delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 10));
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
