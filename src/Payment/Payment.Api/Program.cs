using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Payment.Api.Auditing;
using Payment.Api.Auth;
using Payment.Api.Services;
using Payment.Data;
using Payment.Business.Services;
using Payment.Business.Auditing;
using Payment.Data.Repositories;
using ProtoBuf.Grpc.Server;
using Broker.Messaging.DependencyInjection;


var builder = WebApplication.CreateBuilder(args);

// HTTP/2 explícito también para el perfil HTTP local sin TLS.
builder.WebHost.ConfigureKestrel(options =>
    options.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http2));
// Autenticación servicio a servicio: cada llamada gRPC debe traer el token de servicio de Booking, que se valida
// de forma delegada contra el Authorization Server (GET /api/auth/validate).
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IValidadorTokenServicio, ValidadorTokenServicioHttp>(client =>
{
    var authority = builder.Configuration["Auth:Authority"]
        ?? throw new InvalidOperationException("No se configuró Auth:Authority (URL del Authorization Server).");

    client.BaseAddress = new Uri(authority.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddCodeFirstGrpc(options => options.Interceptors.Add<AutenticacionServicioInterceptor>());
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentAuditPublisher, PaymentAuditPublisher>();

builder.Services.AddBroker(builder.Configuration);
builder.Services.AddBrokerPublisher();



builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DbConexion")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    await context.Database.MigrateAsync();
}

app.MapGrpcService<PayGrpcService>();
app.Run();

public partial class Program { }
