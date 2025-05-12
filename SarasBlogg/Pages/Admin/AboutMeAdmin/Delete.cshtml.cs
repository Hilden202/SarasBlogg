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
    public class DeleteModel : PageModel
    {
        private readonly SarasBlogg.Data.ApplicationDbContext _context;

        public DeleteModel(SarasBlogg.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public AboutMe AboutMe { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var aboutme = await _context.AboutMe.FirstOrDefaultAsync(m => m.Id == id);

            if (aboutme == null)
            {
                return NotFound();
            }
            else
            {
                AboutMe = aboutme;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var aboutme = await _context.AboutMe.FindAsync(id);
            if (aboutme != null)
            {
                AboutMe = aboutme;
                _context.AboutMe.Remove(AboutMe);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
