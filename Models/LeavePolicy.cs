namespace Employee_Management_System.Models
{
    // Saal bhar mein kitni leaves milti hain. Yahan badlo to poore project mein badal jayega.
    public static class LeavePolicy
    {
        public static readonly Dictionary<string, int> YearlyQuota = new()
        {
            ["Casual"] = 12,
            ["Sick"] = 6,
            ["Paid"] = 15
        };

        // Unpaid par koi limit nahi
        public static readonly string[] Types = { "Casual", "Sick", "Paid", "Unpaid" };
    }
}
