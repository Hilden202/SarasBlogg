using Microsoft.AspNetCore.Identity;

namespace SarasBlogg.Data
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public int? BirthYear { get; set; }
        [PersonalData]
        public string? Name { get; set; }
        [PersonalData]
        public string? ProfileImageUrl { get; set; }
    }
}
