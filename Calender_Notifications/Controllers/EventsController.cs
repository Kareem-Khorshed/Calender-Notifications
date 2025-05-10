using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Calender_Notifications.Data;
using Calender_Notifications.Services;

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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var events = await _db.Events
                .Select(e => new { e.Id, e.Title, e.StartUtc, e.NotifyMinutesBefore })
                .ToListAsync();

            return Ok(events);
        }

        [HttpPost("trigger/{id}")]
        public IActionResult Trigger([FromRoute] Guid id)
        {
            _jobs.Enqueue<INotificationService>(svc => svc.SendEventReminder(id));
            return Ok(new { triggered = true, id });
        }

        [HttpPost("immediate/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Immediate([FromRoute] Guid id)
        {
            await _notificationService.SendEventReminder(id);
            return Ok(new { success = true, id });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEventDto dto)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(dto.TimeZoneId);
            var unspecifiedLocal = DateTime.SpecifyKind(dto.StartLocal, DateTimeKind.Unspecified);
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(unspecifiedLocal, tz);


            var ev = new Event
            {
                UserId = User.Identity?.Name ?? string.Empty,
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

            return Ok(new { ev.Id, startUtc });
        }

        [HttpPost("reactivate/{id}")]
        public async Task<IActionResult> Reactivate(Guid id)
        {
            var ev = await _db.Events.FindAsync(id);
            if (ev == null) return NotFound();

            ev.IsNotified = false;
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateEventDto dto)
        {
            var ev = await _db.Events.FindAsync(id);
            if (ev == null) return NotFound();

            var tz = TimeZoneInfo.FindSystemTimeZoneById(dto.TimeZoneId);
            var unspecifiedLocal = DateTime.SpecifyKind(dto.StartLocal, DateTimeKind.Unspecified);
            var newStartUtc = TimeZoneInfo.ConvertTimeToUtc(unspecifiedLocal, tz);



            ev.Title = dto.Title;
            ev.Description = dto.Description;
            ev.StartUtc = newStartUtc;
            ev.NotifyMinutesBefore = dto.NotifyBefore;
            ev.IsNotified = false;

            if (!string.IsNullOrEmpty(ev.HangfireJobId))
            {
                BackgroundJob.Delete(ev.HangfireJobId);
            }

            var newJobId = _jobs.Schedule<INotificationService>(
                svc => svc.SendEventReminder(ev.Id),
                newStartUtc.AddMinutes(-dto.NotifyBefore));

            ev.HangfireJobId = newJobId;

            await _db.SaveChangesAsync();

            return Ok(new { updated = true, ev.Id });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ev = await _db.Events.FindAsync(id);
            if (ev == null) return NotFound();

            if (!string.IsNullOrEmpty(ev.HangfireJobId))
            {
                BackgroundJob.Delete(ev.HangfireJobId);
            }

            _db.Events.Remove(ev);
            await _db.SaveChangesAsync();

            return Ok(new { deleted = true, ev.Id });
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
