using Employee_Management_System.Data;
using Employee_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Employee_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DepartmentsController(ApplicationDbContext context) => _context = context;

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

            existing.Name = d.Name;
            existing.Description = d.Description;
            await _context.SaveChangesAsync();
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

            _context.Departments.Remove(d);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Department deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
