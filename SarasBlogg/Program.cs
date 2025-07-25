using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.DAL;
using SarasBlogg.Data;
using SarasBlogg.Services;
using Npgsql.EntityFrameworkCore.PostgreSQL;


namespace SarasBlogg
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Konfigurera
            //builder.Configuration.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);

            // Konfigurera apptjänster och databasanslutning
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // DATABAS OCH IDENTITET
            //builder.Services.AddDbContext<ApplicationDbContext>(options =>
            //    options.UseSqlServer(connectionString));

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));


            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
                options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

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

            // TJÄNSTER
            builder.Services.AddScoped<IFileHelper, FileHelper>();
            builder.Services.AddScoped<BloggService>();
            builder.Services.AddScoped<BloggAPIManager>();
            builder.Services.AddScoped<CommentAPIManager>();
            builder.Services.AddScoped<ForbiddenWordAPIManager>();
            builder.Services.AddScoped<AboutMeAPIManager>();
            builder.Services.AddScoped<ContactMeAPIManager>();
            builder.Services.AddSingleton<UserAPIManager>();


            // COOKIEPOLICY
            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });


            var app = builder.Build();

            app.UseCookiePolicy(); // slå på cookie policy

            // konfigurera
            //CreateAdminUserAsync(app).GetAwaiter().GetResult(); // nödvändigt för att skapa admin-användaren innan appen startar. kommentera in om databasen  är ny

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
        //public static async Task CreateAdminUserAsync(WebApplication app)
        //{
        //    // Hämta UserManager och RoleManager från DI
        //    using var scope = app.Services.CreateScope();
        //    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        //    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        //    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        //    string adminEmail = config["AdminUser:Email"];
        //    string adminPassword = config["AdminUser:Password"];
        //    string superAdminRole = "superadmin";

        //    // Skapa rollen superadmin om den inte finns
        //    if (!await roleManager.RoleExistsAsync(superAdminRole))
        //    {
        //        await roleManager.CreateAsync(new IdentityRole(superAdminRole));
        //    }

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
