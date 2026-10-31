using System.ComponentModel.DataAnnotations;

namespace Employee_Management_System.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [Required, StringLength(150)]
        public string UserEmail { get; set; } = "";

        [Required, StringLength(100)]
        public string Action { get; set; } = "";

        [Required, StringLength(50)]
        public string Entity { get; set; } = "";

        [StringLength(50)]
        public string? EntityId { get; set; }

        [StringLength(1000)]
        public string? Details { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }
    }
}
