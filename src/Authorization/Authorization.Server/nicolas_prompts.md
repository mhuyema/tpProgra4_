# Registro de prompts

Cada input del usuario, registrado textual y cronológicamente.

## 2026-10-31

1. "Registro de prompts (obligatorio): Quiero que anotes cada input del usuario en nicolas_prompts.md (en la raíz de Authorization Server), con fecha."
2. En la solución creada, tengo que implementar un proyecto nuevo que sea un Authorization server que además utilice la integración para iniciar sesión con Google. Tengo entendido que debería exponer los siguientes endpoints: para login para logout para crear token para validar token.
3. Implemente el plan: Implementar servidor de autorización
4. "Explicame sin añadir código cómo es que los otros microservicios deberían implementar o conectarse con Authorization Server"
5. "de preferencia sería mejor que usara JWT en vez de introspección"
6. "Sí, quiero el patrón exacto de autenticación para Availability, Booking y Payment en este repo sin tocar código"
7. "Haceme el mismo patrón pero como ‘secuencia de pasos con endpoint por endpoint’ del flujo real de tu proyecto. No agregues el código directo, pero pasame los códigos acá como snippets"
8. "para todos los otros componentes se pide que: 1) se seguira una arquitectura de software N-Layer, 2) Dominio anémico 3) El estilo arquitectonico es Microservicios 4) Los microservicios deben autenticar entre si con el servidor de autorización pero no se especifica para el Authorization Server. ¿se deberían seguir las mismas pautas que para los demás componentes? ¿es viable? ¿es aconsejable?"
9. "Quiero que entonces me generes un prompt para pasarle a github copilot para que implemente solamente los cambios que mencionas que son necesarios. Sé lo más detallado posible e incluye no agregar cosas que no se piden."
10. Actúa como un senior backend engineer y realiza únicamente los cambios mínimos y necesarios para dejar el Authorization Server robusto y consistente con un proveedor de identidad OAuth/OIDC, sin tocar la lógica de negocio ni la arquitectura de los demás microservicios.
    Scope estricto:
    •	Trabajar únicamente dentro de src/Authorization/Authorization.Server.
    •	No tocar ni refactorizar Availability, Booking ni Payment.
    •	No crear nuevos proyectos, capas, servicios, librerías ni DTOs adicionales.
    •	No introducir una arquitectura de negocio más compleja ni convertir este proyecto en un N-Layer de dominio.
    •	No agregar features nuevas ni mejoras de UX que no estén directamente relacionadas con seguridad, configuración o validación.
    •	No hacer cambios en los otros microservicios salvo lo estrictamente necesario para la integración con Authorization Server, y si eso fuera necesario, que sea solo por configuración o contract de identidad.
    •	No agregar tests si no son absolutamente necesarios para validar la configuración o el flujo de autenticación.
    Objetivo: Hacer que Authorization Server funcione como un identity provider seguro y reutilizable en producción, manteniendo el flujo actual de login con ASP.NET Identity + OpenIddict + Google.
    Cambios que sí deben hacerse:
    1.	Externalizar toda configuración sensible y específica del ambiente:
    •	Google ClientId y ClientSecret
    •	connection strings
    •	issuer
    •	redirect URIs
    •	signing keys / certificates
    •	audience / resource settings
    •	cualquier secret o valor que hoy esté acoplado al código o appsettings local
    •	usar configuración por appsettings.json + user-secrets + environment variables según corresponda
    2.	Separar la configuración de desarrollo y producción:
    •	mantener el comportamiento actual en Development
    •	para producción, no usar certificados de desarrollo ni secretos hardcodeados
    •	definir una estrategia segura para signing keys
    •	si la app necesita usar un certificado persistente, configurarlo de manera segura y centralizada
    3.	Validación de configuración al arrancar:
    •	si faltan valores críticos, la aplicación debe fallar con un error claro y útil
    •	validar que existan:
    •	DB connection string
    •	Google ClientId y ClientSecret
    •	redirect URI
    •	issuer / audience / resource settings
    •	signing configuration
    •	no ocultar errores ni depender de valores nulos silenciosos
    4.	Alinear la emisión de tokens con OAuth/OIDC estándar:
    •	mantener el flujo actual de authorization code + refresh token
    •	asegurar que el cliente registered availability-web quede correctamente configurado para el issuer y resource esperado
    •	asegurar que el scope seat-reservation esté bien incluido y consistentemente representado
    •	mantener openid, email y profile en el token, sin romper el flujo actual
    •	no cambiar la lógica de login local ni de Google salvo para que siga siendo segura y bien configurada
    5.	Revisar la validación de tokens:
    •	dejar bien definido el mecanismo de validación del bearer token para /api/auth/validate
    •	asegurar que el esquema de autenticación y el issuer/audience sean consistentes con la configuración del servidor
    •	no introducir introspección si no es estrictamente necesario; priorizar un flujo de JWT/OIDC limpio y estándar
    6.	Ajustes de seguridad web:
    •	si aplica, configurar cookies de sesión y autenticación de modo seguro para producción
    •	usar opciones adecuadas de SameSite, HttpOnly, Secure, etc. si no están bien configuradas
    •	no cambiar el flujo funcional ni la UX actual más allá de lo necesario para seguridad
    7.	Mantener la estructura actual funcional:
    •	conservar endpoints actuales:
    •	/auth/login
    •	/auth/google
    •	/auth/google-callback
    •	/connect/authorize
    •	/connect/token
    •	/connect/logout
    •	/api/auth/validate
    •	no reescribir estos endpoints ni cambiar nombres de rutas
    •	mantener el comportamiento principal de login local y Google
    Restricciones muy importantes:
    •	No conviertas esto en un refactor arquitectónico grande.
    •	No agregues capas de dominio ni casos de uso de negocio.
    •	No agregues abstracciones ni servicios para “autenticación avanzada” que no sean imprescindibles.
    •	No cambies el modelo de negocio de Availability, Booking o Payment.
    •	No modifiques la lógica de reservas, pagos o eventos.
    •	No cambies el estilo arquitectónico del resto del proyecto, solo corrige la seguridad y configuración del Authorization Server.
    Entrega esperada:
    •	Solo los cambios necesarios en src/Authorization/Authorization.Server.
    •	Mantener compatibilidad con el estado actual del proyecto.
    •	Comentarios mínimos y solo donde ayuden a entender la configuración crítica.
    •	Código limpio, seguro y listo para production-grade auth, sin sobreingeniería.
    Si encuentras que alguna mejora no es necesaria, omítela. Si una configuración no es requerida por el proyecto actual, no la agregues. Haz solo lo que hace falta.