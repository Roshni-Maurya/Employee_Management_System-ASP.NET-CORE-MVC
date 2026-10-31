using Employee_Management_System.Data;
using Employee_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Employee_Management_System.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        public UserController(ApplicationDbContext context) => _context = context;

        // Login email se employee record dhoondhta hai
        private Task<Employee?> CurrentEmployee() =>
            _context.Employees.Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Email == User.Identity!.Name);

        public async Task<IActionResult> Index()
        {
            var emp = await CurrentEmployee();
            if (emp != null)
            {
                ViewBag.Pending = await _context.LeaveRequests.CountAsync(l => l.EmployeeId == emp.Id && l.Status == "Pending");
                ViewBag.Approved = await _context.LeaveRequests.CountAsync(l => l.EmployeeId == emp.Id && l.Status == "Approved");
                ViewBag.Rejected = await _context.LeaveRequests.CountAsync(l => l.EmployeeId == emp.Id && l.Status == "Rejected");

                // Last 3 decided leaves (Approved / Rejected) alert ke liye
                ViewBag.Decided = await _context.LeaveRequests
                    .Where(l => l.EmployeeId == emp.Id && l.Status != "Pending")
                    .OrderByDescending(l => l.Id)
                    .Take(3)
                    .ToListAsync();
            }
            return View(emp);
        }

        public async Task<IActionResult> Leaves()
        {
            var emp = await CurrentEmployee();
            if (emp == null) return RedirectToAction(nameof(Index));

            var list = await _context.LeaveRequests
                .Where(l => l.EmployeeId == emp.Id)
                .OrderByDescending(l => l.AppliedOn)
                .ToListAsync();
            return View(list);
        }

        public IActionResult ApplyLeave() => View(new LeaveRequest());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyLeave([Bind("LeaveType,FromDate,ToDate,Reason")] LeaveRequest model)
        {
            var emp = await CurrentEmployee();
            if (emp == null) return RedirectToAction(nameof(Index));

            if (model.ToDate < model.FromDate)
                ModelState.AddModelError("ToDate", "To date cannot be before From date.");

            if (model.FromDate.Date < DateTime.Today)
                ModelState.AddModelError("FromDate", "From date cannot be in the past.");

            bool overlap = await _context.LeaveRequests.AnyAsync(l =>
                l.EmployeeId == emp.Id && l.Status != "Rejected" &&
                l.FromDate <= model.ToDate && l.ToDate >= model.FromDate);
            if (overlap)
                ModelState.AddModelError("", "You already have a leave request for these dates.");

            if (!ModelState.IsValid) return View(model);

            model.EmployeeId = emp.Id;
            model.Status = "Pending";
            model.AppliedOn = DateTime.Now;

            _context.LeaveRequests.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave request submitted.";
            return RedirectToAction(nameof(Leaves));
        }
    }
}
