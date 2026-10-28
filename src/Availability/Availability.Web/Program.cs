using Availability.Application.CasosDeUso;
using Availability.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<ListarEventos>();
builder.Services.AddScoped<ObtenerEvento>();
builder.Services.AddScoped<ConsultarAsientosLibres>();
builder.Services.AddScoped<SolicitarReserva>();
builder.Services.AddScoped<ConsultarEstadoSolicitud>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Aplica migraciones pendientes y carga datos de prueba (idempotente).
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();


app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
