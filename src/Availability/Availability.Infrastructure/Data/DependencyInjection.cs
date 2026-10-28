using Availability.Application.Interfaces;
using Availability.Infrastructure.Repositorios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

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
            services.AddScoped<IAsientoRepositorio, AsientoRepositorio>();
            services.AddScoped<ISolicitudReservaRepositorio, SolicitudReservaRepositorio>();

            services.AddScoped<IPublicadorSolicitudes, PublicadorFalso>();
            services.AddScoped<IConsultaReservas, ConsultaReservasFalsa>();

            return services;
        }
    }
}
