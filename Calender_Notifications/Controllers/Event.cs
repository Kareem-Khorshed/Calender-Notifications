
public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime StartUtc { get; set; }
    public int NotifyMinutesBefore { get; set; }
    public bool IsNotified { get; set; } = false;
    public string? HangfireJobId { get; set; }   // نخزن Job ID لو حابب تلغيه
}
