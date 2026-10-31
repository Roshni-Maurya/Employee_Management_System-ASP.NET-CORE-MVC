using Employee_Management_System.Data;
using Employee_Management_System.Models;
using Employee_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Employee_Management_System.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public UserController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // Login email se employee record dhoondhta hai
        private Task<Employee?> CurrentEmployee() =>
            _context.Employees.Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Email == User.Identity!.Name);

        // Saal ki leave balance (Pending + Approved gine jaate hain; Rejected/Cancelled nahi)
        private async Task<List<LeaveBalanceItem>> GetBalances(int employeeId, int year, int excludeId = 0)
        {
            var leaves = await _context.LeaveRequests
                .Where(l => l.EmployeeId == employeeId
                            && l.Id != excludeId
                            && (l.Status == "Pending" || l.Status == "Approved")
                            && l.FromDate.Year == year)
                .ToListAsync();

            return LeavePolicy.YearlyQuota.Select(q => new LeaveBalanceItem
            {
                Type = q.Key,
                Quota = q.Value,
                Used = leaves.Where(l => l.LeaveType == q.Key).Sum(l => l.Days)
            }).ToList();
        }

        private async Task LoadBalances(int employeeId)
        {
            ViewBag.Balances = await GetBalances(employeeId, DateTime.Today.Year);
        }

        // Apply aur Edit dono ke liye same checks
        private async Task ValidateLeaveAsync(Employee emp, LeaveRequest m, int excludeId = 0)
        {
            if (!LeavePolicy.Types.Contains(m.LeaveType))
                ModelState.AddModelError("LeaveType", "Invalid leave type.");

            if (m.ToDate < m.FromDate)
            {
                ModelState.AddModelError("ToDate", "To date cannot be before From date.");
                return;
            }
            if (m.FromDate.Date < DateTime.Today)
                ModelState.AddModelError("FromDate", "From date cannot be in the past.");

            if (m.ToDate.Year != m.FromDate.Year)
            {
                ModelState.AddModelError("ToDate", "Please apply separately for each calendar year.");
                return;
            }

            bool overlap = await _context.LeaveRequests.AnyAsync(l =>
                l.EmployeeId == emp.Id && l.Id != excludeId &&
                l.Status != "Rejected" && l.Status != "Cancelled" &&
                l.FromDate <= m.ToDate && l.ToDate >= m.FromDate);
            if (overlap)
                ModelState.AddModelError("", "You already have a leave request for these dates.");

            if (LeavePolicy.YearlyQuota.TryGetValue(m.LeaveType, out int quota))
            {
                var balances = await GetBalances(emp.Id, m.FromDate.Year, excludeId);
                int used = balances.First(b => b.Type == m.LeaveType).Used;
                int left = Math.Max(quota - used, 0);
                if (m.Days > left)
                    ModelState.AddModelError("",
                        $"Not enough balance: only {left} day(s) of {m.LeaveType} leave left for {m.FromDate.Year}.");
            }
        }

        public async Task<IActionResult> Index()
        {
            var emp = await CurrentEmployee();
            if (emp != null)
            {
                ViewBag.Pending = await _context.LeaveRequests.CountAsync(l => l.EmployeeId == emp.Id && l.Status == "Pending");
                ViewBag.Approved = await _context.LeaveRequests.CountAsync(l => l.EmployeeId == emp.Id && l.Status == "Approved");
                ViewBag.Rejected = await _context.LeaveRequests.CountAsync(l => l.EmployeeId == emp.Id && l.Status == "Rejected");

                await LoadBalances(emp.Id);

                // Last 3 decisions (Approved / Rejected) alert ke liye
                ViewBag.Decided = await _context.LeaveRequests
                    .Where(l => l.EmployeeId == emp.Id && (l.Status == "Approved" || l.Status == "Rejected"))
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

        // ---------------- Apply ----------------
        public async Task<IActionResult> ApplyLeave()
        {
            var emp = await CurrentEmployee();
            if (emp == null) return RedirectToAction(nameof(Index));

            await LoadBalances(emp.Id);
            return View(new LeaveRequest());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyLeave([Bind("LeaveType,FromDate,ToDate,Reason")] LeaveRequest model)
        {
            var emp = await CurrentEmployee();
            if (emp == null) return RedirectToAction(nameof(Index));

            await ValidateLeaveAsync(emp, model);

            if (!ModelState.IsValid)
            {
                await LoadBalances(emp.Id);
                return View(model);
            }

            model.EmployeeId = emp.Id;
            model.Status = "Pending";
            model.AppliedOn = DateTime.Now;

            _context.LeaveRequests.Add(model);
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Leave Applied", "LeaveRequest", model.Id.ToString(),
                $"{emp.EmployeeCode} {model.LeaveType} {model.FromDate:dd MMM yyyy} to {model.ToDate:dd MMM yyyy} ({model.Days} day(s))");

            TempData["Success"] = "Leave request submitted.";
            return RedirectToAction(nameof(Leaves));
        }

        // ---------------- Edit (sirf Pending) ----------------
        public async Task<IActionResult> EditLeave(int id)
        {
            var emp = await CurrentEmployee();
            if (emp == null) return RedirectToAction(nameof(Index));

            var leave = await _context.LeaveRequests
                .FirstOrDefaultAsync(l => l.Id == id && l.EmployeeId == emp.Id);   // sirf apni leave
            if (leave == null) return NotFound();

            if (leave.Status != "Pending")
            {
                TempData["Error"] = "Only pending leaves can be edited.";
                return RedirectToAction(nameof(Leaves));
            }

            await LoadBalances(emp.Id);
            return View("ApplyLeave", leave);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLeave([Bind("Id,LeaveType,FromDate,ToDate,Reason")] LeaveRequest model)
        {
            var emp = await CurrentEmployee();
            if (emp == null) return RedirectToAction(nameof(Index));

            var leave = await _context.LeaveRequests
                .FirstOrDefaultAsync(l => l.Id == model.Id && l.EmployeeId == emp.Id);
            if (leave == null) return NotFound();

            if (leave.Status != "Pending")
            {
                TempData["Error"] = "This leave can no longer be edited (already decided or cancelled).";
                return RedirectToAction(nameof(Leaves));
            }

            await ValidateLeaveAsync(emp, model, leave.Id);

            if (!ModelState.IsValid)
            {
                await LoadBalances(emp.Id);
                return View("ApplyLeave", model);
            }

            string before = $"{leave.LeaveType} {leave.FromDate:dd MMM yyyy} to {leave.ToDate:dd MMM yyyy}";

            leave.LeaveType = model.LeaveType;
            leave.FromDate = model.FromDate;
            leave.ToDate = model.ToDate;
            leave.Reason = model.Reason;
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Leave Edited", "LeaveRequest", leave.Id.ToString(),
                $"{emp.EmployeeCode}: {before} -> {leave.LeaveType} {leave.FromDate:dd MMM yyyy} to {leave.ToDate:dd MMM yyyy}");

            TempData["Success"] = "Leave request updated.";
            return RedirectToAction(nameof(Leaves));
        }

        // ---------------- Cancel (sirf Pending) ----------------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelLeave(int id)
        {
            var emp = await CurrentEmployee();
            if (emp == null) return RedirectToAction(nameof(Index));

            var leave = await _context.LeaveRequests
                .FirstOrDefaultAsync(l => l.Id == id && l.EmployeeId == emp.Id);
            if (leave == null) return NotFound();

            if (leave.Status != "Pending")
            {
                TempData["Error"] = "Only pending leaves can be cancelled.";
                return RedirectToAction(nameof(Leaves));
            }

            leave.Status = "Cancelled";
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Leave Cancelled", "LeaveRequest", leave.Id.ToString(),
                $"{emp.EmployeeCode} {leave.LeaveType} {leave.FromDate:dd MMM yyyy} to {leave.ToDate:dd MMM yyyy}");

            TempData["Success"] = "Leave request cancelled.";
            return RedirectToAction(nameof(Leaves));
        }
    }
}
