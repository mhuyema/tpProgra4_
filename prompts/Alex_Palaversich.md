# Historial de prompts — Alex Palaversich

Responsabilidad: Availability.
Herramienta: Claude Code.

## Integración con el message broker (commits `966f640` y `90c1be9`)

1. Necesito traer los últimos cambios de `main` a `Availability`. Indicame los pasos para hacerlo. No hagas push todavía.

2. En Availability necesito comunicarme con Azure Service Bus, reemplazando `PublicadorFalso`.

3. Entiendo, pero ¿por qué accedemos con esto? `config["Broker:ConnectionString"]`. No lo veo en ninguna otra parte del proyecto; además, `GetEnvironmentVariable` ya nos traería la env.

4. Necesito alinear `SolicitudReserva` con lo que Booking espera en el mensaje. Agregá la propiedad `Monto` a la entidad, cargala desde `asiento.Precio` en `SolicitarReserva` y configurala como decimal en `AppDbContext`.

5. Indicame cómo crear la migración `AgregarMontoSolicitud`.

## Autenticación entre microservicios (commit `5c4c0ba`)

6. Tenemos que enviar el token de autenticación en las llamadas a otros microservicios desde Availability. Incluilo como Bearer en las consultas de `IConsultaReservas`.
