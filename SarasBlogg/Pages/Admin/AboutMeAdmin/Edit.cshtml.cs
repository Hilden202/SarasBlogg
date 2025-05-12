using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.Data;
using SarasBlogg.Models;

namespace SarasBlogg.Pages.Admin.AboutMeAdmin
{
    public class EditModel : PageModel
    {
        private readonly SarasBlogg.Data.ApplicationDbContext _context;

        public EditModel(SarasBlogg.Data.ApplicationDbContext context)
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

            var aboutme =  await _context.AboutMe.FirstOrDefaultAsync(m => m.Id == id);
            if (aboutme == null)
            {
                return NotFound();
            }
            AboutMe = aboutme;
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(AboutMe).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AboutMeExists(AboutMe.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool AboutMeExists(int id)
        {
            return _context.AboutMe.Any(e => e.Id == id);
        }
    }
}
