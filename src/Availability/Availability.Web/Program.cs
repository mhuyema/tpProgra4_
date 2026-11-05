using Availability.Application.CasosDeUso;
using Availability.Application.Interfaces;
using Availability.Application.Servicios;
using Availability.Infrastructure.Data;
using Availability.Infrastructure.Servicios;
using Availability.Web;
using Broker.Messaging.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Sesión por cookie (no persistente: se cierra con el navegador) y Session para guardar la reserva pendiente.
// El login se delega al Authorization Server (OIDC authorization code + PKCE); la sesión local sigue siendo una cookie.
builder.Services.AddAuthentication(o =>
    {
        o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(o =>
    {
        var auth = builder.Configuration.GetSection("Auth");
        o.Authority = auth["Authority"];
        o.ClientId = auth["ClientId"];
        o.ResponseType = OpenIdConnectResponseType.Code;
        o.UsePkce = true;
        o.SaveTokens = true;
        o.MapInboundClaims = false;
        o.GetClaimsFromUserInfoEndpoint = false;
        o.CallbackPath = "/signin-oidc";
        o.Scope.Clear();
        o.Scope.Add("openid");
        o.Scope.Add("email");
        o.Scope.Add("profile");
        o.TokenValidationParameters.NameClaimType = "name";
    });
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

builder.Services.AddInfrastructure(builder.Configuration);

// Mismo cliente tipado que registra Infrastructure (comparten nombre): se le suma el Bearer del usuario.
// El handler vive en Web porque lee el token del HttpContext; Infrastructure no conoce ASP.NET.
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<TokenUsuarioHandler>();
builder.Services.AddHttpClient<IConsultaReservas, BookingService>()
    .AddHttpMessageHandler<TokenUsuarioHandler>();

// Lo mismo para Notificaciones: Notifications solo entrega los avisos a su dueño, asi que cada pedido tiene
// que llevar el token del usuario logueado (Notifications lo valida contra el Authorization Server).
builder.Services.AddHttpClient<IConsultaNotificaciones, NotificacionesService>()
    .AddHttpMessageHandler<TokenUsuarioHandler>();

builder.Services.AddBroker(builder.Configuration);
builder.Services.AddBrokerPublisher();

builder.Services.AddScoped<ListarEventos>();
builder.Services.AddScoped<ObtenerEvento>();
builder.Services.AddScoped<ConsultarAsientosLibres>();
builder.Services.AddScoped<ArmadorMapaAsientos>();
builder.Services.AddScoped<SolicitarReserva>();
builder.Services.AddScoped<ConsultarEstadoSolicitud>();
builder.Services.AddScoped<ConsultarNotificaciones>();

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

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
