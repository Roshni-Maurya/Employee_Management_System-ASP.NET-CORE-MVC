using Employee_Management_System.Data;
using Employee_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Employee_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LeavesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _email;

        public LeavesController(ApplicationDbContext context, IEmailService email)
        {
            _context = context;
            _email = email;
        }

        public async Task<IActionResult> Index(string? status)
        {
            var q = _context.LeaveRequests.Include(l => l.Employee).AsQueryable();
            if (!string.IsNullOrEmpty(status))
                q = q.Where(l => l.Status == status);

            ViewBag.Status = status;
            return View(await q.OrderByDescending(l => l.AppliedOn).ToListAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(int id, string status, string? remark)
        {
            if (status != "Approved" && status != "Rejected") return BadRequest();

            var leave = await _context.LeaveRequests
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);
            if (leave == null || leave.Employee == null) return NotFound();

            // Ek baar decide ho gayi leave dobara badli nahi ja sakti
            if (leave.Status != "Pending")
            {
                TempData["Error"] = "This leave has already been decided.";
                return RedirectToAction(nameof(Index));
            }

            leave.Status = status;
            leave.AdminRemark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();
            await _context.SaveChangesAsync();

            bool sent = await _email.SendLeaveDecisionAsync(leave, leave.Employee);

            if (sent)
                TempData["Success"] = $"Leave {status.ToLower()} and email sent to {leave.Employee.FullName}.";
            else
                TempData["Error"] = $"Leave {status.ToLower()}, but the email could not be sent. Check email settings.";

            return RedirectToAction(nameof(Index));
        }
    }
}
