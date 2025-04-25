namespace Calender_Notifications.Services
{
    public interface INotificationService
    {
        Task SendEventReminder(Guid eventId);
    }
}
