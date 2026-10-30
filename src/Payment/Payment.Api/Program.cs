using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Payment.Api.Services;
using Payment.Data;
using Payment.Business.Services;
using Payment.Data.Repositories;
using ProtoBuf.Grpc.Server;

var builder = WebApplication.CreateBuilder(args);

// HTTP/2 explícito también para el perfil HTTP local sin TLS.
builder.WebHost.ConfigureKestrel(options =>
    options.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http2));
builder.Services.AddCodeFirstGrpc();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

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
