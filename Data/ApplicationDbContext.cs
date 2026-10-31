using Employee_Management_System.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Employee_Management_System.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Employee>(e =>
            {
                e.Property(x => x.Salary).HasColumnType("decimal(18,2)");
                e.HasIndex(x => x.EmployeeCode).IsUnique();
                e.HasIndex(x => x.Email).IsUnique();
                e.HasOne(x => x.Department)
                 .WithMany(d => d.Employees)
                 .HasForeignKey(x => x.DepartmentId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<LeaveRequest>()
                .HasOne(l => l.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AuditLog>().HasIndex(a => a.Timestamp);

            builder.Entity<Department>().HasData(
                new Department { Id = 1, Name = "HR", Description = "Human Resources" },
                new Department { Id = 2, Name = "IT", Description = "Information Technology" },
                new Department { Id = 3, Name = "Finance", Description = "Accounts & Finance" },
                new Department { Id = 4, Name = "Sales", Description = "Sales & Marketing" }
            );
        }
    }
}