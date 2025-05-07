using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.Data;
using SarasBlogg.Models;

namespace SarasBlogg.Pages.Admin.BloggAdmin
{
    public class IndexModel : PageModel
    {
        private readonly SarasBlogg.Data.ApplicationDbContext _context;

        public IndexModel(SarasBlogg.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Blogg> Blogg { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Blogg = await _context.Blogg.ToListAsync();
        }
    }
}
