using Calender_Notifications.Data;
using Calender_Notifications.Hubs;
using Calender_Notifications.Services;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// 1) Configure CORS
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod()));

// 2) Register DbContext with SQL Server
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3) Configure Hangfire (storage + server)
builder.Services.AddHangfire(cfg =>
    cfg.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

// 4) Add SignalR for real-time notifications
builder.Services.AddSignalR();

// 5) Register our notification service
builder.Services.AddScoped<INotificationService, NotificationService>();

// 6) Add Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 7) Enable Developer Exception Page and Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CalendarNotify API V1");
        c.RoutePrefix = "swagger"; // Serve at /swagger
    });
}

// 8) Enforce HTTPS and HSTS
app.UseHttpsRedirection();
app.UseHsts();

// 9) Enable CORS) Enable CORS
app.UseCors("AllowAll");

// 9) Serve static files from wwwroot
app.UseStaticFiles();

// 10) Routing
app.UseRouting();

// 11) Hangfire Dashboard at /hangfire
app.UseHangfireDashboard("/hangfire");

// 12) Authorization (if needed)
app.UseAuthorization();

// 13) Map endpoints: Controllers and SignalR hub
app.MapControllers();
app.MapHub<NotifyHub>("/notifyhub");

app.Run();
