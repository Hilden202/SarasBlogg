using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.Models;

namespace SarasBlogg.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext()
        {
            
        }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Blogg> Blogg { get; set; } = default!;
        public DbSet<AboutMe> AboutMe { get; set; } = default!;
    }
}
