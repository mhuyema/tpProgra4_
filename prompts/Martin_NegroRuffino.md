# Historial de prompts - Martín Negro Ruffino

## Prompt 1 - Setup - API para integración con Booking

FASE 1 Implementación del módulo Payment usando gRPC code first. Por ahora los datos se guardan en memoria. SQL, autenticación y logs quedan para después.

1. Crear una clase Payment con:
   - paymentId: Guid, generado por Payment con Guid.NewGuid().
   - bookingId: Guid, es el Id de la reserva que recibe de Booking.
   - paymentAttemptId: Guid, lo recibe de Booking e identifica el intento de pago.
   - amount: decimal.
   - status: enum con PROCESSING, APPROVED y REJECTED.
   - createdAt: DateTimeOffset, fecha de creación en UTC.
1. Crear un proyecto Payment.Contracts, referenciado por Payment. Dentro crear:
   - IPaymentGrpcService con un método PayAsync.
   - PayRequest con bookingId, amount y paymentAttemptId.
   - PayResponse con bookingId, amount, paymentId y status.
   - PaymentStatus con UNSPECIFIED = 0, PROCESSING, APPROVED y REJECTED.
   Agregar las anotaciones necesarias para gRPC code first y la serialización de los datos.
3. Crear PayGrpcService, que implemente IPaymentGrpcService. Recibe el pedido, llama a PaymentService y devuelve el resultado como PayResponse.
4. Crear IPaymentService y PaymentService en la capa de negocio:
   - Validar que bookingId y paymentAttemptId no sean Guid.Empty.
   - Si ya existe un pago con el mismo bookingId y paymentAttemptId y el importe coincide, devolver ese pago sin procesarlo otra vez.
   - Si existe pero el importe es distinto, devolver un error de conflicto.
   - Si es un intento nuevo y la reserva tiene un pago APPROVED o PROCESSING, rechazar la operación.
   - Si no tiene pagos o los anteriores están REJECTED, crear un pago con status PROCESSING.
   - Como simulación, si amount es mayor a 0, aprobarlo. Si es 0 o menor, rechazarlo.
   - Guardar el estado final y devolver el pago.
   Booking debe enviar el mismo paymentAttemptId cuando repite una llamada. Para hacer un nuevo intento después de un rechazo, debe generar otro Guid.
5. Crear IPaymentRepository y PaymentRepository en la capa de acceso a datos, con estos métodos:
   - CreatePayment: guarda un pago nuevo.
   - GetPayment: busca por paymentId.
   - GetPaymentByBookingId: devuelve la lista de pagos de una reserva.
   - GetPaymentByAttempt: busca por bookingId y paymentAttemptId.
   - UpdatePaymentStatus: recibe paymentId y el nuevo estado.
   Si una búsqueda individual no encuentra el pago, devuelve null. Solo permitir cambios de PROCESSING a APPROVED o REJECTED.
6. Usar inyección de dependencias. Mantener los datos entre llamadas mientras la aplicación esté ejecutándose y proteger la validación y creación para que dos llamadas simultáneas no generen pagos duplicados.
Mantener N-Layer y dominio anémico. Las reglas van en PaymentService, el repositorio maneja los datos y PayGrpcService se encarga de la comunicación. Si se repite una llamada mientras el pago sigue PROCESSING, devolver ese estado sin iniciar otro cobro.

## Prompt 2 - Migración

Crear un mapeo con EF Core para la tabla Payments, utilizando los datos de la clase Payment existente:

- PaymentId: uniqueidentifier, clave primaria. Mantener el Guid generado por la entidad.
- BookingId: uniqueidentifier.
- PaymentAttemptId: uniqueidentifier.
- Amount: decimal(18,2), con dos decimales.
- Status: int en SQL, manteniendo el enum PaymentStatus en C#.
- CreatedAt: datetimeoffset.

PaymentId no se puede repetir. La combinación de BookingId y PaymentAttemptId tampoco se puede repetir, para evitar registrar dos veces el mismo intento.
Se puede crear un nuevo pago para un BookingId si no tiene pagos anteriores o si todos están REJECTED. Si tiene alguno en PROCESSING o APPROVED, no se debe permitir otro pago. Implementar esta restricción con un índice único filtrado sobre BookingId para los estados PROCESSING y APPROVED.
BookingId no debe tener una clave foránea hacia Booking, porque cada módulo tiene su propia base.
Agregar el mapeo en el AppDbContext existente, siguiendo el estilo de Booking. Generar la migración inicial y dejar los archivos listos para commitear.
Reemplazar la creación actual de la base vacía por `Database.MigrateAsync()`, para que al iniciar Payment se cree la base si falta y se apliquen las migraciones pendientes.
Por ahora hacer solamente el setup de la tabla y la migración, sin modificar los métodos del repositorio ni la lógica de negocio. Verificar que compile y que se cree la tabla en PaymentDb.

## Prompt 3 - Implementar SQL en Repository

Adaptar el PaymentRepository existente para que utilice la base SQL
en lugar de guardar los pagos en memoria.

Usar el contexto y la tabla Payments ya configurados, siguiendo
la forma de trabajar del repositorio de Booking.

Implementar los métodos actuales para crear pagos, buscarlos
y actualizar su estado, manteniendo las reglas que ya tiene Payment.

Si llega nuevamente el mismo intento con el mismo importe, devolver
el pago existente. Si cambia el importe, devolver un conflicto.
Contemplar también que puedan llegar solicitudes al mismo tiempo.

Ajustar la configuración necesaria para que el repositorio y el
servicio trabajen correctamente con EF Core.

Mantener el contrato gRPC y verificar que los pagos queden guardados
y puedan consultarse después de reiniciar Payment.

## Prompt 4 - Implementación de AuditLogMessage en Broker

Agregá en el proyecto Broker.Messaging un contrato AuditLogMessage, siguiendo el estilo de los mensajes que ya están en Contracts.

Tiene que tener estos campos, todos opcionales y con null por defecto:

- LogId: Guid?
- DateUtc: DateTime?
- Service: string?
- Action: string?
- Message: string?
- CorrelationId: string?
- Details: Dictionary<string, object?>?

Details nos va a servir para mandar datos distintos según el registro, por ejemplo información de un error o de una reserva.

Por ahora solo agregá el contrato en el broker. No modifiques el publisher, los otros servicios ni la configuración de Azure.

## Prompt 5 - Implementación de AuditLogServices

Crear un proyecto dentro del tp que es un microservicio más del proyecto.  
Este microservicio debe llamarse AuditLog.

Está suscripto a AuditLogMessages del message broker. Este message ya está definido en el broker.  
Al recibir un mensaje debe validar LogId y DateUtc; si no los tiene, debe crearles un valor.  
Para el restos de los campos acepta sus valores, pudiendo estar vacíos.  
Luego de recibir y procesar los datos, debe guardarlos en base noSQL usando LiteDB.  
Crear las configuraciones necesarias para la conexión de LiteDB, utilizando el estándar de .NET.

Por otro lado, debe exponer un endpoint GET /logs que recibe como parámetros correletionId, action, logId, service, startDate, endDate y page. Todos los parámetros son opcionales. Siempre devuelve un máximo de 50 resultados y controla la paginación con el parámetro page.

