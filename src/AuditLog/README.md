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
dotnet run --project src/AuditLog/AuditLog.Api
```

El topic predeterminado es `grupo9` (`Broker__TopicName`). La suscripción
predeterminada es `auditlog` y debe existir en Azure Service Bus. Si la suscripción
filtra por Subject, debe aceptar `AuditLogMessage`, que es el nombre del contrato
y el Subject usado por el publisher compartido. No se crean entidades remotas.

La ruta relativa de LiteDB se resuelve desde el directorio del proyecto; el
directorio se crea automáticamente. LiteDB es embebida: no necesita servidor.
Se utiliza una instancia compartida por el consumidor y HTTP, administrada por DI.
En Docker Compose se publica el puerto 5305 y el archivo se conserva en el volumen
`auditlogdata`, montado en `/data`. Cada instancia debe tener su propio archivo;
esta configuración no comparte una base entre réplicas.

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
Con AuditLog ejecutándose, consultá:

```text
http://localhost:5305/logs?correletionId=prueba-auditlog-1
```

La recepción es asíncrona: consultá nuevamente si el evento todavía no se procesó.
El envío se realiza por el broker; AuditLog solo expone HTTP para consultar logs.
Los mensajes de `ILogger` con texto `[AUDITORIA]` no publican automáticamente un
`AuditLogMessage`: el emisor debe llamar a `IMessagePublisher` explícitamente.

## GET /logs

```text
http://localhost:5305/logs?correletionId=request-1&action=Created&service=Booking&startDate=2026-10-31T00:00:00Z&endDate=2026-10-31T23:59:59Z&page=1
```

Todos los filtros son opcionales: `correletionId` (también se acepta
`correlationId`; si se envían ambos tiene prioridad `correletionId`), `action`,
`logId`, `service`, `startDate`, `endDate` y `page`. Los filtros se combinan con
AND. Las cadenas usan igualdad con la colación predeterminada de LiteDB.
Las fechas son ISO 8601, con límites inclusivos; conviene indicar `Z` o un offset.

Devuelve un arreglo JSON de hasta 50 registros, ordenados por fecha descendente
y luego por LogId descendente para mantener estable la paginación ante empates.
`page` comienza en 1 (valor predeterminado). Una página sin resultados devuelve
`[]`. Una página inválida, un GUID o fecha mal formado, o un rango invertido
devuelven HTTP 400. No hay parámetro para ampliar el tamaño de página.

## Verificación

```powershell
dotnet test tests/AuditLog.Tests/AuditLog.Tests.csproj
```

Las pruebas ejercitan el handler, LiteDB real, persistencia al reabrir el archivo,
el endpoint HTTP, campos opcionales, filtros, validación y paginación sin depender
de una conexión remota a Azure Service Bus.

Referencia: [configuración UTC de LiteDB](https://www.litedb.org/docs/pragmas/).
