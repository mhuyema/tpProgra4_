# Notifications

Microservicio de Notificaciones (punto 3.h). Consume los avisos que publica Booking en el topic y le informa al usuario.

| Aviso | Contrato (Broker.Messaging) | Suscripción |
|---|---|---|
| Reserva creada (Retenida) | `ReservationCreatedMessage` | `notificaciones-creada` |
| Reserva confirmada (pago exitoso) | `ReservationConfirmedMessage` | `notificaciones-confirmada` |

## Para correrlo

1. Variable de entorno `SERVICEBUS_CONNECTION_STRING` con la cadena del namespace.
2. En el topic (por defecto `grupo9-booking-topic`) tienen que existir las dos suscripciones de la tabla, con el nombre exacto.
3. Puertos: `http://localhost:5310` / `https://localhost:7310`.
4. `GET /api/notificaciones/{userId}` devuelve los avisos enviados a un usuario.

## Notas de diseño

- Un solo proyecto: no tiene dominio propio, solo traduce un evento en un aviso.
- Una suscripción por tipo de aviso: el suscriptor compartido ignora los mensajes cuyo `Subject` no es el suyo.
- El envío está simulado (log de auditoría); el canal real (email, SMS) se reemplaza implementando `INotificationSender`.
- Idempotente por (`RequestId`, tipo): una re-entrega del broker no avisa dos veces. El registro es en memoria.
