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

            // Autenticación servicio a servicio (punto 3.e): para los pedidos sin usuario, Availability le pide un
            // token al Authorization Server con su propio client_id/client_secret y se lo manda a Booking.
            //  - "AuthorizationServer": HttpClient que apunta al Authorization Server (para pedir el token).
            //  - ProveedorTokenServicio: singleton, guarda el token hasta que está por vencer.
            //  - TokenServicioHandler: transient (los handlers de HttpClient deben serlo), agrega el token al pedido.
            services.AddHttpClient(ProveedorTokenServicio.NombreCliente, client =>
            {
                var authority = config["Auth:Authority"] ?? "https://localhost:7000";
                client.BaseAddress = new Uri(authority.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            services.AddSingleton<IProveedorTokenServicio, ProveedorTokenServicio>();
            services.AddTransient<TokenServicioHandler>();

            services.AddHttpClient<IConsultaReservas, BookingService>(client =>
            {
                client.BaseAddress = new Uri(config["Booking:BaseUrl"] ?? "http://localhost:5295/api/reservas/");
            })
            .AddHttpMessageHandler<TokenServicioHandler>();

            // Cliente HTTP del servicio de Notificaciones (puerto 5310 en local). Mismo patrón que Booking:
            // acá se fija la URL base; Availability.Web le suma el Bearer del usuario. La URL tiene que
            // terminar en "/" para que .NET le pegue bien el id del usuario al final.
            services.AddHttpClient<IConsultaNotificaciones, NotificacionesService>(client =>
            {
                client.BaseAddress = new Uri(config["Notifications:BaseUrl"] ?? "http://localhost:5310/api/notificaciones/");
            });

            return services;
        }
    }
}