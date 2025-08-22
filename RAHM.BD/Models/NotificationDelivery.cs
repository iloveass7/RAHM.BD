namespace RAHM.BD.Models
{
    public class NotificationDelivery
    {
        public int Id { get; set; }
        public int CampaignId { get; set; }
        public int UserId { get; set; }
        public string Channel { get; set; } = "";     // "SMS" | "Email"
        public string ToAddress { get; set; } = "";   // phone (E.164) or email
        public string Status { get; set; } = "";      // "Queued" | "Sent" | "Failed"
    }
}
