using System.ComponentModel.DataAnnotations;

namespace Employee_Management_System.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "Employee Code")]
        public string EmployeeCode { get; set; } = "";

        [Required, StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = "";

        [Required, StringLength(15)]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid 10 digit phone number")]
        public string Phone { get; set; } = "";

        [Required]
        public string Gender { get; set; } = "";

        [DataType(DataType.Date)]
        [Display(Name = "Date of Joining")]
        public DateTime DateOfJoining { get; set; } = DateTime.Today;

        [Required, StringLength(100)]
        public string Designation { get; set; } = "";

        [Range(1, 100000000, ErrorMessage = "Enter a valid salary")]
        public decimal Salary { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Range(1, int.MaxValue, ErrorMessage = "Select a department")]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
        public Department? Department { get; set; }

        public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }
}
