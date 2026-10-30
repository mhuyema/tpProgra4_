using Booking.Data;
using Microsoft.EntityFrameworkCore;
using Booking.Business.Services;
using Booking.Data.Repositories;
using Payment.Contracts;
using ProtoBuf.Grpc.ClientFactory;
using Booking.Business.Payments;
using Booking.API.Integrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConexion")));
builder.Services.AddScoped<IReservaService, ReservaService>();
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();

// Registra el cliente gRPC de Payment utilizando la dirección
// configurada para el entorno de ejecución.
builder.Services.AddCodeFirstGrpcClient<IPaymentGrpcService>(options =>
{
    var paymentUrl = builder.Configuration["Payment:GrpcUrl"]
        ?? throw new InvalidOperationException(
            "No se configuró la dirección gRPC de Payment.");

    options.Address = new Uri(paymentUrl);
});

// Vincula el contrato interno de pagos con su implementación gRPC,
// manteniendo desacoplada la capa Business de la comunicación externa.
builder.Services.AddScoped<IPagoGateway, PagoGrpcGateway>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();