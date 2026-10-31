using Employee_Management_System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Employee_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditController : Controller
    {
        private const int PageSize = 50;
        private readonly ApplicationDbContext _context;
        public AuditController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var q = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(a => a.UserEmail.Contains(search) ||
                                 a.Action.Contains(search) ||
                                 a.Entity.Contains(search) ||
                                 (a.Details != null && a.Details.Contains(search)));

            int total = await q.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            page = Math.Clamp(page, 1, totalPages);

            var items = await q.OrderByDescending(a => a.Id)
                               .Skip((page - 1) * PageSize)
                               .Take(PageSize)
                               .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Total = total;
            ViewBag.Search = search;
            return View(items);
        }
    }
}
