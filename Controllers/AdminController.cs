using Employee_Management_System.Data;
using Employee_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Employee_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalEmployees = await _context.Employees.CountAsync(),
                ActiveEmployees = await _context.Employees.CountAsync(e => e.IsActive),
                TotalDepartments = await _context.Departments.CountAsync(),
                PendingLeaves = await _context.LeaveRequests.CountAsync(l => l.Status == "Pending"),
                // Count database mein hi nikalta hai (pehle Employees load nahi ho rahe the, isliye 0 aa raha tha)
                EmployeesByDepartment = await _context.Departments
                    .OrderBy(d => d.Name)
                    .Select(d => new { d.Name, Count = d.Employees.Count() })
                    .ToDictionaryAsync(x => x.Name, x => x.Count),
                RecentEmployees = await _context.Employees
                    .Include(e => e.Department)
                    .OrderByDescending(e => e.Id)
                    .Take(5)
                    .ToListAsync()
            };
            return View(vm);
        }
    }
}
