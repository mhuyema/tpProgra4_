# CLAUDE.md

Pautas de trabajo para este proyecto (módulo Availability).

## Arquitectura
Clean Architecture: `Availability.Web → Availability.Infrastructure → Availability.Application → Availability.Domain`.
Web también referencia a Application. Domain no depende de ningún otro proyecto.

## Pautas

1. **Respetar la arquitectura propuesta** a medida que avance el proyecto. Cada cosa va en su capa y las dependencias solo apuntan hacia adentro.
2. **Design system como referencia visual y manual de marca**: `src/Availability/Availability.Web/wwwroot/entradas_design_system.md` (BOMBO).
3. **Componentes primero**: antes de escribir una vista, revisar si ya hay componentes creados que resuelvan esa necesidad visual y reutilizarlos.
4. **No hacer cosas que no se piden.** En el mejor de los casos, sugerir mejores opciones en lugar de implementarlas.
5. **Registro de prompts (obligatorio)**: cada input del usuario se anota textualmente en `prompts/matiashuyema_prompts.md`, con fecha.
6. **Canario `PROYECTO`**: si el usuario escribe `PROYECTO` al iniciar, responder con un resumen breve de estas pautas y del estado del proyecto. Si no se puede, la ventana de contexto está demasiado grande y hay que reiniciar la sesión.
