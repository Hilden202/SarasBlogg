using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.Data;
using SarasBlogg.Models;

namespace SarasBlogg.Pages.Admin.AboutMeAdmin
{
    public class IndexModel : PageModel
    {
        private readonly SarasBlogg.Data.ApplicationDbContext _context;

        public IndexModel(SarasBlogg.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<AboutMe> AboutMe { get;set; } = default!;

        public async Task OnGetAsync()
        {
            AboutMe = await _context.AboutMe.ToListAsync();
        }
    }
}
