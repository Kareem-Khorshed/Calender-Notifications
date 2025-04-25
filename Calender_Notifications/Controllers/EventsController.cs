using Microsoft.EntityFrameworkCore;
using Calender_Notifications.Data;
using Calender_Notifications.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace Calender_Notifications.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IBackgroundJobClient _jobs;
        private readonly INotificationService _notificationService;

        public EventsController(
            AppDbContext db,
            IBackgroundJobClient jobs,
            INotificationService notificationService)
        {
            _db = db;
            _jobs = jobs;
            _notificationService = notificationService;
        }

        // GET api/events
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var events = await _db.Events
                .Select(e => new
                {
                    e.Id,
                    title = e.Title,
                    start = e.StartUtc,
                    notifyBefore = e.NotifyMinutesBefore
                })
                .ToListAsync();

            return Ok(events);
        }

        // POST api/events/trigger/{id}
        [HttpPost("trigger/{id}")]
        public IActionResult Trigger([FromRoute] Guid id)
        {
            _jobs.Enqueue<INotificationService>(svc => svc.SendEventReminder(id));
            return Ok(new { triggered = true, eventId = id });
        }

        // POST api/events/immediate/{id}
        [HttpPost("immediate/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Immediate([FromRoute] Guid id)
        {
            await _notificationService.SendEventReminder(id);
            return Ok(new { success = true, eventId = id });
        }

        // POST api/events
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEventDto dto)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(dto.TimeZoneId);
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(dto.StartLocal, tz);

            var ev = new Event
            {
                UserId = User.Identity?.Name ?? "anonymous",
                Title = dto.Title,
                Description = dto.Description,
                StartUtc = startUtc,
                NotifyMinutesBefore = dto.NotifyBefore
            };
            _db.Events.Add(ev);
            await _db.SaveChangesAsync();

            var jobId = _jobs.Schedule<INotificationService>(
                svc => svc.SendEventReminder(ev.Id),
                ev.StartUtc.AddMinutes(-ev.NotifyMinutesBefore));

            ev.HangfireJobId = jobId;
            await _db.SaveChangesAsync();

            return Ok(new { ev.Id, ev.StartUtc });
        }
    }

    public record CreateEventDto(
        string Title,
        string? Description,
        DateTime StartLocal,
        int NotifyBefore,
        string TimeZoneId
    );
}
