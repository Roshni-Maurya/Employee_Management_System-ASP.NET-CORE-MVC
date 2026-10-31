using Employee_Management_System.Data;
using Employee_Management_System.Models;
using Employee_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Employee_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _audit;

        public EmployeesController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditService audit)
        {
            _context = context;
            _userManager = userManager;
            _audit = audit;
        }

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
            if (await _userManager.FindByEmailAsync(e.Email) != null)
                ModelState.AddModelError("Email", "This email already has a login account.");

            if (!ModelState.IsValid)
            {
                await LoadDepartments(e.DepartmentId);
                return View("Form", e);
            }

            _context.Employees.Add(e);
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Employee Created", "Employee", e.Id.ToString(),
                $"{e.EmployeeCode} - {e.FullName} ({e.Email})");

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
            var old = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == e.Id);
            if (old == null) return NotFound();

            if (await _context.Employees.AnyAsync(x => x.EmployeeCode == e.EmployeeCode && x.Id != e.Id))
                ModelState.AddModelError("EmployeeCode", "Employee code already used.");
            if (await _context.Employees.AnyAsync(x => x.Email == e.Email && x.Id != e.Id))
                ModelState.AddModelError("Email", "Email already used.");

            // Is employee ka login account (agar bana ho) purane email se dhoondho
            var linked = await _userManager.FindByEmailAsync(old.Email);
            bool emailChanged = !string.Equals(old.Email, e.Email, StringComparison.OrdinalIgnoreCase);

            if (emailChanged)
            {
                var clash = await _userManager.FindByEmailAsync(e.Email);
                if (clash != null && (linked == null || clash.Id != linked.Id))
                    ModelState.AddModelError("Email", "This email already has a login account.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDepartments(e.DepartmentId);
                return View("Form", e);
            }

            // Employee aur login account ek saath update hon (transaction): ya dono, ya koi nahi
            await using var tx = await _context.Database.BeginTransactionAsync();

            _context.Employees.Update(e);
            await _context.SaveChangesAsync();

            if (linked != null && (emailChanged || linked.FullName != e.FullName))
            {
                linked.FullName = e.FullName;
                var errors = new List<IdentityError>();

                if (emailChanged)
                {
                    errors.AddRange((await _userManager.SetEmailAsync(linked, e.Email)).Errors);
                    errors.AddRange((await _userManager.SetUserNameAsync(linked, e.Email)).Errors);
                }
                errors.AddRange((await _userManager.UpdateAsync(linked)).Errors);

                if (errors.Any())
                {
                    foreach (var err in errors) ModelState.AddModelError("", err.Description);
                    await LoadDepartments(e.DepartmentId);
                    return View("Form", e);   // commit nahi hua, to sab rollback
                }
            }

            await tx.CommitAsync();

            await _audit.LogAsync("Employee Updated", "Employee", e.Id.ToString(),
                $"{e.EmployeeCode}: {Diff(old, e)}" +
                (emailChanged && linked != null ? " | Login email updated too" : ""));

            TempData["Success"] = emailChanged && linked != null
                ? "Employee updated. Login email was also updated; the employee must login again with the new email."
                : "Employee updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _context.Employees.FindAsync(id);
            if (e == null) return RedirectToAction(nameof(Index));

            var linked = await _userManager.FindByEmailAsync(e.Email);
            string info = $"{e.EmployeeCode} - {e.FullName} ({e.Email})";

            await using var tx = await _context.Database.BeginTransactionAsync();

            _context.Employees.Remove(e);
            await _context.SaveChangesAsync();

            // Login account bhi hatao (Admin ka account kabhi nahi)
            bool loginRemoved = false;
            if (linked != null && !await _userManager.IsInRoleAsync(linked, "Admin"))
            {
                var r = await _userManager.DeleteAsync(linked);
                loginRemoved = r.Succeeded;
            }

            await tx.CommitAsync();

            await _audit.LogAsync("Employee Deleted", "Employee", id.ToString(),
                info + (loginRemoved ? " | Login account removed" : ""));

            TempData["Success"] = "Employee deleted.";
            return RedirectToAction(nameof(Index));
        }

        // Kaun si field badli, uski list (audit log ke liye)
        private static string Diff(Employee o, Employee n)
        {
            var parts = new List<string>();
            void Add(string label, object? a, object? b)
            {
                if (a?.ToString() != b?.ToString()) parts.Add($"{label}: {a} -> {b}");
            }

            Add("Code", o.EmployeeCode, n.EmployeeCode);
            Add("Name", o.FullName, n.FullName);
            Add("Email", o.Email, n.Email);
            Add("Phone", o.Phone, n.Phone);
            Add("Gender", o.Gender, n.Gender);
            Add("Joining", o.DateOfJoining.ToString("dd MMM yyyy"), n.DateOfJoining.ToString("dd MMM yyyy"));
            Add("Designation", o.Designation, n.Designation);
            Add("Salary", o.Salary, n.Salary);
            Add("Address", o.Address, n.Address);
            Add("Active", o.IsActive, n.IsActive);
            Add("DepartmentId", o.DepartmentId, n.DepartmentId);

            return parts.Count == 0 ? "No field changed" : string.Join("; ", parts);
        }
    }
}
