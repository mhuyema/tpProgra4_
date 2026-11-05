# Registro unificado de prompts utilizados durante el desarrollo del trabajo práctico

**Integrante:** Gianni Mozzi\
**Trabajo práctico:** Sistema de reserva de asientos basado en
microservicios\
**Tecnologías principales:** .NET 10, C#, Azure Service Bus, gRPC,
Entity Framework Core, SQL Server, Docker y Git/GitHub\
**Herramienta de IA utilizada:** Claude (Anthropic)\
**Alcance del trabajo:** Desarrollo e integración de componentes de
Booking y Notifications, junto con tareas de análisis, pruebas y
coordinación con el repositorio grupal.
------------------------------------------------------------------------

## 1. Análisis del enunciado y planificación del desarrollo

### Prompt 1. Análisis de requerimientos y distribución de responsabilidades

> Analizá el enunciado del trabajo práctico y la estructura inicial del
> proyecto. Identificá los requerimientos funcionales y técnicos
> correspondientes al microservicio Booking, así como sus dependencias
> con Availability, Payment, Authorization y Notifications.
>
> A partir de ese análisis, proponé una división del trabajo entre los
> integrantes responsables de Booking. La distribución debe delimitar
> responsabilidades, componentes, clases y entregables de cada
> integrante, evitando superposiciones en el desarrollo.
>
> Además, identificá los contratos, interfaces y decisiones de
> arquitectura que deberían acordarse antes de comenzar la
> implementación para facilitar la integración posterior con el resto
> del equipo. Justificá las recomendaciones considerando la arquitectura
> de microservicios y la organización del repositorio.

**Objetivo:** comprender el alcance del trabajo, organizar las tareas y
establecer acuerdos técnicos antes de implementar.

## 2. Diseño e implementación del microservicio Booking

### Prompt 2. Definición de la estructura por capas

> Diseñá la estructura inicial del microservicio Booking utilizando una
> arquitectura N-Layer, separando las responsabilidades en los proyectos
> Booking.Api, Booking.Business y Booking.Data.
>
> Indicá qué responsabilidades corresponden a cada capa, qué referencias
> entre proyectos deben establecerse y cómo debería organizarse la
> solución en .NET 10. La capa de presentación debe encargarse de los
> puntos de entrada, Business de los casos de uso y reglas de negocio, y
> Data del acceso a persistencia.
>
> Proponé una estructura inicial coherente con los requerimientos del
> trabajo práctico y explicá las decisiones de diseño. Priorizá una
> implementación comprensible, mantenible y consistente con el nivel
> académico del proyecto.

**Objetivo:** establecer una separación de responsabilidades que
facilite el mantenimiento y la integración del servicio.

### Prompt 3. Modelado de la entidad y persistencia

> A partir de los requerimientos de Booking, diseñá la entidad Reserva y
> su persistencia mediante Entity Framework Core y SQL Server.
>
> Considerá los estados posibles de una reserva ---Retenida, Confirmada,
> Rechazada y Vencida---, los motivos de rechazo y los datos necesarios
> para identificar una solicitud y un asiento.
>
> Analizá cómo garantizar la idempotencia mediante el identificador de
> solicitud y cómo evitar que dos operaciones concurrentes reserven el
> mismo asiento. Proponé las restricciones e índices necesarios a nivel
> de base de datos, explicando qué problema resuelve cada uno.
>
> Incluí la configuración del DbContext, el registro de dependencias y
> los pasos para generar y aplicar las migraciones. Diferenciá las
> responsabilidades de la entidad, el contexto y la infraestructura de
> persistencia.

**Objetivo:** implementar un modelo persistente que contemple integridad
de datos, concurrencia e idempotencia.

### Prompt 4. Implementación de la lógica de negocio y patrón Repository

> Desarrollá una propuesta para implementar la lógica de procesamiento
> de solicitudes de reserva en Booking.Business, utilizando el patrón
> Repository para desacoplar las reglas de negocio de Entity Framework
> Core y SQL Server.
>
> El flujo debe contemplar la recepción de una solicitud, la consulta de
> solicitudes previamente procesadas, la verificación de disponibilidad
> del asiento y la creación de una reserva en estado Retenida o
> Rechazada, según corresponda.
>
> Prestá especial atención a la idempotencia ante posibles entregas
> duplicadas del broker y a las condiciones de concurrencia cuando
> distintas instancias intentan reservar el mismo asiento.
>
> Presentá las interfaces y clases involucradas, explicá el propósito de
> cada método y detallá cómo se deben manejar las violaciones de índices
> únicos sin trasladar los detalles de infraestructura a la capa de
> negocio.

**Objetivo:** construir el caso de uso principal de Booking con
responsabilidades separadas y un tratamiento consistente de solicitudes
duplicadas o concurrentes.

## 3. Integración entre microservicios

### Prompt 5. Adaptación al contrato compartido de mensajería

> Analizá la implementación actual de Booking y los componentes de
> mensajería compartidos por el equipo. Compará el mecanismo de consumo
> de mensajes desarrollado inicialmente con la abstracción provista por
> la biblioteca Broker.Messaging.
>
> Identificá incompatibilidades en los contratos, nombres de mensajes,
> topics, suscripciones, interfaces y configuración de dependencias. A
> partir de esa revisión, proponé los cambios necesarios para que
> Booking utilice el mecanismo de integración acordado por el equipo,
> evitando mantener implementaciones duplicadas.
>
> Indicá qué archivos deben modificarse, qué referencias entre proyectos
> deben agregarse y qué componentes deben conservarse o reemplazarse. No
> alteres contratos compartidos sin justificar previamente el impacto
> sobre los demás microservicios.

**Objetivo:** adaptar Booking a la infraestructura de mensajería común y
reducir conflictos de integración.

### Prompt 6. Comunicación entre Booking y Payment mediante gRPC

> Analizá el flujo de procesamiento de una reserva y explicá cómo debe
> integrarse Booking con Payment para solicitar el cobro
> correspondiente.
>
> Determiná qué componente debe iniciar la comunicación, qué contratos
> se comparten y cómo se realiza la invocación mediante gRPC. Diferenciá
> una llamada remota entre microservicios de una invocación a un método
> local, e identificá las responsabilidades de los gateways, servicios y
> contratos involucrados.
>
> Describí cómo debería continuar el flujo según el resultado del pago,
> contemplando la confirmación o el rechazo de la reserva, los posibles
> errores de comunicación y la necesidad de mantener estados
> consistentes.
>
> Antes de proponer modificaciones, revisá la estructura existente del
> repositorio y respetá las decisiones de arquitectura y los contratos
> definidos por el equipo.

**Objetivo:** comprender y definir la integración entre Booking y
Payment sin acoplar directamente sus implementaciones internas.

## 4. Desarrollo e integración del servicio Notifications

### Prompt 7. Análisis de faltantes e integración del servicio de notificaciones

> Revisá el código fuente actualizado del trabajo práctico y contrastalo
> con los requerimientos del enunciado relacionados con el servicio
> Notifications y su integración con los demás componentes.
>
> Identificá qué funcionalidades ya están implementadas, cuáles se
> encuentran incompletas y qué modificaciones son necesarias para
> completar el flujo de notificaciones.
>
> Considerá especialmente la recepción de eventos de notificación, la
> integración con Azure Service Bus, el registro de auditoría, la
> autenticación de las operaciones de consulta y la visualización de
> avisos desde Availability.
>
> Presentá un diagnóstico organizado por componente. Para cada
> modificación propuesta, indicá los archivos afectados, su finalidad,
> las dependencias involucradas y el impacto que podría generar sobre
> los servicios existentes. Evitá reemplazar código funcional o
> introducir cambios que contradigan la arquitectura acordada por el
> equipo.

**Objetivo:** evaluar el estado real de la implementación y establecer
un plan de integración basado en el código existente.

### Prompt 8. Implementación de notificaciones, auditoría y autenticación

> A partir del diagnóstico del servicio Notifications, desarrollá una
> propuesta de implementación que complete su integración con el
> sistema.
>
> Contemplá el procesamiento de los eventos correspondientes a la
> creación y confirmación de reservas, el registro de las operaciones
> relevantes en AuditLog y la validación de autenticación en el endpoint
> de consulta.
>
> Además, analizá cómo debe exponer Availability los avisos recibidos
> para que puedan visualizarse desde la interfaz correspondiente.
>
> Para cada cambio, proporcioná el código necesario, especificá en qué
> archivo y ubicación debe incorporarse y explicá la responsabilidad de
> cada clase o método. Mantené la separación de responsabilidades,
> reutilizá los contratos compartidos y evitá introducir dependencias
> innecesarias.
>
> La solución debe ser compatible con la estructura actual del
> repositorio y contemplar su ejecución dentro del entorno de desarrollo
> utilizado por el equipo.

**Objetivo:** integrar el servicio de notificaciones con auditoría,
autenticación y visualización, manteniendo la coherencia del sistema.

## 5. Configuración del entorno y resolución de problemas

### Prompt 9. Configuración de credenciales y secretos de desarrollo

> Explicá cómo configurar de forma segura las credenciales necesarias
> para la integración con Google en el proyecto Authorization.Server
> utilizando el mecanismo User Secrets de .NET.
>
> Indicá cómo identificar el proyecto al que deben asociarse los
> secretos, cómo registrar la configuración desde Visual Studio o
> mediante la CLI de .NET y cómo comprobar que la aplicación pueda leer
> esos valores durante la ejecución.
>
> Diferenciá los secretos de usuario de los archivos de configuración
> versionados, y explicá qué información debe excluirse del repositorio
> para evitar exponer credenciales. No incluyas valores sensibles en el
> código, los logs ni los archivos que se compartan con el equipo.

**Objetivo:** configurar credenciales locales sin incorporarlas al
control de versiones.

### Prompt 10. Diagnóstico de errores de compilación y ejecución

> Analizá los errores y advertencias que aparecen al compilar, restaurar
> dependencias o ejecutar los proyectos de la solución. Utilizá los
> mensajes de error, las capturas y la estructura de archivos
> proporcionada para identificar la causa concreta de cada problema.
>
> Clasificá los inconvenientes según correspondan a referencias entre
> proyectos, paquetes NuGet, configuración de archivos JSON, registro de
> dependencias, migraciones o configuración de ejecución.
>
> Para cada caso, explicá por qué se produce el error, qué archivo o
> configuración está involucrado y cuál es la corrección mínima
> necesaria. Diferenciá los errores que impiden la ejecución de las
> advertencias que no bloquean la compilación.
>
> Proponé los pasos de verificación posteriores para confirmar que la
> solución quedó correctamente aplicada, evitando cambios innecesarios
> en componentes que ya funcionan.

**Objetivo:** utilizar la IA como apoyo para diagnosticar problemas
técnicos a partir de evidencia concreta y aplicar correcciones
controladas.

## 6. Integración colaborativa y control de versiones

### Prompt 11. Revisión de cambios y coordinación con el repositorio grupal

> Revisá la versión actualizada del repositorio del equipo y comparala
> con los cambios desarrollados en la rama correspondiente a Booking o
> Notifications.
>
> Identificá diferencias entre ambas versiones, dependencias
> incorporadas por otros integrantes y posibles conflictos en archivos
> compartidos, como docker-compose.yml, archivos de configuración,
> README y la solución de Visual Studio.
>
> Elaborá un plan de integración que indique qué cambios deben
> conservarse, cuáles deben adaptarse y en qué orden conviene
> aplicarlos. Prestá especial atención a los servicios que se ejecutan
> mediante Docker, las cadenas de conexión y la configuración de Azure
> Service Bus.
>
> Antes de recomendar sobrescribir o reemplazar archivos, verificá su
> función y el impacto sobre los demás microservicios. Proponé un
> procedimiento de trabajo con ramas independientes, commits acotados y
> Pull Requests para facilitar la revisión del equipo.

**Objetivo:** integrar los desarrollos individuales sin perder cambios
de otros integrantes ni generar inconsistencias en el repositorio.

### Prompt 12. Preparación y revisión del Pull Request

> A partir de los cambios implementados en la rama de trabajo, elaborá
> una revisión previa a la apertura del Pull Request.
>
> Verificá que los archivos modificados correspondan al alcance de la
> funcionalidad, que las referencias entre proyectos sean correctas y
> que no se incluyan credenciales, archivos temporales ni artefactos de
> compilación.
>
> Proponé una secuencia de validación que contemple compilación,
> restauración de dependencias, ejecución de pruebas y revisión de la
> configuración necesaria.
>
> Finalmente, sugerí una descripción técnica para el Pull Request que
> resuma el objetivo de los cambios, los componentes afectados, las
> decisiones relevantes y las verificaciones realizadas. Diferenciá
> claramente las tareas terminadas de aquellas que todavía dependen de
> otros integrantes o de integraciones pendientes.

**Objetivo:** preparar una entrega revisable, trazable y alineada con el
trabajo colaborativo.

## 7. Pruebas y validación del comportamiento

### Prompt 13. Diseño de pruebas de integración para Booking

> Diseñá un procedimiento de pruebas de punta a punta para validar el
> comportamiento de Booking utilizando Azure Service Bus y SQL Server
> LocalDB.
>
> Definí los requisitos de configuración del entorno, incluyendo el
> topic, la suscripción correspondiente, las variables de entorno y el
> proyecto que debe ejecutarse como punto de entrada.
>
> Establecé escenarios que permitan verificar el procesamiento de una
> solicitud nueva, la recepción duplicada de un mismo identificador, el
> intento de reservar un asiento ocupado, la recepción de mensajes
> incompletos y el vencimiento de reservas retenidas.
>
> Para cada escenario, especificá los datos de entrada, el
> comportamiento esperado, los cambios que deberían observarse en la
> base de datos y los registros relevantes de ejecución. Diferenciá los
> resultados efectivamente observados de los resultados esperados y
> explicá cómo interpretar posibles fallos.

**Objetivo:** comprobar el comportamiento del microservicio frente a
escenarios normales, duplicados, inválidos y concurrentes.

### Prompt 14. Revisión de pruebas y validación de resultados

> Analizá los logs, resultados de compilación y registros de base de
> datos obtenidos durante las pruebas del microservicio.
>
> Contrastá cada resultado con el comportamiento esperado para
> determinar si el flujo se ejecutó correctamente. Identificá posibles
> inconsistencias entre el estado de la reserva, los mensajes procesados
> y las operaciones persistidas.
>
> Cuando se detecte un error, explicá qué etapa del flujo podría estar
> originándolo y qué evidencia adicional permitiría confirmar el
> diagnóstico. No consideres una prueba exitosa únicamente porque la
> aplicación inicia o compila sin errores: verificá también el resultado
> funcional del escenario.
>
> Elaborá una conclusión que distinga las funcionalidades verificadas,
> los problemas pendientes y las mejoras que podrían incorporarse en una
> etapa posterior.

**Objetivo:** evaluar los resultados de las pruebas con evidencia y
evitar conclusiones basadas únicamente en la compilación.

------------------------------------------------------------------------

