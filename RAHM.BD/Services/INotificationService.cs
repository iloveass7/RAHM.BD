using RAHM.BD.Models;

namespace RAHM.BD.Services
{
    public interface INotificationService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
        Task<bool> SendEmailAsync(string email, string subject, string message);
        Task<NotificationResult> SendBulkNotificationAsync(NotificationRequest request);
    }

    public class NotificationRequest
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public NotificationChannel Channel { get; set; }
        public string? Division { get; set; }
        public string? District { get; set; }
        public bool SendToAll { get; set; } = false;
    }

    public class NotificationResult
    {
        public int TotalTargeted { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public bool IsSuccess => FailureCount == 0;
    }

    public enum NotificationChannel
    {
        SMS = 1,
        Email = 2,
        Both = 3
    }
}