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
            Console.WriteLine($"[Reminder] SendEventReminder called for {eventId} at {DateTime.UtcNow}");

            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null)
            {
                Console.WriteLine("[Reminder] Event not found!");
                return;
            }
            if (ev.IsNotified)
            {
                Console.WriteLine($"[Reminder] Event {eventId} already notified.");
                return;
            }

            Console.WriteLine($"[Reminder] Found event '{ev.Title}', scheduling notification...");

            // Broadcast to all connected clients
            await _hub.Clients.All.SendAsync("ReceiveReminder", new
            {
                title = ev.Title,
                startUtc = ev.StartUtc
            });
            Console.WriteLine("[Reminder] Sent ReceiveReminder to all clients");

            // Mark as notified
            ev.IsNotified = true;
            await _db.SaveChangesAsync();
            Console.WriteLine("[Reminder] Event marked as notified in database");
        }
    }
}
