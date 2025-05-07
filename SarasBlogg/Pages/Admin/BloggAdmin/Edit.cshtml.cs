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

namespace SarasBlogg.Pages.Admin.BloggAdmin
{
    public class EditModel : PageModel
    {
        private readonly SarasBlogg.Data.ApplicationDbContext _context;

        public EditModel(SarasBlogg.Data.ApplicationDbContext context)
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

            var blogg =  await _context.Blogg.FirstOrDefaultAsync(m => m.Id == id);
            if (blogg == null)
            {
                return NotFound();
            }
            Blogg = blogg;
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

            _context.Attach(Blogg).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BloggExists(Blogg.Id))
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

        private bool BloggExists(int id)
        {
            return _context.Blogg.Any(e => e.Id == id);
        }
    }
}
