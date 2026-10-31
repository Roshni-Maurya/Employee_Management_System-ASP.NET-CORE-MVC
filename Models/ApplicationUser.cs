using Microsoft.AspNetCore.Identity;

namespace Employee_Management_System.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = "";
    }
}
