using Calender_Notifications.Data;
using Calender_Notifications.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Calender_Notifications.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<NotifyHub> _hub;

        public NotificationService(AppDbContext db, IHubContext<NotifyHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        public async Task SendEventReminder(Guid eventId)
        {
            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null || ev.IsNotified)
            {
                return;
            }

            // Send notification to all connected clients
            await _hub.Clients.All.SendAsync("ReceiveReminder", new
            {
                title = ev.Title,
                startUtc = ev.StartUtc
            });

            // Mark event as notified
            ev.IsNotified = true;
            await _db.SaveChangesAsync();
        }
    }
}
