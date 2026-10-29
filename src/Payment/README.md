# Payment — Fase 1

Implementación gRPC code first con protobuf-net.Grpc. No requiere SQL ni autenticación.
Los pagos se guardan en memoria hasta detener Payment.Api.

## Capas

- Payment.Contracts: interfaz gRPC, mensajes serializados y enum público con UNSPECIFIED = 0.
- Payment.Api: comunicación gRPC y traducción de errores.
- Payment.Business: reglas en PaymentService; no depende de gRPC.
- Payment.Data: entidad anémica, enum de dominio y repositorio en memoria.

El enum del dominio solo admite PROCESSING, APPROVED y REJECTED. La API lo convierte
explícitamente al enum del contrato; sus valores numéricos no se usan como equivalentes.

## Ejecutar

Desde la raíz de la solución:

```powershell
dotnet run --project src/Payment/Payment.Api --launch-profile http
dotnet test tests/Payment.Tests/Payment.Tests.csproj
```

Payment escucha en http://localhost:5087 usando HTTP/2. No redirige a HTTPS.
La integración del cliente en Booking queda pendiente; esta fase no modifica Booking.

## Contrato para la futura integración con Booking

Booking deberá referenciar Payment.Contracts y configurar un cliente gRPC para
IPaymentGrpcService apuntando a Payment. El llamador debe conservar el identificador del intento.
Generarlo una sola vez al iniciar el intento, nunca dentro de cada llamada o reintento:

```csharp
var paymentAttemptId = Guid.NewGuid();
var request = new PayRequest
{
    BookingId = bookingId,
    Amount = amount,
    PaymentAttemptId = paymentAttemptId
};
var response = await paymentClient.PayAsync(request);

// Ante una falla de comunicación o PROCESSING, volver a consultar el mismo intento:
response = await paymentClient.PayAsync(request);

// Solo después de REJECTED puede iniciarse otro intento con un nuevo Guid.
```

Payment recibe el identificador explícitamente y lo utiliza para identificar el intento.
La referencia al contrato, el registro del cliente, su configuración y la conexión
con el flujo de reservas deberán implementarse en Booking por separado.

## Comportamiento y concurrencia

- Mismo bookingId y paymentAttemptId con igual importe: mismo pago, incluso si está PROCESSING.
- Mismo intento con distinto importe: conflicto.
- Otro intento con un pago APPROVED o PROCESSING en la reserva: conflicto.
- Sin pagos o con todos REJECTED: crea PROCESSING y simula APPROVED si amount > 0;
  de lo contrario finaliza REJECTED.
- Guid.Empty en cualquiera de los identificadores: gRPC InvalidArgument.
- Conflictos: gRPC AlreadyExists, con el motivo en el detalle.
- Las búsquedas individuales y la actualización de un ID inexistente devuelven null.
- Solo se permite actualizar PROCESSING a APPROVED o REJECTED.

El servicio y el repositorio son singleton. PaymentService protege conjuntamente la
consulta, validación y creación mediante un lock compartido; libera el lock antes de
procesar. El repositorio protege sus datos y devuelve copias para impedir cambios externos.
La simulación asíncrona no agrega demoras artificiales ni cancela el pago si el cliente se
desconecta después de crearlo.

Esta exclusión protege una única instancia del proceso. Los datos y la coordinación
distribuida entre réplicas quedan para una fase con persistencia.

Las pruebas cubren reglas, reintentos, concurrencia, transiciones y aislamiento del
repositorio, además de llamadas gRPC a la aplicación con un cliente de Payment.Contracts.
La plantilla previa de Payment.Api conserva archivos y dependencias de infraestructura;
no se registran ni se ejecutan SQL, autenticación o middleware de logs de aplicación.

## Generar el .proto y probar desde VS Code

Desde la raíz de la solución:

```powershell
dotnet run --project tools/Payment.ProtoGenerator
```

La herramienta usa SchemaGenerator de protobuf-net.Grpc.Reflection sobre
IPaymentGrpcService. Genera Protos/payment.proto y exporta Protos/protobuf-net/bcl.proto
desde la dependencia oficial. También valida el esquema y sus imports.
Los contratos C# siguen siendo la fuente; ejecutar de nuevo al modificarlos.
Se puede pasar otro directorio de salida con `-- ruta/de/salida`.

Instalar la extensión httpYac:

```powershell
code --install-extension anweber.vscode-httpyac
dotnet run --project src/Payment/Payment.Api --launch-profile http
```

Abrir Payment.Api/Payment.Api.http y usar Send Request de httpYac sobre cada caso,
en orden. El archivo ya importa el esquema generado. Las dos primeras respuestas
deben mostrar APPROVED y el mismo PaymentId; siguen un rechazo, otro intento aprobado,
dos conflictos (código gRPC 6) y un identificador vacío (código gRPC 3).
Esos errores son resultados esperados. Los ejemplos usan IDs fijos: para repetir
desde cero, reiniciar Payment o cambiar los identificadores.

Como Guid y decimal usan los mensajes de bcl.proto, los cuerpos contienen objetos
en lugar de strings UUID o números decimales directos. Los valores de 64 bits se
escriben como strings para conservar precisión en JavaScript.
Por ejemplo, 150.75m se representa con lo = "15075", hi = 0 y signScale = 4:
signScale codifica la escala decimal desplazada un bit y el signo en el bit inferior.

Se verificaron los siete casos con httpYac contra un proceso real de Payment.
La herramienta de exportación no necesita levantar Payment ni conectarse a Booking o SQL.
