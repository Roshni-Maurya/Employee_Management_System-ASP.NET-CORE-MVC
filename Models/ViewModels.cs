using System.ComponentModel.DataAnnotations;

namespace Employee_Management_System.Models
{
    public class RegisterViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required, Compare("Password"), DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = "";
    }

    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int TotalDepartments { get; set; }
        public int PendingLeaves { get; set; }
        public Dictionary<string, int> EmployeesByDepartment { get; set; } = new();
        public List<Employee> RecentEmployees { get; set; } = new();
    }
}

namespace Employee_Management_System.Models
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";
    }

    public class ResetPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Token { get; set; } = "";

        [Required, DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string Password { get; set; } = "";

        [Required, Compare("Password", ErrorMessage = "Passwords do not match."), DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = "";
    }

    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = "";

        [Required, DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = "";

        [Required, Compare("NewPassword", ErrorMessage = "Passwords do not match."), DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = "";
    }

    public class ProfileViewModel
    {
        [Required, StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [Required, EmailAddress, StringLength(150)]
        [Display(Name = "Email (Gmail)")]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password)]
        [Display(Name = "Current Password (to confirm)")]
        public string CurrentPassword { get; set; } = "";
    }
}
