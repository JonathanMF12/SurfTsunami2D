# HUD del juego — Vidas, Puntaje y Game Over

Sistema de vidas y puntaje conectado a las imágenes de `Assets/Sprites/mensajes`.

## ¿Qué se creó / cambió?

| Archivo | Qué hace |
|---|---|
| `Assets/HUDMensajes.cs` | **Reescrito.** Construye y actualiza el HUD: 3 tablas de surf (vidas), el número del puntaje y el panel de **Game Over**. |
| `Assets/Scenes/SampleScene.unity` | El componente **HUD Mensajes** se añadió al objeto **`Canvas`** y ya tiene los sprites asignados. |
| `Assets/AutoConfiguradorJuego.cs` | Ahora configura la escena de juego **cada vez que se carga** (antes solo la primera vez, por lo que al reiniciar con **R** el surfista perdía sus scripts). |

> ℹ️ En la carpeta `mensajes` **no había ningún script de "Game Over"**: lo que hay es la
> imagen `game over.png` (una hoja de sprites). El cartel de Game Over se implementó con esa
> imagen dentro del panel transparente.

## ¿Qué hace el HUD?

1. **Vidas (3 tablas de surf).** Se muestra una tabla por cada vida, con la palabra
   **VIDAS** a su izquierda. Al recibir un golpe desaparece una. Usa los sub-sprites
   `vidas_0` (la palabra "VIDAS") y `vidas_2`, `vidas_3` y `vidas_4` (las tablas de surf
   de `vidas.png`).
2. **Puntaje.** Muestra la palabra **PUNTAJE** y, debajo, el número que crece al esquivar
   al pulpo, delfín, bañista y tiburón (y al recoger las tablas de bonus).
3. **Game Over.** Al quedarte sin vidas aparece un **cuadrado negro semitransparente** a
   pantalla completa con el cartel **GAME OVER** (grande), la palabra **PUNTAJE**, el
   **número final** y el texto *"Pulsa R para volver a surfear"*. Pulsando **R** se reinicia.

## Cómo está montado

- El componente vive en el objeto **`Canvas`** de `SampleScene`.
- En el Inspector verás los campos:
  - **Sprites de las vidas (tablas de surf):** las 3 tablas (`vidas_2/3/4`).
  - **Sprites del puntaje y Game Over:** la palabra "PUNTAJE"
    (`puntaje_0`) y el cartel "Game Over" (`game over_0`).
  - **Etiquetas del HUD en juego (PUNTAJE / VIDAS):** la palabra "VIDAS"
    (`vidas_0`) y la escala (`escalaEtiquetaHud`) de ambas palabras durante la partida.
  - **Posición y tamaño del HUD:** con `anclarArriba` activado el HUD se pega a la parte
    **superior** de la pantalla (vidas a la derecha, puntaje a la izquierda). Ajusta la
    altura con `margenSuperior` y la distancia a los lados con `margenLateral`. Si lo
    desactivas, vuelve al modo centrado antiguo (`posicionVidas` / `posicionPuntaje`).
  - **Game Over:** color del cuadrado transparente, escalas y tamaño de la fuente final.

> ⚠️ El HUD se coloca arriba por código (`HUDMensajes`). También **oculta** los objetos
> `score` / `vidas` que quedaron en la escena de una versión anterior, para no dibujar el
> HUD dos veces.

## Ajustarlo a tu gusto

1. Selecciona el objeto **`Canvas`** en la jerarquía.
2. En el componente **HUD Mensajes** cambia:
   - `anclarArriba` / `margenSuperior` / `margenLateral`: dónde queda el HUD arriba.
   - `posicionVidas` / `separacionVidas` / `escalaVidas`: dónde y cómo de grandes salen las tablas (modo centrado).
   - `posicionPuntaje` / `tamanoFuentePuntaje`: posición y tamaño del número del marcador (modo centrado).
   - `colorFondoGameOver`: qué tan oscuro es el cuadro transparente del Game Over.
   - `escalaGameOver` / `escalaEtiquetaPuntaje`: tamaño del cartel del Game Over.
   - `escalaEtiquetaHud`: tamaño de las palabras "PUNTAJE" y "VIDAS" durante la partida.
3. Los sprites se pueden **arrastrar** en el Inspector. Si se dejan vacíos, el script intenta
   cargarlos solo desde `Assets/Resources/mensajes`.

## Música

| Archivo | Qué hace |
|---|---|
| `Assets/GeneradorMusica.cs` | Compone por código los temas (estilo chiptune): el **tema del juego** (rápido y alegre) y el jingle cómico de **Game Over** ("perdiste"). |
| `Assets/MusicaFondo.cs` | Suena en todas las escenas. Usa el tema generado (o tu archivo) y al perder cambia al jingle de Game Over; al reiniciar (R) o volver al menú recupera el tema del juego. |

- La velocidad del tema principal se ajusta con el campo `bpm` de **MusicaFondo** (a más `bpm`,
  más rápido y movido).
- Para usar tu propia música: mete un archivo en `Assets/Resources/Audio` llamado `musica`
  (o arrástralo en `clipPersonalizado`). Si hay archivo propio, se deja de generar y el
  Game Over mantiene tu música.
- El botón **MÚSICA** del panel OPCIONES de la portada enciende/apaga la música (se recuerda
  entre partidas).

## Cómo probarlo (rápido)

1. Abre `SampleScene` y dale **Play**.
2. Arriba verás las **3 tablas de surf** y el **puntaje**.
3. Esquiva criaturas: el puntaje sube. Choca 3 veces: las tablas desaparecen una a una y al
   final sale la pantalla de **Game Over** con tu puntaje.
4. Pulsa **R**: la partida vuelve a empezar completa (vidas y puntaje a 0).
