using Microsoft.AspNetCore.Server.Kestrel.Core;
using Payment.Api.Services;
using Payment.Business.Services;
using Payment.Data.Repositories;
using ProtoBuf.Grpc.Server;

var builder = WebApplication.CreateBuilder(args);

// HTTP/2 explícito también para el perfil HTTP local sin TLS.
builder.WebHost.ConfigureKestrel(options =>
    options.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http2));
builder.Services.AddCodeFirstGrpc();
builder.Services.AddSingleton<IPaymentRepository, PaymentRepository>();
builder.Services.AddSingleton<IPaymentService, PaymentService>();

var app = builder.Build();
app.MapGrpcService<PayGrpcService>();
app.Run();

public partial class Program { }
