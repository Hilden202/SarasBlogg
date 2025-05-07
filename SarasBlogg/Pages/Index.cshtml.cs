using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.Data;

namespace SarasBlogg.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }
        public List<Models.Blogg> Bloggs { get; set; }
        public Models.Blogg Blogg { get; set; }

        public async Task OnGetAsync(int showId, int deleteId)
        {
            Bloggs = await _context.Blogg.ToListAsync();

            if (showId != 0)
            {
                Blogg = Bloggs.Where(b => b.Id == showId).FirstOrDefault();
            }

        }
    }
}
