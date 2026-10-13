# Criaturas marinas — animación por fotogramas (Delfín, Tiburón y Bañista)

Continuación del pulpo. Ahora **el delfín, el tiburón y el bañista también se animan
por fotogramas**, igual que el pulpo: no hace falta crear ningún *Animator*, el script
va cambiando el sprite en bucle.

## ¿Qué se creó / cambió?

| Archivo | Qué hace |
|---|---|
| `Assets/AnimadorCriaturaMarina.cs` | **Nuevo.** Animación por fotogramas del delfín, tiburón y bañista. Carga solo los PNG de `Assets/Resources` y da el tamaño adecuado. |
| `Assets/Resources/Delfin/delfin_00.png` … `delfin_05.png` | **6 fotogramas** del delfín. |
| `Assets/Resources/Tiburon/tiburon_00.png` … `tiburon_03.png` | **4 fotogramas** del tiburón. |
| `Assets/Resources/Banista/banista_00.png` … `banista_07.png` | **8 fotogramas** del bañista. |
| `Assets/AutoConfiguradorJuego.cs` | Añade solo el animador nuevo a cada criatura. |

> 🐙 El **pulpo** se queda como estaba (`AnimadorPulpo` + `Assets/Resources/Pulpo`).

## Frames: de dónde salen

Las imágenes originales (`delfin completo.jpg`, `tiburon completo.jpg`,
`bañista completo.jpg`) venían **con el fondo de cuadritos grises dibujado dentro del
JPG**. Se separaron automáticamente en fotogramas con **fondo transparente**:

- `Delfin` → 6 poses (una fila) → un salto del delfín.
- `Banista` → 8 poses (dos filas de 4) → el nado del bañista.
- `Tiburon` → 4 poses horizontales (una fila de 3 + 1 grande).

Cada pose se recorta a su contenido y se centra en un **lienzo común** (todas las
imágenes del mismo tamaño), para que la animación no “tiemble”.

> ⚠️ **Tiburón:** la tira original trae, además, varias poses del tiburón **encorvado
> ~90°** (como si se tirara de cabeza). Esas se dejaron **fuera** porque no encajan en
> un nado normal. Si algún día quieres un tiburón que salte/“ataque”, se pueden añadir.

## ¿Cómo funciona? (automático)

Al dar **Play**, `AutoConfiguradorJuego` añade a cada criatura el componente
`Animador Criatura Marina`, que en su *Awake*:

1. Deduce la carpeta de Resources por el **nombre del objeto** (`Delfin`, `Tiburon`, `Bañista`).
2. Carga y ordena los PNG (`delfin_00`, `delfin_01`, …).
3. Ajusta la escala para que la criatura tenga un tamaño coherente.

Funciona también en las **compilaciones** (no tienes que arrastrar nada).

## Ajustarlo a mano (opcional)

Selecciona `Delfin`, `Tiburon` o `Bañista` en la jerarquía y, en el componente
**Animador Criatura Marina**:

- **Carpeta Recursos**: la carpeta de `Assets/Resources` (se rellena sola).
- **Fotogramas**: arrastra imágenes aquí para cambiar el orden o quitar alguna.
- **Fps**: velocidad de la animación (por defecto 8).
- **Altura Deseada**: tamaño en pantalla (0 = automático según la criatura).

## Cómo comprobarlo (rápido)

1. Abre el proyecto en Unity y dale **Play**.
2. Mira al delfín, al tiburón y al bañista: deben cambiar de pose (nadar) mientras
   cruzan la pantalla de derecha a izquierda.
3. El pulpo sigue animándose como antes.

## Arreglo: el surfista perdía vidas "de la nada"

**Síntoma:** el surfista perdía una vida sin que ninguna criatura pareciera tocarlo
(la criatura se veía todavía lejos).

**Causa:** los `BoxCollider2D` de Delfín, Tiburón, Pulpo y Bañista venían de la escena
con un tamaño enorme (restos de los JPG originales sin recortar). Al cambiar el
animador la escala del objeto, el colisionador crecía con él y **era mucho más grande
que el dibujo**, así que el daño se activaba a distancia (incluso con la criatura fuera
de pantalla). Solo estas 4 criaturas dañan al surfista; el "fantasma" era uno de sus
colisionadores.

**Solución:** `Assets/Obstaculo.cs` ahora encaja el colisionador con el sprite visible en
su `Start()` (`AjustarColisionadorAlSprite`). El tamaño es una fracción del dibujo
(`factorColisionador`, por defecto **0.7**), de modo que el daño solo ocurre cuando de
verdad se tocan. Se puede subir/bajar ese valor en el Inspector durante el Play.
