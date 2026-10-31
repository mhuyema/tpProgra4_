using Availability.Application.Interfaces;
using Availability.Infrastructure.Repositorios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Availability.Infrastructure.Broker;
using Broker.Messaging;
using Broker.Messaging.DependencyInjection;

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

            // Sin connection string del broker se usa el publicador simulado, para poder levantar el sitio sin Azure.
            var conexionBroker = Environment.GetEnvironmentVariable(BrokerOptions.ConnectionStringEnvVar);


            if (string.IsNullOrWhiteSpace(conexionBroker)) 
            {
                services.AddScoped<IPublicadorSolicitudes, PublicadorFalso>();
            }
            else
            {
                services.AddBroker(config);
                services.AddBrokerPublisher();
                services.AddScoped<IPublicadorSolicitudes, PublicadorServiceBus>();
            }

            services.AddScoped<IAsientoRepositorio, AsientoRepositorio>();
            services.AddScoped<ISolicitudReservaRepositorio, SolicitudReservaRepositorio>();

            services.AddScoped<IConsultaReservas, ConsultaReservasFalsa>();

            return services;
        }
    }
}
