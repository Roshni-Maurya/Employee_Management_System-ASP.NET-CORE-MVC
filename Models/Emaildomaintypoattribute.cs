using System.ComponentModel.DataAnnotations;

namespace Employee_Management_System.Models
{
    // Email ke domain mein aam spelling galti (jaise gmial.com) pakadta hai
    [AttributeUsage(AttributeTargets.Property)]
    public class EmailDomainTypoAttribute : ValidationAttribute
    {
        // galat spelling  ->  sahi spelling   (yahan naye typos add kar sakte ho)
        private static readonly Dictionary<string, string> Typos =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["gmial.com"] = "gmail.com",
                ["gmai.com"] = "gmail.com",
                ["gmil.com"] = "gmail.com",
                ["gamil.com"] = "gmail.com",
                ["gnail.com"] = "gmail.com",
                ["gmal.com"] = "gmail.com",
                ["gmaill.com"] = "gmail.com",
                ["gmail.co"] = "gmail.com",
                ["gmail.con"] = "gmail.com",
                ["gmail.cm"] = "gmail.com",
                ["gmail.om"] = "gmail.com",
                ["yaho.com"] = "yahoo.com",
                ["yahooo.com"] = "yahoo.com",
                ["hotmial.com"] = "hotmail.com",
                ["hotmal.com"] = "hotmail.com",
                ["outlok.com"] = "outlook.com",
                ["outllok.com"] = "outlook.com"
            };

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string email || string.IsNullOrWhiteSpace(email))
                return ValidationResult.Success;

            int at = email.LastIndexOf('@');
            if (at < 0) return ValidationResult.Success;   // format check [EmailAddress] karta hai

            string domain = email[(at + 1)..].Trim();

            if (Typos.TryGetValue(domain, out var correct))
                return new ValidationResult($"Please check the email spelling. Did you mean {email[..at]}@{correct}?");

            return ValidationResult.Success;
        }
    }
}
