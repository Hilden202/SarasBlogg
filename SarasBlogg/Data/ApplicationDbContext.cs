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

        public DbSet<Blogg> Blogg { get; set; } = default!; //TODO: Ta bort när Blogg inte längre används via EF
        public DbSet<AboutMe> AboutMe { get; set; } = default!;
        public DbSet<ContactMe> ContactMe { get; set; } = default!;
        public DbSet<ForbiddenWord> ForbiddenWords { get; set; }

    }
}
