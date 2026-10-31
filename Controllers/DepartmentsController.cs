using Employee_Management_System.Data;
using Employee_Management_System.Models;
using Employee_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Employee_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public DepartmentsController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _context.Departments
                .Include(d => d.Employees)
                .OrderBy(d => d.Name)
                .ToListAsync();
            return View(list);
        }

        public IActionResult Create() => View("Form", new Department());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department d)
        {
            if (await _context.Departments.AnyAsync(x => x.Name == d.Name))
                ModelState.AddModelError("Name", "Department already exists.");

            if (!ModelState.IsValid) return View("Form", d);

            _context.Departments.Add(d);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Department Created", "Department", d.Id.ToString(), d.Name);
            TempData["Success"] = "Department added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var d = await _context.Departments.FindAsync(id);
            if (d == null) return NotFound();
            return View("Form", d);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Department d)
        {
            if (await _context.Departments.AnyAsync(x => x.Name == d.Name && x.Id != d.Id))
                ModelState.AddModelError("Name", "Department already exists.");

            if (!ModelState.IsValid) return View("Form", d);

            var existing = await _context.Departments.FindAsync(d.Id);
            if (existing == null) return NotFound();

            string oldName = existing.Name;
            existing.Name = d.Name;
            existing.Description = d.Description;
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Department Updated", "Department", d.Id.ToString(),
                oldName == d.Name ? d.Name : $"Name: {oldName} -> {d.Name}");
            TempData["Success"] = "Department updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var d = await _context.Departments.Include(x => x.Employees).FirstOrDefaultAsync(x => x.Id == id);
            if (d == null) return NotFound();

            if (d.Employees.Any())
            {
                TempData["Error"] = "Cannot delete: employees exist in this department.";
                return RedirectToAction(nameof(Index));
            }

            string name = d.Name;
            _context.Departments.Remove(d);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Department Deleted", "Department", id.ToString(), name);
            TempData["Success"] = "Department deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
