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
    public class DeleteModel : PageModel
    {
        private readonly SarasBlogg.Data.ApplicationDbContext _context;

        public DeleteModel(SarasBlogg.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Blogg Blogg { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var blogg = await _context.Blogg.FirstOrDefaultAsync(m => m.Id == id);

            if (blogg == null)
            {
                return NotFound();
            }
            else
            {
                Blogg = blogg;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var blogg = await _context.Blogg.FindAsync(id);
            if (blogg != null)
            {
                Blogg = blogg;
                _context.Blogg.Remove(Blogg);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
