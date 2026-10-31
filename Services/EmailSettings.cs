namespace Employee_Management_System.Services
{
    public class EmailSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string SenderName { get; set; } = "HR Department";
        public string SenderEmail { get; set; } = "";   // User Secrets se aata hai
        public string AppPassword { get; set; } = "";   // User Secrets se aata hai
    }
}
