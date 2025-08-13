using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.DAL;
using SarasBlogg.Data;
using SarasBlogg.Services;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System.Security.Claims;
using Microsoft.AspNetCore.HttpOverrides;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;
using HealthChecks.NpgSql;

namespace SarasBlogg
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Bind endast i container (Render). Lokalt låter vi launchSettings styra.
            var portEnv = Environment.GetEnvironmentVariable("PORT");
            if (!string.IsNullOrEmpty(portEnv))
            {
                builder.WebHost.UseUrls($"http://0.0.0.0:{portEnv}");
            }

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            // Hämta connection string (stöder både DefaultConnection och MyConnection)
            var connectionString =
                builder.Configuration.GetConnectionString("DefaultConnection")
                ?? builder.Configuration.GetConnectionString("MyConnection")
                ?? throw new InvalidOperationException(
                    "No connection string found. Expected 'DefaultConnection' or 'MyConnection'.");

            //    - SSL krävs ofta: "SSL Mode=Require; Trust Server Certificate=true"
            //    - Håll liv efter kallstart: "Keepalive=30"
            //    - Kortare login-timeout: "Timeout=15"
            var csb = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
            {
                SslMode = Npgsql.SslMode.Require,
                TrustServerCertificate = true,
                KeepAlive = 30,
                Timeout = 15
            };
            var pgConn = csb.ConnectionString;

            // Health checks
            builder.Services.AddHealthChecks()
                .AddNpgSql(pgConn, name: "postgres");

            // EF Core med retry-on - failure
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(pgConn, npgsql =>
                {
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                }));

            // DATABAS OCH IDENTITET
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
                options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier;
            });

            // AUTORISERINGSPOLICIES
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("SkaVaraSuperAdmin", policy => policy.RequireRole("superadmin"));
                options.AddPolicy("SkaVaraAdmin", policy => policy.RequireRole("superadmin", "admin"));
            });

            // BEHÖRIGHETER FÖR RAZOR PAGES
            builder.Services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizePage("/Admin", "SkaVaraAdmin");
                options.Conventions.AuthorizePage("/RoleAdmin", "SkaVaraSuperAdmin");
            });

            // 🔹 Polly-retry-policy för API-anrop (tål kallstart på Render)
            static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
            {
                var jitter = new Random();
                return HttpPolicyExtensions
                    .HandleTransientHttpError()                // 5xx, 408, nätfel
                    .OrResult(msg => (int)msg.StatusCode == 429)
                    .WaitAndRetryAsync(5, retryAttempt =>
                        TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt)) +
                        TimeSpan.FromMilliseconds(jitter.Next(0, 250)));
            }

            // TJÄNSTER
            builder.Services.AddScoped<BloggService>();

            // 🟨 Originalregistreringar — behållna men utkommenterade nedan:
            // builder.Services.AddScoped<BloggAPIManager>();
            // builder.Services.AddHttpClient<BloggImageAPIManager>();
            // builder.Services.AddScoped<CommentAPIManager>();
            // builder.Services.AddScoped<ForbiddenWordAPIManager>();
            // builder.Services.AddScoped<AboutMeAPIManager>();
            // builder.Services.AddHttpClient<AboutMeImageAPIManager>();
            // builder.Services.AddScoped<ContactMeAPIManager>();
            // builder.Services.AddSingleton<UserAPIManager>();

            // 🔹 Nytt: registrera typed HttpClient för alla API-managers med BaseAddress + Polly
            var apiBase = builder.Configuration["ApiSettings:BaseAddress"]; // ex: https://sarasbloggapi.onrender.com/
            if (string.IsNullOrWhiteSpace(apiBase))
            {
                // fallback om ej satt – hellre tom än null
                apiBase = "https://sarasbloggapi.onrender.com/";
            }

            builder.Services.AddHttpClient<BloggAPIManager>(c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.Timeout = TimeSpan.FromSeconds(15);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),   // byt ut anslutningar regelbundet
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1) // släng riktigt gamla idle-anslutningar
            })
            .AddPolicyHandler(GetRetryPolicy());

            builder.Services.AddHttpClient<BloggImageAPIManager>(c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.Timeout = TimeSpan.FromSeconds(15);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),   // byt ut anslutningar regelbundet
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1) // släng riktigt gamla idle-anslutningar
            })
            .AddPolicyHandler(GetRetryPolicy());

            builder.Services.AddHttpClient<CommentAPIManager>(c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.Timeout = TimeSpan.FromSeconds(15);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),   // byt ut anslutningar regelbundet
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1) // släng riktigt gamla idle-anslutningar
            })
            .AddPolicyHandler(GetRetryPolicy());

            builder.Services.AddHttpClient<ForbiddenWordAPIManager>(c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.Timeout = TimeSpan.FromSeconds(15);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),   // byt ut anslutningar regelbundet
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1) // släng riktigt gamla idle-anslutningar
            })
            .AddPolicyHandler(GetRetryPolicy());

            builder.Services.AddHttpClient<AboutMeAPIManager>(c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.Timeout = TimeSpan.FromSeconds(15);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),   // byt ut anslutningar regelbundet
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1) // släng riktigt gamla idle-anslutningar
            })
            .AddPolicyHandler(GetRetryPolicy());

            builder.Services.AddHttpClient<AboutMeImageAPIManager>(c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.Timeout = TimeSpan.FromSeconds(15);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),   // byt ut anslutningar regelbundet
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1) // släng riktigt gamla idle-anslutningar
            })
            .AddPolicyHandler(GetRetryPolicy());

            builder.Services.AddHttpClient<ContactMeAPIManager>(c =>
            {
                c.BaseAddress = new Uri(apiBase);
                c.Timeout = TimeSpan.FromSeconds(15);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),   // byt ut anslutningar regelbundet
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1) // släng riktigt gamla idle-anslutningar
            })
            .AddPolicyHandler(GetRetryPolicy());

            // Om UserAPIManager behöver HttpClient: byt till typed klient
            // (om klassen inte tar HttpClient i konstruktorn, låt din Singleton stå kvar)
            builder.Services.AddSingleton<UserAPIManager>(); // ✅ lämnas oförändrad om den inte använder HttpClient

            // COOKIEPOLICY
            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            var app = builder.Build();

            // Viktigt bakom proxy (Render) – tidigt i pipelinen
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
            });

            try
            {
                app.UseCookiePolicy();

                if (app.Environment.IsDevelopment())
                {
                    app.UseMigrationsEndPoint();
                }
                else
                {
                    app.UseExceptionHandler("/Error");
                    app.UseHsts();
                    // app.UseHttpsRedirection(); // fortsatt avstängt i container om du vill undvika https-portvarning
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

                // Health endpoints
                app.MapHealthChecks("/health/db");

                app.Run();
            }
            catch (Exception ex)
            {
                // Logga ALLT till stderr (Render fångar upp)
                Console.Error.WriteLine("❌ Fatal startup exception:");
                Console.Error.WriteLine(ex.ToString());
                throw; // låt processen faila så Render visar tydlig logg
            }

            //public static async Task CreateAdminUserAsync(WebApplication app)
            //{
            //    // Hämta UserManager och RoleManager från DI
            //    using var scope = app.Services.CreateScope();
            //    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            //    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            //    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            //
            //    string adminEmail = config["AdminUser:Email"];
            //    string adminPassword = config["AdminUser:Password"];
            //    string superAdminRole = "superadmin";
            //
            //    // Skapa rollen superadmin om den inte finns
            //    if (!await roleManager.RoleExistsAsync(superAdminRole))
            //    {
            //        await roleManager.CreateAsync(new IdentityRole(superAdminRole));
            //    }
            //
            //    // Kolla om admin-användaren finns, annars skapa den
            //    var adminUser = await userManager.FindByEmailAsync(adminEmail);
            //    if (adminUser == null)
            //    {
            //        adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            //        var result = await userManager.CreateAsync(adminUser, adminPassword);
            //        if (result.Succeeded)
            //        {
            //            await userManager.AddToRoleAsync(adminUser, superAdminRole);
            //        }
            //        else
            //        {
            //            // Hantera fel, t.ex. logga det eller kasta exception
            //            throw new Exception("Misslyckades skapa admin-användaren: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            //        }
            //    }
            //}
        }
    }
}