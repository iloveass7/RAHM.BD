namespace RAHM.BD.Models
{
    // Admin-defined message fan-out (area targeting via Division/District)
    public class NotificationCampaign
    {
        public int Id { get; set; }
        public int CreatedByAdminId { get; set; }
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public string Channel { get; set; } = ""; // "SMS" | "Email" | "Both"
        public string? Division { get; set; }
        public string? District { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public string Status { get; set; } = ""; // "Draft" | "Scheduled" | "Completed" | "Cancelled"
    }
}
