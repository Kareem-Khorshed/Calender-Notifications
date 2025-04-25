using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Calender_Notifications.Data;
public class AppDbContext : DbContext
{
    public DbSet<Event> Events => Set<Event>();
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}
