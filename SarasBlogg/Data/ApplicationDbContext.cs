using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Core.Types;
using SarasBlogg.Models;

namespace SarasBlogg.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
        {
            
        }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AboutMe> AboutMe { get; set; } = default!;
        public DbSet<ContactMe> ContactMe { get; set; } = default!;
        public DbSet<ForbiddenWord> ForbiddenWords { get; set; }

    }
}
