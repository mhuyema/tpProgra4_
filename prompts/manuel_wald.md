# manuel_wald.md — Historial de prompts (Manuel Wald)

Archivo de memoria del punto 2 del TP de Programación IV: historial de prompts usados con el modelo de lenguaje para implementar mi componente. Se entrega junto al repositorio.

- **Integrante:** Manuel Wald (wald16)
- **Grupo:** N° 9
- **Componente a cargo:** Broker de mensajería
- **Modelo/herramienta:** Claude Code (Claude Sonnet 5.5)

## Contexto del trabajo práctico

Sistema de reserva de asientos para eventos, con estilo arquitectónico de microservicios.

| Componente | Arquitectura | Rol |
|---|---|---|
| Availability (sitio) | Clean Architecture, dominio anémico | Consulta de asientos, recibe la solicitud de reserva, la publica en el broker y responde "recibida" sin esperar el resultado |
| Booking (REST API) | N-Layer, dominio anémico | Consume el mensaje, crea la reserva retenida, llama a Payment por gRPC y confirma o rechaza. Maneja el vencimiento |
| Payment (gRPC API) | N-Layer, dominio anémico | Cobra la reserva retenida |
| Message Broker | — | Desacopla Availability de Booking (PUBLISH / SUBSCRIBE) |
| Authorization Server | — | Autentica a los usuarios y a los microservicios entre sí |
| Notificaciones | — | Avisa al usuario cuando la reserva queda confirmada (punto 3.h) |

Estados de una reserva: Retenida, Confirmada, Rechazada (asiento ocupado, pago fallido o pago no disponible) y Vencida.

## Mi parte: Broker para mensajería

Implementa la comunicación asincrónica entre Availability (publica) y Booking (se suscribe), y es la base de la eventual consistencia del sistema (punto 3.g). Debe cumplir:

- Availability publica la solicitud con un identificador único y responde de inmediato que fue recibida.
- Una de las réplicas de Booking toma el mensaje.
- Logs de auditoría del inicio y fin de la cadena de sucesos (3.f).
- Justificar en comentarios del código las decisiones y patrones usados (punto 3).
- Debe correr en la solución final, con un escenario de happy path y uno de fallo si falta un componente (3.j).

### Material de partida de la cátedra

`mensajeria/` (EjemploMensajeria.zip): app de consola .NET 10 con `Azure.Messaging.ServiceBus`. Publica un mensaje a un topic (`grupoN-booking-topic`) y lo lee desde una suscripción (`booking`) con `ServiceBusReceiver`, confirmándolo con `CompleteMessageAsync`.

## Historial de prompts

Se registran textuales, en orden cronológico.

### Sesión 1 — 2026-10-26

1. > bien. empeza el manuel_wald.md (contract). tiene que incluír todos mis prompts, los utiliza luego el profesor para evaluar lo realizado. analiza y contextualizate acerca del trabajo práctico (/home/wald16/Facultad/programacion4/Trabajo_practico.pdf). A mi me tocó la parte del Broker para mensajería.

   Resultado: se creó este archivo con el contexto del TP, mi parte y el historial.

2. > mi número de equipo es 9, y el connection string lo tendré más adelante. lo voy a colocar yo como variable de entorno secreta

   Resultado: se completó el grupo N° 9 (topic `grupo9-booking-topic`). El connection string no va en el repo ni en el código: se lee de una variable de entorno.

3. > sí, usá SERVICEBUS_CONNECTION_STRING. registra todos los prompts como te fue indicado

   Resultado: la variable de entorno se llama `SERVICEBUS_CONNECTION_STRING`. Desde acá se registran todos los prompts, textuales y en orden.

### Sesión 2 — 2026-10-28

4. > arrancá con la librería del broker. vamos a trabajar sobre el repo ya clonado:
   > /home/wald16/Facultad/programacion4/tpP

   Resultado: interrumpido por el usuario. Solo se inspeccionó el repo (`tpProgra4_`).

5. > [Request interrupted by user]
   > arrancá con la librería del broker. vamos a trabajar sobre el repo ya clonado:
   > /home/wald16/Facultad/programacion4/tpP
   > Vamos a trabajar sobre una rama broker

   Resultado: se creó la rama `broker` en el repo clonado (`tpProgra4_`, la ruta indicada `tpP` no existe como tal) y se implementó la librería `Broker.Messaging` (ver commit en la rama).

6. > bien, ya arregle que el contrato entre availability y booking es el que hicimos:
   >
   > RequestId: Guid
   >
   > EventId: int
   >
   > SeatId: int
   >
   > UserId: string
   >
   > cambio los tipos y commiteo.

   Resultado: `ReservationRequestedMessage` pasó a `Guid RequestId, int EventId, int SeatId, string UserId`. La solución compila y se commiteó en la rama `broker`.

7. > reformulé los commits, a partir de ahora seguí el formato que usé

   Resultado: se revisó el historial reescrito. Formato de commits: `feat: <descripción en español, minúsculas>`, una sola línea, sin cuerpo ni trailers.

8. > bien, actualizo estado:
   > ya tenemos el connection string, lo dejé en un .env local. quería ver la manera de dejarlo como secret de github, para que mis compañeros puedan usarlo.
   > tambien me gustaría armar un compose, para levantar todos los microservicios juntos de manera eficiente

   Resultado: en la rama `compose` se agregaron `Dockerfile` (único, parametrizado por servicio), `docker-compose.yml` (SQL Server + payment + booking + availability), `.dockerignore` y `.env.example`; `.env` quedó en `.gitignore`. Se movieron los archivos de `Booking.API/` a `Booking.Api/` (en Linux eran carpetas distintas y Booking no compilaba). Se explicó que un secret de GitHub no sirve para uso local de los compañeros.

9. > Changes to be committed:
   > (use "git restore --staged <file>..." to unstage)
   > renamed: src/Booking/Booking.API/Controllers/ReservasController.cs -> src/Booking/Booking.Api/Controllers/ReservasController.cs
   > renamed: src/Booking/Booking.API/DTOs/EstadoSolicitudResponse.cs -> src/Booking/Booking.Api/DTOs/EstadoSolicitudResponse.cs
   > renamed: src/Booking/Booking.API/Integrations/PagoGrpcGateway.cs -> src/Booking/Booking.Api/Integrations/PagoGrpcGateway.cs
   > renamed: src/Booking/Booking.API/Workers/PagosPendientesWorker.cs -> src/Booking/Booking.Api/Workers/PagosPendientesWorker.cs
   >
   > esto malisimo, no creo q les guste a mis compañeros

   Resultado: se revirtió el movimiento de archivos de Booking. La unificación de `Booking.API` y `Booking.Api` pasó a hacerse solo dentro del `Dockerfile`, sin tocar el repo.

10. > de esta manera (considerando que yo envío a todos mis compañeros la variable para su .env), todos podemos levantar el proyecto?

    Resultado: se confirmó que sí, con condiciones (SA_PASSWORD propio, docker, suscripción compartida). Se reforzó el `Dockerfile` para que también funcione si el clon (p. ej. en Windows) trae solo la carpeta `Booking.API`.

11. > mi pregunta es ala siguiente:
    > la idea no es que todos usemos la misma suscripcion que nos mando el profe? como crearias una por persona?

    Resultado: se aclaró que la suscripción compartida es el diseño pedido (competing consumers) y que una por persona sería solo una opción para desarrollo local; se revisó con qué permisos cuenta el connection string.

12. > no tengo acceso al portal, lo maneja el profesor. simplemente recibí el string

    Resultado: como no hay acceso al portal, se consultaron solo los metadatos de la entidad con un script descartable (sin leer ni enviar mensajes) para averiguar si `grupo9` es topic o cola y qué suscripciones tiene. Hallazgos: el string del `.env` no tiene el prefijo `Endpoint=`; la entidad es el topic `grupo9` (no `grupo9-booking-topic`); el topic no tiene ninguna suscripción; la clave `All` permite administrar.

13. > ahí lo arregle, se me paso el Endpoint= je

    Resultado: se verificó que el `.env` corregido ya se parsea y conecta al namespace. Quedan pendientes: topic por defecto `grupo9` y la suscripción `booking`, que no existe.

14. > ahora si

    Resultado: se volvió a verificar el `.env` y la conexión al namespace.

15. > hacé el cambio del topic a grupo9, y crea la suscripción

    Resultado: el topic por defecto de `BrokerOptions` pasó a `grupo9` y se creó la suscripción `booking` (MaxDeliveryCount 10, dead-letter al vencer) en el topic `grupo9` con un script de administración descartable.

16. > bien, hace la prueba así luego commiteo

    Resultado: se hizo una prueba end-to-end descartable con la librería (publicar un `ReservationRequestedMessage` de prueba y consumirlo desde la suscripción `booking`). Resultado: OK, con logs de auditoría en orden y el mismo `correlationId`.

17. > dale, hace los dos commits en mi rama broker

    Resultado: se actualizó `broker` con `main` (fast-forward) y se hicieron dos commits en esa rama: topic `grupo9` y docker compose.

18. > está fallando el build, probablemente porque falta crear notificaciones-creada y notificaciones-confirmada en azure

    Resultado: `dotnet build` de la solución compila con 0 errores. En Azure solo existen las suscripciones `auditlog` y `booking`; faltan `notificaciones-creada` y `notificaciones-confirmada`. La creación fue bloqueada por el sistema de permisos y quedó pendiente de decisión del usuario.

19. > bien, autorizo a crear las suscripciones. por lo que veo el error surge al usar docker, ya que availability usa SQL server embebido y no el del compose. agrega la linea en el compose para pisarle el server y que use el del compose

    Resultado: se crearon `notificaciones-creada` y `notificaciones-confirmada` en el topic `grupo9` (MaxDeliveryCount 10, dead-letter al vencer). En `docker-compose.yml`, Availability pisa `ConnectionStrings__DbConexion` para usar la base `AvailabilityDb` del contenedor `sqlserver` y espera a que esté sano.

20. > sí, sumá Authorization y Notifications al compose. primero analiza el código de ambos microservicios para que no se te pase nada necesario.

    Resultado: se analizó el código de Authorization, Notifications y cómo los usan Availability, Booking y Payment. En `docker-compose.yml` se agregaron `authorization` y `notifications`, un servicio `certs` que genera un certificado autofirmado (HTTPS obligatorio en Authorization y en el login de Availability), el alias `authorization.localtest.me` para que la URL del issuer sea la misma desde el navegador y desde los contenedores, y las variables `Auth__*` de Booking, Payment y Availability. Availability y Authorization corren en `Development` porque solo ahí migran/crean la base. Se verificó en un proyecto aislado: discovery y token del Authorization, redirect de Availability al login, confianza TLS desde contenedores .NET (con prueba en negativo) y Notifications respondiendo.

## Reglas de este archivo

- Cada prompt que le doy al modelo para implementar el broker se agrega acá, textual, con su resultado.
- No se editan ni se borran entradas anteriores.
- Si se usa Copilot u otra herramienta, se registra igual, indicando cuál.
