using System.ComponentModel.DataAnnotations;

namespace Employee_Management_System.Models
{
    public class Department
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Department Name")]
        public string Name { get; set; } = "";

        [StringLength(250)]
        public string? Description { get; set; }

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
