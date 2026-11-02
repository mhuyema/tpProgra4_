using AuditLog.Api.Data;
using AuditLog.Api.Handlers;
using Broker.Messaging.Contracts;
using Broker.Messaging.DependencyInjection;
using LiteDB;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "AuditLog.Session";
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(4);
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<ILiteDatabase>(_ =>
{
    var connectionString = builder.Configuration.GetConnectionString("LiteDb")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:LiteDb.");
    var connection = new ConnectionString(connectionString);
    if (connection.Filename is not ":memory:" and not ":temp:")
    {
        connection.Filename = Path.GetFullPath(connection.Filename, builder.Environment.ContentRootPath);
        Directory.CreateDirectory(Path.GetDirectoryName(connection.Filename)!);
    }
    return new LiteDatabase(connection) { UtcDate = true };
});
builder.Services.AddSingleton<AuditLogRepository>();
builder.Services.AddScoped<AuditLogMessageHandler>();
builder.Services.AddBroker(builder.Configuration);
builder.Services.AddBrokerSubscriber<AuditLogMessage, AuditLogMessageHandler>(
    builder.Configuration["AuditLog:SubscriptionName"] ?? "auditlog");

var app = builder.Build();
// Abre la base y crea los índices antes de comenzar a consumir mensajes.
app.Services.GetRequiredService<AuditLogRepository>();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
