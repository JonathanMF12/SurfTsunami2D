# Surfista1 — Movimiento, física, animación y escena de inicio

## ¿Qué se creó / cambió?

| Archivo | Qué hace |
|---|---|
| `Assets/SurfistaController.cs` | Movimiento del jugador, límites dentro de la cámara, salto en la copa de la ola, daño/vidas y **caída (game over)**. Usa el nuevo Input System. |
| `Assets/AnimadorSurfista.cs` | Anima los sprites de **surf** (bucle), **salto** y **caída**. No hace falta crear un Animator. |
| `Assets/AutoConfiguradorJuego.cs` | Carga sola los fotogramas de las carpetas, ajusta el tamaño del surfista y su colisionador. |
| `Assets/MenuInicio.cs` | **Nuevo.** Script de la escena de inicio (fondo + "PRESIONA UNA TECLA"). |
| `Assets/Scenes/Inicio.unity` | **Nueva.** Escena de inicio, ya puesta como primera escena del juego. |
| `Assets/GameManager.cs` | Reiniciar con **R** ahora usa el nuevo Input System. |

> ⚠️ El proyecto está configurado con **solo el Input System nuevo** (`activeInputHandler: 1`),
> por eso los scripts usaban `Input.GetAxisRaw(...)` y fallaban. Ahora lee el teclado/mando
> con `Keyboard.current` y `Gamepad.current`.

## Sprites del surfista (¡importante!)

Las imágenes originales (`surfistas.png`, `caida.png`) venían **con fondo de cuadritos
grises dibujado dentro del PNG**, así que no se podían usar tal cual. Se separaron
automáticamente en fotogramas con fondo transparente:

- `Assets/Resources/Surfista1/Surf/surf_00.png` … `surf_19.png` → **20 fotogramas de surf**.
- `Assets/Resources/Surfista1/Caida/caida_00.png` → **imagen de caída** (game over).

`parado.png` **no se usó** (es para tu escena previa, como querías).

> ❗El orden de los fotogramas es el de lectura (fila por fila) y la carpeta `Caida` se tomó
> como **una sola imagen**. Si el orden no queda bien o la caída tenía varios fotogramas,
> se ajusta fácil desde el Inspector (ver más abajo).

### Cómo se rellenan los fotogramas

El `AutoConfiguradorJuego` los carga solo al dar Play (buscando en las carpetas
`Surf` y `Caida` dentro de `Assets/Resources/Surfista1`). Funciona también en las
compilaciones. No tienes que arrastrar nada. Si quieres otro orden, hazlo a mano:

1. Selecciona `Surfista1` → componente **Animador Surfista**.
2. Arrastra las imágenes sobre **“Fotogramas Surf”** / **“Fotogramas Caida”**.
3. Ajusta `fpsSurf`, `fpsSalto` y `fpsCaida`.

El surfista se reescala solo a ~1.6 unidades de alto y su colisionador se ajusta al sprite,
así que da igual si los fotogramas son de 240 px (surf) o de ~1000 px (caída).

## Teclas

- **Mover:** `W A S D` o flechas (también stick izquierdo / cruceta del mando).
- **Saltar:** `Espacio` o botón sur del mando. Además, **salta solo** al llegar a la copa de la ola subiendo.
- **Reiniciar (al perder):** `R`.

## Cómo probarlo (rápido)

1. Abre el proyecto en Unity y dale **Play**. Se carga primero `Inicio` (la pantalla de título):
   pulsa cualquier tecla para pasar al juego.
2. En la escena del juego, mueve al surfista: debe quedarse dentro de la pantalla, no caerse,
   y saltar al subir a la cresta.
3. Al perder las 3 vidas debe verse la **animación de caída**.

## Escena de inicio (`Assets/Scenes/Inicio.unity`)

Ya está creada con: cámara, luz 2D, un objeto `Fondo` (SpriteRenderer) y el script `MenuInicio`.
Está registrada como **primera escena** en `File > Build Settings`.

Para poner tu imagen de fondo:

1. Importa tu imagen a `Assets/Sprites/...`.
2. Selecciona el objeto **`Fondo`** en la jerarquía y arrastra tu imagen a su `Sprite`,
   **o** selecciona **`MenuInicio`** y arrástrala al campo **“Sprite Fondo”**.
3. Si quieres otro texto o escena de destino, edita **`MenuInicio`** en el Inspector
   (`mensajeInicio` y `nombreEscenaJuego`).

## Ajustar el área de juego (la foto `area-de-juego-surfista`)

Para poder afinar los límites con el Inspector y ver el rectángulo en la vista *Scene*:

1. En la jerarquía selecciona **`Surfista1`**.
2. **Add Component → Surfista Controller** (y, si quieres ver la animación editable,
   **Add Component → Animador Surfista**).
3. En el Inspector verás el grupo **“Área de juego”** y un rectángulo verde en la *Scene*:
   - `minY` = borde inferior (donde rompe la ola).
   - `maxY` = **copa de la ola** (donde debe saltar).
   - `usarAnchoDeCamara` = mantiene los límites laterales pegados al encuadre de la cámara.

## Criaturas marinas (delfín, tiburón y bañista)

La animación por fotogramas de los enemigos está documentada aparte en
[`INSTRUCCIONES_CRIATURAS.md`](INSTRUCCIONES_CRIATURAS.md).
