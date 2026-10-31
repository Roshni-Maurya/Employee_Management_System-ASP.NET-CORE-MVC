using System.Net;
using Employee_Management_System.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Employee_Management_System.Services
{
    public interface IEmailService
    {
        Task<bool> SendLeaveDecisionAsync(LeaveRequest leave, Employee employee);
        Task<bool> SendPasswordResetAsync(string toEmail, string toName, string resetLink);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> SendLeaveDecisionAsync(LeaveRequest leave, Employee employee)
        {
            bool approved = leave.Status == "Approved";

            // HtmlEncode: naam/remark mein koi HTML ya script ho to email mein chal na paaye
            string name = WebUtility.HtmlEncode(employee.FullName);
            string type = WebUtility.HtmlEncode(leave.LeaveType);
            string remark = string.IsNullOrWhiteSpace(leave.AdminRemark)
                ? ""
                : $"<p><b>Remark from HR:</b> {WebUtility.HtmlEncode(leave.AdminRemark)}</p>";

            string statusText = approved ? "Approved" : "Rejected";
            string color = approved ? "#198754" : "#dc3545";

            string closing = approved
                ? "Kindly ensure that your pending work is handed over before your leave begins. We wish you a pleasant and relaxing break."
                : "We understand this may be disappointing. You are welcome to speak with HR or your manager, or to submit a new request for alternate dates.";

            string html = $@"
<div style='font-family:Segoe UI,Arial,sans-serif;font-size:15px;color:#212529;max-width:600px'>
  <p>Dear <b>{name}</b>,</p>
  <p>We hope this message finds you well.</p>
  <p>We would like to inform you that your <b>{type}</b> leave request from
     <b>{leave.FromDate:dd MMM yyyy}</b> to <b>{leave.ToDate:dd MMM yyyy}</b>
     ({leave.Days} day(s)) has been
     <span style='color:{color};font-weight:bold'>{statusText}</span>.</p>
  {remark}
  <p>{closing}</p>
  <p>Warm regards,<br/><b>{WebUtility.HtmlEncode(_settings.SenderName)}</b><br/>Employee Management System</p>
  <hr/><small style='color:#6c757d'>This is an automated message. Please do not share your account details with anyone.</small>
</div>";

            return await SendAsync(employee.FullName, employee.Email,
                $"Leave Request {statusText} - {leave.LeaveType}", html);
        }

        public async Task<bool> SendPasswordResetAsync(string toEmail, string toName, string resetLink)
        {
            string name = WebUtility.HtmlEncode(toName);
            string link = WebUtility.HtmlEncode(resetLink);

            string html = $@"
<div style='font-family:Segoe UI,Arial,sans-serif;font-size:15px;color:#212529;max-width:600px'>
  <p>Dear <b>{name}</b>,</p>
  <p>We received a request to reset your password for the Employee Management System.</p>
  <p><a href='{link}' style='background:#0d6efd;color:#fff;padding:10px 18px;text-decoration:none;border-radius:6px'>Reset Password</a></p>
  <p>This link is valid for <b>1 hour</b> and can be used only once.</p>
  <p>If you did not request this, please ignore this email. Your password will not change.</p>
  <p>Warm regards,<br/><b>{WebUtility.HtmlEncode(_settings.SenderName)}</b></p>
</div>";

            return await SendAsync(toName, toEmail, "Reset your password", html);
        }

        private async Task<bool> SendAsync(string toName, string toEmail, string subject, string html)
        {
            if (string.IsNullOrWhiteSpace(_settings.SenderEmail) || string.IsNullOrWhiteSpace(_settings.AppPassword))
            {
                _logger.LogWarning("Email settings missing (User Secrets). Email not sent.");
                return false;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = html }.ToMessageBody();

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_settings.SenderEmail, _settings.AppPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                return true;
            }
            catch (Exception ex)
            {
                // Email fail ho to app crash nahi hona chahiye
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                return false;
            }
        }
    }
}
