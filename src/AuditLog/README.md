# AuditLog

Microservicio ASP.NET Core (.NET 10) que consume el contrato existente
`Broker.Messaging.Contracts.AuditLogMessage` y guarda los registros en LiteDB.

## Configuración y ejecución

La configuración usa `appsettings.json`, `IConfiguration` y
`GetConnectionString("LiteDb")`. Se puede sobrescribir con variables de entorno:

```powershell
$env:SERVICEBUS_CONNECTION_STRING = '<connection string del broker>'
$env:ConnectionStrings__LiteDb = 'Filename=Data/auditlog.db;Connection=direct'
$env:AuditLog__SubscriptionName = 'auditlog'
$env:AuditLog__Password = '<contraseña del front>'
dotnet run --project src/AuditLog/AuditLog.Api
```

El topic predeterminado es `grupo9` (`Broker__TopicName`). La suscripción
predeterminada es `auditlog` y debe existir en Azure Service Bus. Si la suscripción
filtra por Subject, debe aceptar `AuditLogMessage`, que es el nombre del contrato
y el Subject usado por el publisher compartido. El servicio no crea entidades
remotas automáticamente.

Con Docker, completar `SERVICEBUS_CONNECTION_STRING`, `SA_PASSWORD` y
`AUDITLOG_PASSWORD` en el `.env` de la raíz y ejecutar desde esa carpeta:

```powershell
docker compose up --build -d auditlog
```

Abrir http://localhost:5305 e ingresar con `AUDITLOG_PASSWORD`. Compose lee el
`.env` al iniciar el contenedor; la contraseña no se incluye en la imagen ni en el
JavaScript. Con `dotnet run`, el `.env` no se carga automáticamente: definir las
variables de entorno del ejemplo anterior.

Copiar completa la conexión de Service Bus, incluido `Endpoint=`:

```text
Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<policy>;SharedAccessKey=<key>;EntityPath=grupo9
```

La ruta relativa de LiteDB se resuelve desde el directorio del proyecto; el
directorio se crea automáticamente. LiteDB es embebida: no necesita servidor.
Se utiliza una instancia compartida por el consumidor y HTTP, administrada por DI.
En Docker Compose se publica el puerto 5305 y el archivo se conserva en el volumen
`auditlogdata`, montado en `/data`. Cada instancia debe tener su propio archivo;
esta configuración no comparte una base entre réplicas.

### Topic compartido y suscripción de auditoría

```text
Topic: grupo9
├── Suscripción: booking  → Booking
└── Suscripción: auditlog → AuditLog (Subject = AuditLogMessage)
```

Los servicios publican en el mismo topic. Cada consumidor necesita su propia
suscripción para recibir una copia. Si AuditLog compartiera la de Booking, ambos
competirían por los mensajes. El filtro de auditoría debe aceptar el Subject
`AuditLogMessage`.

No hace falta entrar al portal si la conexión tiene permiso **Manage** sobre el
topic. Este ejemplo se ejecuta una sola vez desde una herramienta de consola .NET
con el paquete `Azure.Messaging.ServiceBus` y la variable
`SERVICEBUS_CONNECTION_STRING` cargada en su entorno:

```csharp
using Azure.Messaging.ServiceBus.Administration;

var connection = Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTION_STRING")
    ?? throw new InvalidOperationException("Falta SERVICEBUS_CONNECTION_STRING.");
var admin = new ServiceBusAdministrationClient(connection);

if (!(await admin.SubscriptionExistsAsync("grupo9", "auditlog")).Value)
{
    await admin.CreateSubscriptionAsync(
        new CreateSubscriptionOptions("grupo9", "auditlog"),
        new CreateRuleOptions("AuditLogMessages",
            new CorrelationRuleFilter { Subject = "AuditLogMessage" }));
}
```

Si ya existe, el ejemplo no modifica sus reglas. La suscripción `auditlog` del
entorno usado en la prueba se creó con ese filtro. En otro entorno hay que
verificarla o crearla antes de publicar los eventos de prueba. Con permisos
Send/Listen solamente, la creación requiere a alguien con permiso Manage.

## Procesamiento

- `LogId` nulo o `Guid.Empty`: genera un GUID.
- `DateUtc` nulo o `DateTime.MinValue`: genera la fecha UTC actual. Las fechas
  recibidas se normalizan a UTC; una fecha sin zona se interpreta como UTC.
- Los demás campos conservan sus valores, incluidos nulos y cadenas vacías.
  `Details` conserva su JSON en el documento y se devuelve como objeto JSON.
- Se confirma el mensaje después de persistirlo. Ante errores, el suscriptor
  compartido permite la reentrega. Un `LogId` ya existente conserva el registro
  original. Si el mensaje no traía `LogId`, una nueva entrega genera otro GUID.

## Cómo emitir eventos para AuditLog

Desde otro microservicio, agregá una referencia al proyecto
`src/Broker/Broker.Messaging/Broker.Messaging.csproj`. En su `Program.cs`,
registrá el broker y el publicador si todavía no están registrados:

```csharp
using Broker.Messaging.DependencyInjection;

builder.Services.AddBroker(builder.Configuration);
builder.Services.AddBrokerPublisher();
builder.Services.AddScoped<AuditExample>();
```

El servicio emisor debe tener `SERVICEBUS_CONNECTION_STRING` configurada y
apuntar al mismo topic que AuditLog (`grupo9` de forma predeterminada).
Luego inyectá `IMessagePublisher` en el servicio que genera el evento:

```csharp
using Broker.Messaging.Abstractions;
using Broker.Messaging.Contracts;

public sealed class AuditExample(IMessagePublisher publisher)
{
    public Task EmitAsync(string correlationId, CancellationToken cancellationToken)
    {
        var log = new AuditLogMessage(
            LogId: Guid.NewGuid(),
            DateUtc: DateTime.UtcNow,
            Service: "Booking",
            Action: "ReservationCreated",
            Message: "Se creó una reserva",
            CorrelationId: correlationId,
            Details: new Dictionary<string, object?>
            {
                ["ReservationId"] = "reserva-123",
                ["Amount"] = 15000
            });

        return publisher.PublishAsync(log, correlationId, cancellationToken);
    }
}
```

Invocá `EmitAsync` desde el flujo que querés auditar. El publicador serializa el
evento y lo envía al topic con `Subject = AuditLogMessage` y
`ContentType = application/json`; AuditLog lo recibe mediante su suscripción.
El nombre del contrato es **AuditLogMessage**, en singular.

El emisor usa la conexión al broker; no necesita la dirección HTTP ni la
contraseña del front. AuditLog no expone un `POST /logs` para insertar eventos.

El `CorrelationId` del cuerpo es el que se guarda y se usa para filtrar los logs.
Pasá el mismo valor a `PublishAsync` para que coincida con el identificador del
broker. Podés omitir `LogId` y `DateUtc` para que AuditLog los genere; si vas a
reintentar el envío, conservá el mismo `LogId` para evitar duplicar el registro.

### Envío manual para probar

Desde una herramienta que permita publicar en Azure Service Bus, enviá al topic
`grupo9` un mensaje con estas propiedades:

- Subject: `AuditLogMessage`.
- ContentType: `application/json`.
- CorrelationId: `prueba-auditlog-1`.

Usá este cuerpo JSON, respetando las mayúsculas de los nombres de los campos:

```json
{
  "Service": "PruebaManual",
  "Action": "Test",
  "Message": "Evento de prueba para AuditLog",
  "CorrelationId": "prueba-auditlog-1",
  "Details": {
    "Origin": "manual",
    "Success": true
  }
}
```

Este ejemplo omite `LogId` y `DateUtc` para comprobar su generación automática.
Iniciar sesión en el front y buscar el Correlation ID `prueba-auditlog-1`, o
consultar esta URL desde el mismo navegador autenticado:

```text
http://localhost:5305/logs?correlationId=prueba-auditlog-1
```

La recepción es asíncrona: consultá nuevamente si el evento todavía no se procesó.
El envío se realiza por el broker; AuditLog solo expone HTTP para consultar logs.
Los mensajes de `ILogger` con texto `[AUDITORIA]` no publican automáticamente un
`AuditLogMessage`: el emisor debe llamar a `IMessagePublisher` explícitamente.

### Prueba integrada con Payment

Desde la raíz, levantar Payment y AuditLog; SQL Server se inicia como dependencia:

```powershell
docker compose up --build -d payment auditlog
```

Payment publica desde `PaymentService.PayAsync`. Para invocarlo por gRPC, abrir
[`Payment.Api.http`](../Payment/Payment.Api/Payment.Api.http) en VS Code con
httpYac. El archivo ya importa el esquema; usar este cuerpo en una solicitud a
`localhost:5087/Payment.Contracts.PaymentGrpcService/Pay`:

```json
{
  "BookingId": { "lo": "900001", "hi": "0" },
  "PaymentAttemptId": { "lo": "900101", "hi": "0" },
  "Amount": { "lo": "15075", "hi": 0, "signScale": 4 }
}
```

El contrato usa la representación de Guid y decimal de protobuf-net; el importe
es 150,75. Para crear un pago nuevo al repetir la prueba, cambiar ambos
identificadores por otros numéricos válidos. Reutilizar los mismos conserva el pago.

En el front, buscar el servicio `Payment` y pulsar **Actualizar**. Cada llamada
genera un Correlation ID nuevo y registra:

| Acción | Contenido |
|---|---|
| `InitProcess` | Inicio de `ProcesarPago`. |
| `EndProcess` | `Details.Result` con PaymentId, BookingId, PaymentAttemptId, Amount y Status. |
| `EmitError` | Ante una excepción, ErrorType, ErrorMessage y StackTrace; reemplaza a EndProcess. |

Un resultado de negocio `REJECTED` también genera `EndProcess`. Para probar un
error, enviar importe cero: la llamada devuelve gRPC InvalidArgument y se
registran InitProcess y EmitError. Copiar el Correlation ID de una fila al filtro
permite aislar una llamada. Actualmente se genera dentro de Payment y no se
reutiliza el RequestId de Availability.

## Sesión y consulta desde otro cliente HTTP

`GET /logs` requiere una cookie de sesión. Sin autenticar devuelve HTTP 401.
El login de ejemplo usa una contraseña compartida y una cookie de cuatro horas.

Ejemplo en PowerShell, con la contraseña configurada en el servidor:

```powershell
$loginBody = @{ password = (Read-Host 'Contraseña de AuditLog') } | ConvertTo-Json
Invoke-WebRequest -Uri 'http://localhost:5305/session' -Method Post `
    -ContentType 'application/json' -Body $loginBody -SessionVariable auditSession

Invoke-RestMethod -Uri 'http://localhost:5305/logs?service=Payment&sortBy=dateUtc&sortDirection=desc' `
    -WebSession $auditSession

Invoke-WebRequest -Uri 'http://localhost:5305/session' -Method Delete `
    -WebSession $auditSession
```

`POST /session` devuelve 204 al autenticar, 401 si la contraseña es incorrecta y
503 si no se configuró. `GET /session` comprueba la sesión y `DELETE /session`
la cierra.

## GET /logs

```text
http://localhost:5305/logs?correletionId=request-1&action=Created&service=Booking&startDate=2026-10-31T00:00:00Z&endDate=2026-10-31T23:59:59Z&page=1
```

Todos los filtros son opcionales: `correletionId` (también se acepta
`correlationId`; si se envían ambos tiene prioridad `correletionId`), `action`,
`logId`, `service`, `startDate`, `endDate` y `page`. Los filtros se combinan con
AND. Las cadenas usan igualdad con la colación predeterminada de LiteDB.
Las fechas son ISO 8601, con límites inclusivos; conviene indicar `Z` o un offset.
El front convierte las fechas ingresadas en hora local a UTC y las muestra en hora local.

El ordenamiento se aplica en el servidor antes de paginar:

- `sortBy`: `dateUtc`, `service`, `action`, `message`, `correlationId` o `logId`.
- `sortDirection`: `asc` o `desc`.

En el front se alterna pulsando los encabezados de la tabla. Los detalles JSON se
despliegan por fila con **Ver JSON**.

Devuelve un arreglo JSON de hasta 50 registros. Por defecto ordena por fecha
descendente y luego por LogId descendente para desempatar.
`page` comienza en 1 (valor predeterminado). Una página sin resultados devuelve
`[]`. Una página inválida, un GUID o fecha mal formado, o un rango invertido
devuelven HTTP 400, al igual que un campo o dirección de ordenamiento inválidos.
No hay parámetro para ampliar el tamaño de página. Si una página contiene
exactamente 50 registros, el front habilita Siguiente aunque la próxima pueda estar vacía.

## Diagnóstico

```powershell
docker compose logs --tail=100 auditlog payment
```

| Síntoma | Revisar |
|---|---|
| Error de formato de conexión | La cadena completa de Service Bus debe incluir `Endpoint=`. |
| `MessagingEntityNotFound` | Existencia del topic `grupo9` y de la suscripción `auditlog`. |
| Error de autorización al crear la suscripción | La conexión necesita permiso Manage para crearla. |
| Payment responde pero no aparecen logs | Filtro Subject de la suscripción y errores `No se pudo publicar la auditoría` en Payment. |
| HTTP 401 al consultar `/logs` | Iniciar sesión y conservar la cookie. |
| HTTP 503 al iniciar sesión | Configurar `AUDITLOG_PASSWORD` en Compose o `AuditLog__Password` fuera de Docker. |

La suscripción recibe los mensajes publicados mientras existe; para comprobar una
suscripción recién creada, generar un pago nuevo. El botón Actualizar vuelve a
consultar LiteDB y no vuelve a ejecutar ni publicar el pago.

## Verificación

```powershell
dotnet test tests/AuditLog.Tests/AuditLog.Tests.csproj
```

Las pruebas ejercitan el handler, LiteDB real, persistencia al reabrir el archivo,
el endpoint HTTP, campos opcionales, filtros, validación, paginación, ordenamiento,
login y cierre de sesión sin depender de una conexión remota a Azure Service Bus.

Referencia: [configuración UTC de LiteDB](https://www.litedb.org/docs/pragmas/).
