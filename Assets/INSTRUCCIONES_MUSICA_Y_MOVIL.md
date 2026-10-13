# Música y controles para móvil

Dos añadidos al juego: **música de fondo** (con tema generado por código y soporte para tu
propio archivo) y **controles táctiles** para móvil/tablet. **La jugabilidad no cambia**: el
surfista se mueve igual, solo cambia *cómo se le da la orden*.

## ¿Qué se creó / cambió?

| Archivo | Qué hace |
|---|---|
| `Assets/MusicaFondo.cs` | **Nuevo.** Reproductor de música en bucle que se crea solo, sobrevive a los cambios de escena y guarda ON/OFF. |
| `Assets/GeneradorMusica.cs` | **Nuevo.** Compone un tema playero/chiptune en bucle por código (por si no pones un archivo de audio). |
| `Assets/ControladorTactil.cs` | **Nuevo.** Joystick flotante + botón de salto en pantalla, solo para móvil/táctil. |
| `Assets/SurfistaController.cs` | Lee también el joystick táctil y el botón de salto. |
| `Assets/HUDMensajes.cs` | En Game Over añade botones **REINTENTAR** y **MENÚ** cuando hay soporte táctil. |
| `Assets/MenuInicio.cs` | Nuevo botón **MÚSICA: ON/OFF** en OPCIONES y textos de ayuda adaptados al táctil. |
| `Assets/AutoConfiguradorJuego.cs` | Crea los controles táctiles al cargar la escena de juego. |
| `Assets/Scenes/SampleScene.unity` | El Canvas ahora escala con la pantalla (se ve bien en móvil). |
| `ProjectSettings/ProjectSettings.asset` | En móvil el juego se bloquea en **horizontal**. |

---

## 🎵 Música

### Cómo funciona

- Al darle **Play** se crea solo un objeto `MusicaFondo` que **no se destruye** al cambiar de
  escena: el tema sigue sonando del menú al juego y al reiniciar con **R**.
- Primero intenta cargar **tu archivo**. Si no existe, **compone un tema playero en bucle por
  código** (bajo, melodía, acordes y percusión). Así el juego nunca se queda en silencio.

### Usar TU propia música

1. En `Assets` crea la carpeta `Resources` (si no existe) y dentro otra llamada `Audio`.
2. Mete tu archivo ahí con el nombre **`musica`** (por ejemplo `musica.ogg`, `musica.mp3`
   o `musica.wav`).
3. Listo: el juego lo detecta y deja de usar el tema generado.

> Ruta final: `Assets/Resources/Audio/musica.ogg`

### Encender / apagar y volumen

- En la portada: **OPCIONES → MÚSICA: ON/OFF**. Se guarda solo (PlayerPrefs).
- El botón **SONIDO** de OPCIONES sigue controlando el volumen general
  (`AudioListener.volume`), y afecta a la música además de los efectos.
- Para cambiar el **volumen** del tema: selecciona el objeto `MusicaFondo` en Play y ajusta
  `Volumen` (o el campo homónimo en el script).

---

## 📱 Controles en móvil

### Cómo se juega

- **Joystick flotante:** toca con el pulgar en la **mitad izquierda** de la pantalla donde te
  resulte cómodo y **arrastra** para mover al surfista en las 4 direcciones. Suelta para parar.
- **Salto:** botón **SALTO** abajo a la derecha (o toca en cualquier punto de la **mitad
  derecha**). Se puede saltar y moverse a la vez (multitáctil).
- El salto automático en la copa de la ola sigue funcionando igual.

En **Game Over** aparecen dos botones porque en el móvil no hay tecla **R**:
**REINTENTAR** (reinicia la partida) y **MENÚ** (vuelve a la portada).

### ¿Cuándo aparecen los controles?

Aparecen **solos** si la plataforma es móvil o hay una pantalla táctil. En PC quedan ocultos
y se juega con teclado/mando como siempre.

### Probarlos en el editor (PC)

Dos formas:

1. Selecciona el objeto **`ControlesTactiles`** en la jerarquía durante el Play y marca
   **`Mostrar En Escritorio`**.
2. O durante el Play pulsa la tecla **T** para mostrar/ocultar los controles.

Con los controles visibles, en el editor puedes usar el **ratón**: mitad izquierda = joystick,
mitad derecha o botón = salto.

> Para probar los botones de Game Over en PC, activa **`Mostrar Botones Tactiles En
> Escritorio`** en el componente **HUD Mensajes** del objeto `Canvas`.

---

## 📱 Compilar para móvil

1. Instala el módulo **Android** (o **iOS**) desde Unity Hub.
2. `File → Build Settings`: añade las escenas en este orden:
   - `Assets/Scenes/Inicio.unity`
   - `Assets/Scenes/SampleScene.unity`
3. Elige **Android** → **Switch Platform** → **Build** (o **Build And Run** con el móvil
   conectado).
4. La orientación ya está configurada en **horizontal**; en el proyecto va incluido el nuevo
   **Input System**, que es compatible con táctil.

## Notas

- Los controles táctiles **solo aparecen cuando toca**: no estorban en PC ni en el menú.
- El juego se pensó para **horizontal (landscape)**. En vertical la zona de juego quedaría
  incómoda, por eso se bloqueó la rotación vertical en móvil.
- Si algún día quieres cambiar el tamaño del joystick o del botón, se ajustan en el
  Inspector del objeto **`ControlesTactiles`** (`radioJoystick`, `diametroBotonSalto`, etc.).
