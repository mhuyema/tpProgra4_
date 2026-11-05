using Availability.Application.Interfaces;
using Availability.Infrastructure.Broker;
using Availability.Infrastructure.Repositorios;
using Availability.Infrastructure.Servicios;
using Broker.Messaging;
using Broker.Messaging.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Availability.Infrastructure.Data
{
    /// <summary>
    /// Registra las implementaciones de Infrastructure para las interfaces de Application.
    /// Es el único lugar donde se decide qué tecnología cumple cada contrato.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<AppDbContext>(o =>
                o.UseSqlServer(config.GetConnectionString("DbConexion")));

            services.AddScoped<IEventoRepositorio, EventoRepositorio>();

            var conexionBroker = config[BrokerOptions.ConnectionStringEnvVar];

            if (string.IsNullOrWhiteSpace(conexionBroker)) 
            {
                services.AddScoped<IPublicadorSolicitudes, Publicador>();
                services.AddScoped<IAuditoriaPublicador, AuditoriaPublicadorNulo>();
            }
            else
            {
                services.AddBroker(config);
                // AddBroker lee Broker:ConnectionString o la variable de entorno real; así también acepta la misma clave de user-secrets.
                services.PostConfigure<BrokerOptions>(o => o.ConnectionString ??= conexionBroker);
                services.AddBrokerPublisher();
                services.AddScoped<IPublicadorSolicitudes, PublicadorServiceBus>();
                services.AddScoped<IAuditoriaPublicador, AuditoriaPublicadorServiceBus>();
            }

            services.AddScoped<IAsientoRepositorio, AsientoRepositorio>();
            services.AddScoped<ISolicitudReservaRepositorio, SolicitudReservaRepositorio>();
            services.AddHttpClient<IConsultaReservas, BookingService>(client =>
            {
                client.BaseAddress = new Uri(config["Booking:BaseUrl"] ?? "http://localhost:5295/api/reservas/");
            });

            return services;
        }
    }
}
