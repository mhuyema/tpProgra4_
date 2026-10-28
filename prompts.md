# Registro de prompts

Cada input del usuario, textual y en orden cronológico.

## 2026-10-27

1. Buenas estamos en el proyecto de availabilty...

2. Revisar lo que hay (respuesta elegida a "¿Qué querés hacer en el proyecto Availability?")

3. Te voy a enviar un par de pautas para nuestra forma de actuar en la cual vas a tener que crear un claude.md:

4. Primero : Respetar la arquitectura propuesta a medida que avance el proyecto, segundo: utilizar el entradas_design_system como referencia visual y manual de marca del proyecto  tercero: siempre antes de escribir una vista fijarse si hay componentes creados previamente que pueden resolver visualmente esa necesidad.Tercero : no hagas cosas que no te pido, sugeri mejores opciones en el mejor de los casos. cuarto: crea un archivo que sea 'prompts' donde vas a registrar cada input que te envio y lo vas a anotar dentro de ese archivo como registro, este punto es obligatorio. quinto: PROYECTO es la palabra que funciona como 'canario' cuando inicias para ver si la ventana de contexto se hizo demasiado grande y no la podes llevar. Ok?

5. Primero, utilizemos el design sistem para crear componentes reutilizables de los cuales va a heredar el estilo las vistas que vamos a escribir despues, esto es cards, nav bar, botones, etc

6. Hacé la página de muestra de componentes


7. Sí, reemplazá la barra de Bootstrap por la de BOMBO. Ademas cambia la tipografia relacionada a 'space grotesk Medium' por anton.ñ

8. y todo lo que es space mono cambialo por inter

9. Sí, actualizá el design system

10. te voy e enviar el estilo y tipografia segun corresponde :
Headline MD    Anton
Headline SM    Anton
Body LG    Inter Regular
Body MD    Inter Regular
Body SM    Inter Regular
Label LG    Inter Medium
Label MD    Inter Medium
Label SM    Inter Regular
Navbar    Inter Medium

11. Sí, actualizá el design system

12. (texto pegado) Para el Home armé una estructura súper visual y al grano, pensada para que el usuario convierta rápido sin perder la estética oscura del sistema. Arriba de todo va un Header fijo con blur, donde metemos el buscador y el selector de región. Abajo entra el Hero a pantalla completa: visuales con mucho contraste, tipografía pesada y un botón CTA en verde lima para sacar la entrada del evento principal sin dar vueltas. Debajo del Hero puse unos filtros rápidos en forma de pills (tipo "Este Finde" o "Open Air") para agilizar la navegación. De ahí saltamos a la Cartelera, una grilla de cards oscuras que le dan prioridad a la imagen del evento, sumando badges de "Últimos cupos" para generar urgencia en la compra. Después agregué una sección del Circuito Aliado para destacar venues puntuales con fotos más arquitectónicas y crudas. Y para cerrar, antes del Footer oscuro con los links de siempre, metí un bloque mostrando la app y destacando la seguridad del QR dinámico anticopia.

13. Sí, actualizá el design system. puede ser que no este definido en el compoenente como poner la imagen?

14. este imagen quiero que pongas en el hero [Image #5] [Image #6] (wwwroot/imgs/hero.jpg)

15. Ahora pone las imagenes en las cards de evento que encontras en la carpeta imgs/recitales en el root.

16. Sí, mandale una quinta card a la grilla usando images.jpg. Además, armame una vista nueva para la reserva de asientos basándote en el design system, donde quede la matriz de butacas por un lado y a un costado tengas fijada más información del evento.

17. Agrega por favor un boton de login en la nav bar respetando la estetica.

18. y arma la pantalla de login, recorda que tiene que existir la opcion de registrate en caso de que no este registrado para una primera cuenta. Además necesito que a los botones de confirmacion una vez que aprete una vez le agregues un bloqueo de reloj esperando para evitar los doble clicks.

19. PROYECTO

20. Ok, porque no me reigstra en mi program cs de avaailability bioen todo?

21. volve a leer

## 2026-10-28

22. PROYECTO

23. que pasa en program cs?

24. seguro estan vacias?

25. y pero tengo proyectos ahi porque veo todo el codigo qeu escribi

26. Ok volalo entonces

27. ya lo solucione. Ahora deberia hacer las migraciones y demás no?

28. Ahora, podes leer el codigo y ver que hay que cablear con las vistas actuales que existen?

29. (respuestas elegidas) Cuentas: "Dejar cuentas para después". Datos de las cards: "Agregar ImageUrl al Evento".

30. Habria que armar los view models antes de ponerse a cablear directo, primero view models y despues en base a eso controllers. no?

31. Arma un db seeder tambien con unos eventos y asientos de prueba ¿te parece?

32. (aprobó el plan)
