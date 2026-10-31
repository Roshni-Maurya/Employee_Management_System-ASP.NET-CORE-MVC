using Employee_Management_System.Data;
using Employee_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Employee_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public EmployeesController(ApplicationDbContext context) => _context = context;

        private async Task LoadDepartments(int? selected = null)
        {
            ViewBag.Departments = new SelectList(
                await _context.Departments.OrderBy(d => d.Name).ToListAsync(),
                "Id", "Name", selected);
        }

        public async Task<IActionResult> Index(string? search, int? departmentId)
        {
            var q = _context.Employees.Include(e => e.Department).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(e => e.FullName.Contains(search) ||
                                 e.EmployeeCode.Contains(search) ||
                                 e.Email.Contains(search));

            if (departmentId.HasValue)
                q = q.Where(e => e.DepartmentId == departmentId);

            await LoadDepartments(departmentId);
            ViewBag.Search = search;
            return View(await q.OrderBy(e => e.EmployeeCode).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var emp = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.LeaveRequests)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (emp == null) return NotFound();
            return View(emp);
        }

        public async Task<IActionResult> Create()
        {
            var next = (await _context.Employees.MaxAsync(e => (int?)e.Id) ?? 0) + 1;
            await LoadDepartments();
            return View("Form", new Employee { EmployeeCode = "EMP" + next.ToString("D4") });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee e)
        {
            if (await _context.Employees.AnyAsync(x => x.EmployeeCode == e.EmployeeCode))
                ModelState.AddModelError("EmployeeCode", "Employee code already used.");
            if (await _context.Employees.AnyAsync(x => x.Email == e.Email))
                ModelState.AddModelError("Email", "Email already used.");

            if (!ModelState.IsValid)
            {
                await LoadDepartments(e.DepartmentId);
                return View("Form", e);
            }

            _context.Employees.Add(e);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Employee added. They can now register with this email.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var e = await _context.Employees.FindAsync(id);
            if (e == null) return NotFound();
            await LoadDepartments(e.DepartmentId);
            return View("Form", e);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Employee e)
        {
            if (await _context.Employees.AnyAsync(x => x.EmployeeCode == e.EmployeeCode && x.Id != e.Id))
                ModelState.AddModelError("EmployeeCode", "Employee code already used.");
            if (await _context.Employees.AnyAsync(x => x.Email == e.Email && x.Id != e.Id))
                ModelState.AddModelError("Email", "Email already used.");

            if (!ModelState.IsValid)
            {
                await LoadDepartments(e.DepartmentId);
                return View("Form", e);
            }

            if (!await _context.Employees.AnyAsync(x => x.Id == e.Id)) return NotFound();

            _context.Employees.Update(e);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Employee updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _context.Employees.FindAsync(id);
            if (e != null)
            {
                _context.Employees.Remove(e);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Employee deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
