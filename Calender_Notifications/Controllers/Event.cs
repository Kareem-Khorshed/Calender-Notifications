namespace Calender_Notifications.Data
{
    public class Event
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartUtc { get; set; }
        public int NotifyMinutesBefore { get; set; }
        public bool IsNotified { get; set; }
        public string? HangfireJobId { get; set; }
    }
}
