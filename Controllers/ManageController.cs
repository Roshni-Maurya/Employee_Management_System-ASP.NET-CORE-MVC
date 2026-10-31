using Employee_Management_System.Models;
using Employee_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Employee_Management_System.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAuditService _audit;

        public ManageController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAuditService audit)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _audit = audit;
        }

        // ---------- Admin: naam aur Gmail badalna ----------
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            return View(new ProfileViewModel { FullName = user.FullName, Email = user.Email ?? "" });
        }

        [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Badlav se pehle current password se confirm
            if (!await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                return View(model);
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null && existing.Id != user.Id)
            {
                ModelState.AddModelError("Email", "This email is already used by another account.");
                return View(model);
            }

            user.FullName = model.FullName.Trim();
            var r1 = await _userManager.SetEmailAsync(user, model.Email);
            var r2 = await _userManager.SetUserNameAsync(user, model.Email);   // login email = username
            var r3 = await _userManager.UpdateAsync(user);

            var errors = r1.Errors.Concat(r2.Errors).Concat(r3.Errors).ToList();
            if (errors.Any())
            {
                foreach (var e in errors) ModelState.AddModelError("", e.Description);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);   // login cookie nayi detail ke saath
            await _audit.LogAsync("Admin Profile Updated", "User", user.Id, $"Name: {user.FullName}, Email: {user.Email}");
            TempData["Success"] = "Profile updated. Use the new email to login next time.";
            return RedirectToAction(nameof(Profile));
        }

        // ---------- Sabke liye: password badalna ----------
        public IActionResult ChangePassword() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            await _audit.LogAsync("Password Changed", "User", user.Id);
            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(ChangePassword));
        }
    }
}
