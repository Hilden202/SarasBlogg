using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using SarasBlogg.DAL;
using SarasBlogg.Services;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.AspNetCore.HttpOverrides;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;
using System.IO;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;


namespace SarasBlogg
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 🔹 Frontend base-URL (används t.ex. i e-postlänkar)
            var frontendBase = builder.Configuration["Frontend:BaseUrl"]
                ?? (builder.Environment.IsDevelopment() ? "https://localhost:7130" : "https://sarasblogg.onrender.com");

            // Bind endast i container (Render). Lokalt låter vi launchSettings styra.
            var portEnv = Environment.GetEnvironmentVariable("PORT");
            if (!string.IsNullOrEmpty(portEnv))
            {
                builder.WebHost.UseUrls($"http://0.0.0.0:{portEnv}");
            }

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            // --- DataProtection: miljömedveten och robust ---
            string keysPath;

            if (builder.Environment.IsDevelopment())
            {
                // Lokal: skriv i användarprofilen
                keysPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SarasBlogg", "data-keys");
            }
            else
            {
                // Render/produktion: skrivbar och beständig mapp på Render
                // (lever kvar mellan deploys)
                keysPath = "/opt/render/project/.render/data-keys";
            }

            // Skapa och använd vald plats, med fallback till /tmp om något strular
            try
            {
                Directory.CreateDirectory(keysPath);
            }
            catch
            {
                keysPath = "/tmp/sarasblogg-keys";
                Directory.CreateDirectory(keysPath);
            }

            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
                .SetApplicationName("SarasBloggSharedKeys");
            // --- slut DataProtection ---

            // 🔐 Endast cookie-auth i klienten (JWT hämtas från API och läggs i cookie + minnet)
            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Identity/Account/Login";
                    options.LogoutPath = "/Identity/Account/Logout";
                    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                    options.Cookie.Name = "SarasAuth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);

                    options.Events = new CookieAuthenticationEvents
                    {
                        OnSigningOut = ctx =>
                        {
                            ctx.HttpContext.Response.Cookies.Delete("api_access_token",
                                new CookieOptions { Path = "/" });
                            return Task.CompletedTask;
                        }
                    };
                });



            // AUTORISERINGSPOLICIES
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("SkaVaraSuperAdmin", p => p.RequireRole("superadmin"));          // endast superadmin
                options.AddPolicy("SkaVaraAdmin", p => p.RequireRole("superadmin", "admin")); // admin + superadmin
            });

            builder.Services.AddHttpContextAccessor();

            // BEHÖRIGHETER FÖR RAZOR PAGES
            builder.Services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizePage("/Admin", "SkaVaraAdmin");
                options.Conventions.AuthorizeFolder("/Admin/RoleAdmin", "SkaVaraAdmin"); // båda får se
            });


            // 1) EN sammanhängande retry-policy (GET/HEAD) för 5xx/408/HttpRequestException + 429
            static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
            {
                var jitter = new Random();

                return HttpPolicyExtensions
                    .HandleTransientHttpError()                       // 5xx, 408, HttpRequestException
                    .OrResult(msg => (int)msg.StatusCode == 429)      // Too Many Requests
                    .WaitAndRetryAsync(
                        retryCount: 8,
                        sleepDurationProvider: attempt =>
                            TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 15)) +
                            TimeSpan.FromMilliseconds(jitter.Next(0, 250))
                    );
            }

            // 2) Endast idempotenta metoder får retry
            static IAsyncPolicy<HttpResponseMessage> SelectPolicyFor(HttpRequestMessage req) =>
                (req.Method == HttpMethod.Get || req.Method == HttpMethod.Head)
                    ? GetRetryPolicy()
                    : Polly.Policy.NoOpAsync<HttpResponseMessage>();

            // TJÄNSTER
            builder.Services.AddScoped<BloggService>();

            builder.Services.AddScoped<IAccessTokenStore, InMemoryAccessTokenStore>();

            builder.Services.AddTransient<JwtAuthHandler>();

            // 🟨 Originalregistreringar — behållna men utkommenterade nedan:
            // builder.Services.AddScoped<BloggAPIManager>();
            // builder.Services.AddHttpClient<BloggImageAPIManager>();
            // builder.Services.AddScoped<CommentAPIManager>();
            // builder.Services.AddScoped<ForbiddenWordAPIManager>();
            // builder.Services.AddScoped<AboutMeAPIManager>();
            // builder.Services.AddHttpClient<AboutMeImageAPIManager>();
            // builder.Services.AddScoped<ContactMeAPIManager>();
            // builder.Services.AddSingleton<UserAPIManager>();

            // 🔹 API base URL från konfig (dev: appsettings.Development.json, prod: env ApiSettings__BaseAddress)
            var apiBase = builder.Configuration["ApiSettings:BaseAddress"]
                         ?? throw new InvalidOperationException("ApiSettings:BaseAddress is missing.");

            builder.Services.AddTransient<HttpClientLoggingHandler>();

            builder.Services.AddHttpClient("formspree", client =>
            {
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            })
                .AddHttpMessageHandler<HttpClientLoggingHandler>()
                .AddHttpMessageHandler<JwtAuthHandler>();

            builder.Services.AddHttpClient<UserAPIManager>(c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.Timeout = TimeSpan.FromSeconds(90);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),   // byt ut anslutningar regelbundet
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1) // släng riktigt gamla idle-anslutningar
            })
            .AddPolicyHandler((sp, req) => SelectPolicyFor(req))
            .AddHttpMessageHandler<HttpClientLoggingHandler>()
            .AddHttpMessageHandler<JwtAuthHandler>();

            builder.Services.AddHttpClient<BloggAPIManager>(c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.Timeout = TimeSpan.FromSeconds(90);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
            })
            .AddPolicyHandler((sp, req) => SelectPolicyFor(req))
            .AddHttpMessageHandler<HttpClientLoggingHandler>()
            .AddHttpMessageHandler<JwtAuthHandler>();

            builder.Services.AddHttpClient<BloggImageAPIManager>(c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.Timeout = TimeSpan.FromSeconds(90);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
            })
            .AddPolicyHandler((sp, req) => SelectPolicyFor(req))
            .AddHttpMessageHandler<HttpClientLoggingHandler>()
            .AddHttpMessageHandler<JwtAuthHandler>();

            builder.Services.AddHttpClient<CommentAPIManager>(c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.Timeout = TimeSpan.FromSeconds(90);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
            })
            .AddPolicyHandler((sp, req) => SelectPolicyFor(req))
            .AddHttpMessageHandler<HttpClientLoggingHandler>()
            .AddHttpMessageHandler<JwtAuthHandler>();

            builder.Services.AddHttpClient<ForbiddenWordAPIManager>(c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.Timeout = TimeSpan.FromSeconds(90);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
            })
            .AddPolicyHandler((sp, req) => SelectPolicyFor(req))
            .AddHttpMessageHandler<HttpClientLoggingHandler>()
            .AddHttpMessageHandler<JwtAuthHandler>();

            builder.Services.AddHttpClient<AboutMeAPIManager>(c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.Timeout = TimeSpan.FromSeconds(90);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
            })
            .AddPolicyHandler((sp, req) => SelectPolicyFor(req))
            .AddHttpMessageHandler<HttpClientLoggingHandler>()
            .AddHttpMessageHandler<JwtAuthHandler>();

            builder.Services.AddHttpClient<AboutMeImageAPIManager>(c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.Timeout = TimeSpan.FromSeconds(90);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
            })
            .AddPolicyHandler((sp, req) => SelectPolicyFor(req))
            .AddHttpMessageHandler<HttpClientLoggingHandler>()
            .AddHttpMessageHandler<JwtAuthHandler>();

            builder.Services.AddHttpClient<ContactMeAPIManager>(c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.Timeout = TimeSpan.FromSeconds(90);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
            })
            .AddPolicyHandler((sp, req) => SelectPolicyFor(req))
            .AddHttpMessageHandler<HttpClientLoggingHandler>()
            .AddHttpMessageHandler<JwtAuthHandler>();

            builder.Services.AddHttpClient<LikeAPIManager>(c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.Timeout = TimeSpan.FromSeconds(90);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
            })
            .AddPolicyHandler((sp, req) => SelectPolicyFor(req))
            .AddHttpMessageHandler<HttpClientLoggingHandler>()
            .AddHttpMessageHandler<JwtAuthHandler>();

            // COOKIEPOLICY
            builder.Services.Configure<CookiePolicyOptions>(options =>
             {
                 // Visa banners & kräv samtycke
                 options.CheckConsentNeeded = context => true;
                 // Cookies ska fungera över flikar/refresh men vara säkra
                 options.MinimumSameSitePolicy = SameSiteMode.Lax;
                 options.Secure = CookieSecurePolicy.Always;   // Render kör https
             });

            builder.Services.AddMemoryCache();

            builder.Services.AddHostedService<WarmupService>();

            var app = builder.Build();

            // Viktigt bakom proxy (Render) – tidigt i pipelinen
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
            });

            try
            {
                app.UseCookiePolicy();

                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error");
                    app.UseHsts();
                    // app.UseHttpsRedirection();
                }

                app.UseStaticFiles();
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();

                // Health endpoints
                app.MapGet("/healthz", () => Results.Ok("ok"));

                // HEAD-svar direkt
                app.Use(async (ctx, next) =>
                {
                    if (HttpMethods.IsHead(ctx.Request.Method))
                    {
                        ctx.Response.StatusCode = StatusCodes.Status200OK;
                        await ctx.Response.CompleteAsync();
                        return;
                    }
                    await next();
                });

                app.MapRazorPages();

                app.Run();
            }
            catch (Exception ex)
            {
                // Logga ALLT till stderr (Render fångar upp)
                Console.Error.WriteLine("❌ Fatal startup exception:");
                Console.Error.WriteLine(ex.ToString());
                throw; // låt processen faila så Render visar tydlig logg
            }
        }
    }
}
