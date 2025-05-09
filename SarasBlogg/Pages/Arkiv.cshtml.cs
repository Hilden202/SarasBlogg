using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SarasBlogg.Data;
using SarasBlogg.ViewModels;

namespace SarasBlogg.Pages
{
    public class ArkivModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ArkivModel(ApplicationDbContext context)
        {
            _context = context;
        }
        //public List<Models.Blogg> Bloggs { get; set; }
        //public Models.Blogg Blogg { get; set; }

        public BloggViewModel ViewModel { get; set; } = new();

        public async Task OnGetAsync(int showId, int deleteId)
        {
            ViewModel.Bloggs = await _context.Blogg
                .Where(b => b.IsArchived && !b.Hidden && b.LaunchDate <= DateTime.Today)
                .ToListAsync();

            ViewModel.IsArchiveView = true;

            if (showId != 0)
            {
                ViewModel.Blogg = await _context.Blogg.FirstOrDefaultAsync(b => b.Id == showId && b.IsArchived && !b.Hidden);
            }
        }

    }
}
