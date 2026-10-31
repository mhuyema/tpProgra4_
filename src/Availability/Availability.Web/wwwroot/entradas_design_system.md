# BOMBO - Techno Clubbing & Ticketing Design System

Este documento define el sistema de diseño oficial de **BOMBO**. Está construido para curadores, productores y devotos de la música electrónica en América Latina. La interfaz opera bajo una filosofía exclusiva de "modo nocturno" (Dark Mode) que prioriza la tensión atmosférica y la convicción tipográfica extrema.

---

## 1. Filosofía de Marca y Estilo

El estilo visual fusiona el **Neo-Brutalismo** con el **Minimalismo Industrial de Club**:

*   **Arquitectura Exclusiva Nocturna (Night-Only):** Los fondos son tonos negros puros y de vacío, imitando el entorno sensorial de las raves en almacenes, clubes iluminados con luces estroboscópicas y cabinas de sonido con poca luz.
*   **Acentuación Verde Ácido (Electric Acid):** Un tono verde lima-chartreuse impacta la interfaz con claridad de alto voltaje, entregando jerarquía funcional inmediata, llamadas a la acción cinéticas y credenciales de la cultura rave.
*   **Escala Display Pesada:** Tipografía masiva, ultranegrita y condensada que ofrece declaraciones contundentes, apoyada por etiquetas de metadatos monoespaciadas y técnicas que evocan sintetizadores modulares y setlists impresos.
*   **Utilidad Táctil:** Geometrías fluidas tipo "píldora" (pills), micro-bordes sutiles y controles interactivos de alto rendimiento garantizan una reserva de entradas sin fricciones en dispositivos móviles dentro de entornos oscuros y de alto estímulo.

---

## 2. Design Tokens (Fundamentos)

### 2.1. Paleta de Colores

La paleta está diseñada exclusivamente para ejecución en modo oscuro para conservar la vista bajo condiciones de baja iluminación.

**Acentos y Primarios:**
*   `primary-fixed`: **#c8f250** (Verde Lima Ácido - La baliza sónica característica. Se usa para CTA principales, estados activos y brillos).
*   `on-primary-fixed`: **#161f00** (Texto oscuro sobre el verde lima).
*   `primary`: **#ffffff** (Blanco puro para titulares pesados y legibilidad estructural).

**Superficies y Fondos (Void & Charcoal):**
*   `background`: **#131313** (Fondo general / Lienzo infinito).
*   `surface-container-lowest`: **#0e0e0e** (Modales, Navbar principal, los elementos más profundos).
*   `surface-container-low`: **#1c1b1b** (Tarjetas de eventos, contenedores principales).
*   `surface-container`: **#201f1f** (Botones secundarios, slots inactivos).
*   `surface-container-high`: **#2a2a2a** (Estados hover, elementos interactivos elevados).
*   `surface-container-highest`: **#353534** (Bordes de máxima elevación).

**Texto y Metadatos (Carbon & Muted):**
*   `on-surface`: **#e5e2e1** (Texto de cuerpo principal).
*   `on-surface-variant`: **#c5c9b0** (Texto secundario, descripciones cortas).
*   `secondary`: **#c6c6c7** (Íconos inactivos, placeholders, divisores).

**Estados y Feedback:**
*   `error`: **#ffb4ab**
*   `on-error`: **#690005**

### 2.2. Sistema Tipográfico

El sistema comunica autoridad, audacia de la cultura rave y precisión técnica.

**1. Display & Titulares (Outfit y Anton)**
Impacto contundente, preferentemente en mayúsculas (Uppercase). Los titulares grandes usan Outfit con pesos pesados y tracking negativo; los medianos y chicos usan Anton, que solo existe en peso Regular (400).
*   `headline-xl` (Outfit): 72px / Line Height 74px / Extra-Black (900) / Tracking -0.03em
*   `headline-lg` (Outfit): 48px / Line Height 52px / Extra-Bold (800) / Tracking -0.025em
*   `headline-md` (Anton): 28px / Line Height 34px / Regular (400) / Tracking -0.015em
*   `headline-sm` (Anton): 22px / Line Height 28px / Regular (400) / Tracking -0.01em

**2. Cuerpos de Texto (Inter)**
Legibilidad excepcional para descripciones de eventos.
*   `body-lg`: 18px / Regular (400)
*   `body-md`: 15px / Regular (400)
*   `body-sm`: 13px / Regular (400)

**3. Etiquetas y Micro-Copy (Inter)**
Sans-serif, con amplio tracking, siempre en MAYÚSCULAS. Inspirada en sintetizadores y pulseras de festivales.
*   `label-lg`: 14px / Medium (500) / Tracking 0.06em
*   `label-md`: 12px / Medium (500) / Tracking 0.08em
*   `label-sm`: 10px / Regular (400) / Tracking 0.1em

**4. Navegación (Inter)**
*   Links del navbar: Inter Medium (500), con el estilo de `label-md`.

### 2.3. Radios y Formas (Shapes)

Alto contraste entre puntos de contacto interactivos tipo píldora (Pill) y bloques estructurales geométricos.

*   `rounded-full` (9999px): **Pill Philosophy**. Usado para todos los botones principales interactivos, chips, selectores y badges.
*   `rounded-DEFAULT` (1rem / 16px): Tarjetas de información y bloques menores.
*   `rounded-xl` (3rem / 48px): Tarjetas de eventos masivas, para retener las esquinas suaves pero estructurales preservando el arte del póster.

---

## 3. Elevación y Profundidad (Depth)

En una estética de club negro intenso, las sombras paralelas convencionales se ven planas. La profundidad se construye mediante:

1.  **Capas Tonales:** Desde el vacío `#131313` escalando hacia `#1c1b1b` y `#2a2a2a`.
2.  **Bordes Rasurados (Razor Outlines):** Los contenedores elevados usan bordes nítidos de 1px con muy baja opacidad (ej. `rgba(255,255,255, 0.08)`) para definir límites geométricos.
3.  **Difusión de Luz Ácida (Acid Glow):** Los estados activos e interacciones de hover introducen un resplandor utilizando el verde `#c8f250` con desenfoque (blur radius) para simular láseres cortando el humo del club.
4.  **Efecto Vidrio Esmerilado (Backdrop Frosting):** Barras de navegación fijas y docks de pago usan un desenfoque pesado (`backdrop-blur-xl`) con un color de fondo translúcido.

---

## 4. Componentes UI

### 4.1. Botones (Buttons)
*   **Acción Principal (Primary CTA):** Forma de píldora completa, relleno sólido verde ácido (`bg-primary-fixed`), texto negro oscuro (`text-on-primary-fixed`) en fuente `Outfit` o `Inter` Uppercase. Al hacer *hover*, escala ligeramente (`scale-105`) e irradia un brillo de neón (`shadow-[0_0_24px_rgba(200,242,80,0.35)]`).
*   **Acción Secundaria (Outline/Ghost):** Píldora con borde sólido de 1.5px (ej. `border-primary/40`), texto blanco. Al hacer *hover*, el fondo cambia a un blanco o verde translúcido (`hover:bg-primary/10`).

### 4.2. Entradas y Búsqueda (Inputs)
*   **Buscador Principal (Hero Search):** Forma de píldora alargada, fondo `#1c1b1b`, borde de 1px interno (`shadow-[inset_0_0_0_1px_rgba...]`). El texto del placeholder va en `Inter` gris. En foco, el borde se vuelve verde lima.

### 4.3. Tarjetas de Eventos (Event Cards)
*   **Estructura:** Contenedores verticales u horizontales (`bg-surface-container-low`), bordes de 1.5rem a 2rem (`rounded-2xl`).
*   **Fotografía:** Elementos visuales profundos y saturados con un degradado lineal negro inferior (`bg-gradient-to-t from-background`) para asegurar el contraste de la tipografía superpuesta.
*   **Badges:** Ubicados flotando en las esquinas, con forma de micro-píldora, mostrando disponibilidad (ej. "SOLD OUT" o "LAST TICKETS").

### 4.4. Etiquetas y Filtros (Chips)
*   Píldoras compactas con tipografía `Inter`.
*   *Inactivo:* Fondo de superficie con texto gris/blanco.
*   *Activo:* Fondo verde lima con texto negro oscuro.

### 4.5. Indicador En Vivo (Live Pip)
*   Punto circular verde lima `#c8f250` con animación de pulso (`animate-pulse` / ping), utilizado para indicar el estado activo de venta o sets en vivo, acompañado de texto `label-sm` en mayúsculas.
### 4.6. Navbar / Header
*   Fijo (`sticky`) con fondo `surface-container-lowest` translúcido y desenfoque (`backdrop-blur-xl`), borde inferior de 1px.
*   Contiene la marca, el buscador (4.2), un selector de región en píldora y los links en `Inter` Medium, mayúsculas. El link activo va en verde lima.
*   Componente: `_Navbar` (`NavbarModel`: `ShowSearch`, `Regions`).

### 4.7. Hero
*   Sección a pantalla completa con imagen de fondo, degradado negro inferior y contenido alineado abajo: indicador en vivo (opcional), meta en `label-lg` verde lima, titular `headline-xl`, descripción `body-lg` y botón primario.
*   Componente: `_Hero` (`HeroModel`).

### 4.8. Venue Cards (Circuito Aliado)
*   Tarjeta de 16px de radio con foto **en blanco y negro y alto contraste** (aspecto arquitectónico y crudo), degradado inferior, nombre en `headline-sm` y ubicación en `label-md` verde lima.
*   Componente: `_VenueCard` (`VenueCardModel`).

### 4.9. Bloque de App
*   Tarjeta de 48px de radio con mensaje de seguridad del QR dinámico anticopia, dos botones (primario y secundario) y una tarjeta con QR e indicador en vivo.
*   Componente: `_AppPromo`.

### 4.10. Footer
*   Fondo `surface-container-lowest`, borde superior de 1px, marca, links y copyright en `label-sm`.
*   Componente: `_Footer` (`FooterModel`).

---

## 5. Imágenes

Todo componente con foto (`_EventCard`, `_Hero`, `_VenueCard`) recibe la imagen como una URL en su modelo (`ImageUrl`).

*   **Ubicación:** los archivos viven en `wwwroot/imgs/` y se pasan como ruta relativa (ej. `/imgs/eventos/warehouse-ritual.jpg`).
*   **Encuadre:** la imagen se renderiza como `<img>` absoluto que llena el contenedor (`object-fit: cover`), por lo que se recorta sin deformarse. Conviene subir imágenes verticales para las tarjetas de evento y horizontales para el Hero.
*   **Contraste:** el degradado inferior a `background` lo aplica el componente; no hace falta oscurecer la foto a mano.
*   **Venues:** el filtro blanco y negro lo aplica el componente por CSS; se sube la foto a color.
*   **Texto alternativo:** se toma del título o nombre del modelo.
*   **Mientras no haya fotos:** `ImagePlaceholder.Gradient(...)` genera un degradado de relleno.
