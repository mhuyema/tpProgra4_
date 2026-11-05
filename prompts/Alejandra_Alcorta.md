# Historial de prompts — Alejandra Alcorta

Responsabilidad: Infraestructura e integración.
Herramienta: Claude Code.

Prompts seleccionados, en orden cronológico. Se corrigió la ortografía sin cambiar el contenido.

## 2026-10-28

### Etapa 1: Análisis y organización

1. (adjunto el enunciado) Te paso el TP que tenemos que hacer en grupo. A mí me toca la parte de infra e integración, aunque ya hice algo para usar (está en el proyecto Availability.UI). La solution que armó uno de mis compañeros para hacer el trabajo es tpProgra4_. Revisá tpProgra4_ y fijate si sirve para lo que se pide en el TP.

2. A mí me toca infra e integración. El grupo está dividido así:
   - Payment: Martín
   - Availability: Matías - Alex
   - Infra e integración: Ale
   - Booking: Maxi - Gian
   - Authorization: Nicolás
   - Message Broker: Manuel

   Pasame todo esto en un plan de acción para que sea más fácil de ver.

3. Creá el archivo prompts/Alejandra_Alcorta.md.

4. ¿Message Broker no sería el servicio de notificaciones?

5. ¿La tabla de interpretación del diagrama sería sobre el diagrama que tenemos que hacer o sobre el que ya tenemos?

6. La tabla, los diagramas y demás los quiero entregar en el README, que es lo primero que va a ver el profe cuando abra el repo. Armame el esqueleto con el enunciado que te pasé. Tiene que tener: los integrantes con lo que hace cada uno, el diagrama de arquitectura con el servicio de notificaciones agregado y marcado dónde se vuelve eventualmente consistente, la tabla de interpretación (componente, rol y justificación), una sección por API con su arquitectura, cómo ejecutarlo, la demo del happy path y del fallo, los logs de auditoría y el historial de prompts. Lo que todavía no sepamos dejalo como "completar", que después lo completo yo con el grupo.

## 2026-10-29

### Etapa 2: Integración de ramas

7. (adjunto la consigna) Volvé a revisar todo y armá un nuevo plan de acción.

8. En el plan de acción, marcame las tareas que son 100% mías, las que hago yo sola sin depender de otro integrante.

## 2026-10-30

### Etapa 3: Documentación

9. Revisá el README que hice y fijate si hay que modificar algo teniendo en cuenta lo que hay hasta ahora en el código.

10. En el diagrama de Booking del README, marcá como pendiente el "Publicador al broker", porque Booking todavía no publica nada y el mensaje "reserva confirmada" para Notificaciones no está definido.

11. En el mismo diagrama, cambiá los nombres de las capas por los de los proyectos reales: Booking.Api, Booking.Business y Booking.Data.

12. En el mismo diagrama, agregá los workers de vencimiento de reservas y de pagos pendientes.

13. En el mismo diagrama, mostrá que el cliente gRPC es PagoGrpcGateway, que está en la Api y se usa a través de IPagoGateway, que está en Business, para que se vea que Business no depende de gRPC.

14. En el diagrama de Payment, agregá Payment.Api y Payment.Contracts.

15. Debajo del diagrama de Payment, linkeá el README de Martín (src/Payment/README.md).

16. En el diagrama de Payment, cambiá los cuadros "Services" y "Data" por Payment.Business y Payment.Data, como en el de Booking.

### Etapa 4: Corrección de la carpeta de Booking

17. En GitHub vi que en src/Booking aparecen dos carpetas, Booking.API y Booking.Api, pero en mi código hay una sola. Explicame por qué pasa y pasame los comandos para unificarla sin perder los archivos que solo están en Booking.API, en una rama aparte desde main para que llegue a main sin arrastrar lo de mi rama.

### Etapa 5: README

18. Completá en el README cómo ejecutarlo: LocalDB, cómo levantarlo en VS y los puertos. Sacá Auth y Notificaciones del orden, todavía no existen.

19. Aclará en el README que usamos VS Community y versión 2026.

20. Agregá en el README cómo levantarlo con Docker, como segunda forma.

21. En la tabla del README, sumá a Notificaciones que también tiene un GET para consultar los avisos.

22. Hacé el diagrama de Notificaciones en el README y aclará que es el punto 3i.

23. Corregí los diagramas del README: en Booking el publicador ya no está pendiente y en Availability poné los nombres completos de los proyectos.

24. Que los diagramas de las APIs queden parejos: agregá debajo de Booking y Payment una línea que explique la arquitectura, como en Availability.

25. Actualizá el diagrama general del README con el servicio de auditoría y el GET de asientos ocupados, sumá eso a la consistencia eventual (3g) y sacá el TODO de draw.io.

26. Agregá el servicio de auditoría a la tabla de interpretación del README.

27. Agregá en el README las secciones de Authorization Server y del servicio de auditoría (3i) y sus puertos.

28. Sumá a cómo ejecutarlo Authorization Server y el servicio de auditoría.

## 2026-11-05

### Etapa 6: Correcciones de integración

29. Acabo de modificar un error que surgió después de un merge en docker-compose. Verificá si quedó bien.

30. Cuando cancelo el login con Google me tira AuthenticationFailureException. Arreglalo para que vuelva al login, y agregá en el Dockerfile lo de la caché de NuGet para que los builds en paralelo no se pisen.

31. Verificá si el docker compose up --build anduvo.

32. En el estado de la reserva los avisos siempre muestran 0, ¿por qué?

### Etapa 7: Notificaciones por mail

33. Terminemos lo de los mails: agregá Mailpit al docker-compose para que Notifications mande los mails ahí, y sumá al README cómo se envían y dónde se ven.

### Etapa 8: Revisión final del README

34. Revisá y actualizá el README con lo que falte.

35. En el docker-compose, que Notifications valide el token contra el Authorization Server del compose y no contra uno de afuera.
