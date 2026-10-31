using Employee_Management_System.Data;
using Employee_Management_System.Models;

namespace Employee_Management_System.Services
{
    public interface IAuditService
    {
        Task LogAsync(string action, string entity, string? entityId = null,
                      string? details = null, string? userEmail = null);
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _http;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor http)
        {
            _context = context;
            _http = http;
        }

        public async Task LogAsync(string action, string entity, string? entityId = null,
                                   string? details = null, string? userEmail = null)
        {
            var ctx = _http.HttpContext;

            if (details != null && details.Length > 1000)
                details = details[..1000];

            _context.AuditLogs.Add(new AuditLog
            {
                Timestamp = DateTime.Now,
                UserEmail = userEmail ?? ctx?.User?.Identity?.Name ?? "anonymous",
                Action = action,
                Entity = entity,
                EntityId = entityId,
                Details = details,
                IpAddress = ctx?.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();
        }
    }
}
