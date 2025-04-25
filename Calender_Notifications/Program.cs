using Calender_Notifications.Data;
using Calender_Notifications.Hubs;
using Calender_Notifications.Services;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// 1) سجل سياسة CORS
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// 2) Add DbContext
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("con")));

// 3) Add Hangfire
builder.Services.AddHangfire(cfg =>
    cfg.UseSqlServerStorage(builder.Configuration.GetConnectionString("con")));
builder.Services.AddHangfireServer();

// 4) Add SignalR
builder.Services.AddSignalR();

// 5) Add NotificationService
builder.Services.AddScoped<INotificationService, NotificationService>();

// 6) Add Controllers & Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

// 7) فعّل CORS قبل UseRouting
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    });
}

// 8) Hangfire Dashboard
app.UseHangfireDashboard("/hangfire");

// 9) Static Files from wwwroot
app.UseStaticFiles();

// 10) Routing & Authorization
app.UseRouting();
app.UseAuthorization();

// 11) Map Endpoints
app.MapControllers();
app.MapHub<NotifyHub>("/notifyhub");

app.Run();
