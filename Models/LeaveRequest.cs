using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Employee_Management_System.Models
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        [Required]
        [Display(Name = "Leave Type")]
        public string LeaveType { get; set; } = "Casual";

        [DataType(DataType.Date)]
        [Display(Name = "From")]
        public DateTime FromDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "To")]
        public DateTime ToDate { get; set; } = DateTime.Today;

        [Required, StringLength(500)]
        public string Reason { get; set; } = "";

        public string Status { get; set; } = "Pending";   // Pending / Approved / Rejected
        public DateTime AppliedOn { get; set; } = DateTime.Now;
        public string? AdminRemark { get; set; }

        [NotMapped]
        public int Days => (ToDate.Date - FromDate.Date).Days + 1;
    }
}
