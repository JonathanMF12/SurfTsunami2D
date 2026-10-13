using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Acción que ejecuta una opción de la portada.</summary>
public enum AccionPortada
{
    IniciarJuego,
    Opciones,
    Puntajes,
    Salir
}

/// <summary>
/// Una opción del menú de la portada. La posición y el tamaño están en coordenadas
/// normalizadas (0..1) DENTRO de la imagen de fondo, con el origen abajo-izquierda,
/// de modo que la zona clicable queda justo encima de cada palabra del arte.
/// </summary>
[Serializable]
public class OpcionPortada
{
    public string nombre = "START GAME";
    public AccionPortada accion = AccionPortada.IniciarJuego;

    [Tooltip("Centro de la opción dentro de la imagen (0..1, origen abajo-izquierda).")]
    public Vector2 posicionNormalizada = new Vector2(0.5f, 0.06f);

    [Tooltip("Tamaño de la zona clicable dentro de la imagen (0..1).")]
    public Vector2 tamanoNormalizado = new Vector2(0.2f, 0.08f);
}

/// <summary>
/// Pantalla de inicio (portada) con menú funcional.
///
/// El arte de fondo (<c>portada.jpg</c>) ya trae dibujadas las palabras
/// <b>START GAME · OPTIONS · LEADERBOARDS · QUIT</b>. Este script crea una zona
/// clicable invisible sobre cada palabra, resalta la opción activa (ratón, flechas
/// o mando) y ejecuta la lógica correspondiente:
///
/// <list type="bullet">
///   <item><b>START GAME</b> -> carga la escena del juego (<c>nombreEscenaJuego</c>).</item>
///   <item><b>OPTIONS</b> -> panel con sonido, pantalla completa y borrar puntajes.</item>
///   <item><b>LEADERBOARDS</b> -> panel con los mejores puntajes guardados.</item>
///   <item><b>QUIT</b> -> cierra el juego (en el editor detiene la reproducción).</item>
/// </list>
///
/// Cómo dejarla lista:
///   1. Selecciona el objeto "MenuInicio" en la jerarquía.
///   2. Arrastra tu imagen a "Sprite Fondo" (o al SpriteRenderer del objeto "Fondo").
///   3. Ajusta las opciones en el Inspector si quieres mover las zonas de clic
///      (los valores por defecto ya coinciden con la portada actual).
/// </summary>
[DisallowMultipleComponent]
public class MenuInicio : MonoBehaviour
{
    [Header("Escenas")]
    [Tooltip("Nombre EXACTO de la escena del juego. Debe estar en 'File > Build Settings'.")]
    public string nombreEscenaJuego = "SampleScene";

    [Header("Fondo (tu imagen)")]
    [Tooltip("El objeto con SpriteRenderer que hace de fondo.")]
    public GameObject objetoFondo;
    [Tooltip("Tu imagen de fondo. Si la dejas vacía se usa la que ya tenga el 'Fondo'.")]
    public Sprite spriteFondo;
    [Tooltip("Escala el fondo para que cubra toda la pantalla.")]
    public bool ajustarFondoALaPantalla = true;

    [Header("Opciones del menú")]
    [Tooltip("Si lo dejas vacío, se crean las 4 opciones de la portada actual.")]
    public OpcionPortada[] opciones;

    [Header("Apariencia")]
    [Tooltip("Color del recuadro que resalta la opción seleccionada.")]
    public Color colorResalte = new Color(1f, 0.95f, 0.45f, 0.38f);
    [Tooltip("Velocidad del parpadeo suave del resaltado.")]
    public float velocidadParpadeo = 1.6f;
    [Tooltip("Texto de ayuda que aparece arriba. Déjalo vacío para ocultarlo.")]
    public string mensajeInicio = "Elige con el ratón o las flechas y pulsa Enter";
    public float tamanoFuenteAyuda = 26f;

    // --- Estado en tiempo de ejecución ---
    private class ZonaOpcion
    {
        public RectTransform zona;
        public Image resalte;
        public OpcionPortada datos;
    }

    private class BotonPanel
    {
        public RectTransform zona;
        public Image fondo;
        public GameObject panel;
        public Action accion;
    }

    private Canvas canvas;
    private RectTransform canvasRect;
    private SpriteRenderer fondoSR;
    private Camera camara;
    private Sprite spritePanelRedondeado;

    private readonly List<ZonaOpcion> zonas = new List<ZonaOpcion>();
    private readonly List<BotonPanel> botonesPanel = new List<BotonPanel>();

    private GameObject panelOpciones;
    private GameObject panelPuntajes;
    private GameObject panelActivo;

    private Text textoAyuda;
    private Text textoBotonSonido;
    private Text textoBotonMusica;
    private Text textoBotonPantalla;
    private Text[] lineasPuntajes;

    private int seleccionada;
    private int indiceHover = -1;
    private bool construdo;
    private int ultimoAncho;
    private int ultimoAlto;
    private Vector2 ultimoTamCanvas;

    private Color colorBotonNormal = new Color(1f, 1f, 1f, 0.14f);
    private Color colorBotonHover = new Color(1f, 1f, 1f, 0.30f);

    void Start()
    {
        Time.timeScale = 1f;

        camara = Camera.main;

        // Fondo
        if (objetoFondo == null)
        {
            GameObject encontrado = GameObject.Find("Fondo");
            if (encontrado != null) objetoFondo = encontrado;
        }

        if (objetoFondo != null)
        {
            fondoSR = objetoFondo.GetComponent<SpriteRenderer>();
            if (fondoSR != null && spriteFondo != null) fondoSR.sprite = spriteFondo;
            if (ajustarFondoALaPantalla) AjustarFondoALaPantalla();
            fondoSR = objetoFondo.GetComponent<SpriteRenderer>();
        }

        if (opciones == null || opciones.Length == 0) opciones = OpcionesPorDefecto();

        Construir();
        AplicarSonidoGuardado();

        // En móvil no hay teclas: cambiamos la ayuda por una instrucción táctil.
        if (textoAyuda != null && ControladorTactil.HaySoporteTactil())
        {
            textoAyuda.text = "Toca una opción para elegirla";
        }
    }

    void Update()
    {
        if (!construdo) return;

        // Si cambia la resolución (o el tamaño real del lienzo), reubicamos las zonas
        // sobre la imagen. Comparamos el rect del Canvas y no "scaleFactor" porque ese
        // valor solo queda listo después del primer fotograma y dejaba las zonas mal
        // colocadas (amontonadas en una esquina).
        if (Screen.width != ultimoAncho || Screen.height != ultimoAlto ||
            (canvasRect != null && canvasRect.rect.size != ultimoTamCanvas))
        {
            ColocarZonas();
        }

        if (panelActivo != null)
        {
            ManejarPaneles();
            ActualizarResalte();
            return;
        }

        ActualizarPuntero();
        ActualizarTeclado();
        ActualizarResalte();
    }

    // ------------------------------------------------------------------
    // Construcción
    // ------------------------------------------------------------------

    private static OpcionPortada[] OpcionesPorDefecto()
    {
        // Coordenadas medidas sobre portada.jpg (2752 x 1536) y expresadas en 0..1.
        return new OpcionPortada[]
        {
            new OpcionPortada { nombre = "START GAME",   accion = AccionPortada.IniciarJuego, posicionNormalizada = new Vector2(0.232f, 0.062f), tamanoNormalizado = new Vector2(0.20f, 0.075f) },
            new OpcionPortada { nombre = "OPTIONS",      accion = AccionPortada.Opciones,     posicionNormalizada = new Vector2(0.419f, 0.062f), tamanoNormalizado = new Vector2(0.14f, 0.075f) },
            new OpcionPortada { nombre = "LEADERBOARDS", accion = AccionPortada.Puntajes,     posicionNormalizada = new Vector2(0.624f, 0.062f), tamanoNormalizado = new Vector2(0.23f, 0.075f) },
            new OpcionPortada { nombre = "QUIT",         accion = AccionPortada.Salir,        posicionNormalizada = new Vector2(0.809f, 0.062f), tamanoNormalizado = new Vector2(0.11f, 0.075f) },
        };
    }

    private void Construir()
    {
        if (construdo) return;
        construdo = true;

        CrearCanvas();
        spritePanelRedondeado = CrearSpriteRedondeado(64, 20);

        ConstruirZonas();
        ConstruirAyuda();
        ConstruirPaneles();

        // Fuerza a que el Canvas calcule ya su tamaño real (scaleFactor y rect) para
        // colocar las zonas correctamente desde el primer fotograma.
        Canvas.ForceUpdateCanvases();
        ColocarZonas();
    }

    private void CrearCanvas()
    {
        GameObject go = new GameObject("CanvasPortada", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = go.GetComponent<RectTransform>();
    }

    private void ConstruirZonas()
    {
        for (int i = 0; i < opciones.Length; i++)
        {
            OpcionPortada datos = opciones[i];
            if (datos == null) continue;

            GameObject zonaGO = new GameObject("Zona_" + datos.nombre, typeof(RectTransform));
            RectTransform rt = zonaGO.GetComponent<RectTransform>();
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            GameObject resalteGO = new GameObject("Resalte", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rtResalte = resalteGO.GetComponent<RectTransform>();
            rtResalte.SetParent(rt, false);
            rtResalte.anchorMin = Vector2.zero;
            rtResalte.anchorMax = Vector2.one;
            rtResalte.offsetMin = Vector2.zero;
            rtResalte.offsetMax = Vector2.zero;

            Image img = resalteGO.GetComponent<Image>();
            img.sprite = spritePanelRedondeado;
            img.type = Image.Type.Sliced;
            img.color = colorResalte;
            img.raycastTarget = false;

            zonas.Add(new ZonaOpcion { zona = rt, resalte = img, datos = datos });
        }
    }

    private void ConstruirAyuda()
    {
        if (string.IsNullOrEmpty(mensajeInicio)) return;

        GameObject go = new GameObject("Ayuda", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -24f);
        rt.sizeDelta = new Vector2(1200f, 60f);

        textoAyuda = go.GetComponent<Text>();
        textoAyuda.font = ObtenerFuente();
        textoAyuda.fontSize = Mathf.RoundToInt(tamanoFuenteAyuda);
        textoAyuda.fontStyle = FontStyle.Bold;
        textoAyuda.alignment = TextAnchor.MiddleCenter;
        textoAyuda.color = new Color(1f, 1f, 1f, 0.92f);
        textoAyuda.horizontalOverflow = HorizontalWrapMode.Overflow;
        textoAyuda.verticalOverflow = VerticalWrapMode.Overflow;
        textoAyuda.raycastTarget = false;
        textoAyuda.text = mensajeInicio;

        Outline sombra = go.AddComponent<Outline>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.85f);
        sombra.effectDistance = new Vector2(2f, -2f);
    }

    private void ConstruirPaneles()
    {
        // --- OPCIONES ---
        panelOpciones = CrearPanel("PanelOpciones", "OPCIONES");
        textoBotonSonido = CrearBoton(panelOpciones, "SONIDO", 122f, AlternarSonido);
        textoBotonMusica = CrearBoton(panelOpciones, "MÚSICA", 40f, AlternarMusica);
        textoBotonPantalla = CrearBoton(panelOpciones, "PANTALLA COMPLETA", -42f, AlternarPantallaCompleta);
        CrearBoton(panelOpciones, "BORRAR PUNTAJES", -124f, () =>
        {
            RankingPuntajes.Borrar();
            RefrescarPuntajes();
        });
        CrearBoton(panelOpciones, "VOLVER", -206f, CerrarPaneles);
        panelOpciones.SetActive(false);

        // --- LEADERBOARDS ---
        panelPuntajes = CrearPanel("PanelPuntajes", "MEJORES PUNTAJES");
        lineasPuntajes = new Text[RankingPuntajes.Maximo];
        for (int i = 0; i < lineasPuntajes.Length; i++)
        {
            lineasPuntajes[i] = CrearTextoPanel(panelPuntajes.transform, new Vector2(0f, 120f - i * 52f), 30f);
        }
        CrearBoton(panelPuntajes, "VOLVER", -170f, CerrarPaneles);
        panelPuntajes.SetActive(false);
    }

    private GameObject CrearPanel(string nombre, string titulo)
    {
        GameObject panel = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(760f, 560f);

        Image fondo = panel.GetComponent<Image>();
        fondo.sprite = spritePanelRedondeado;
        fondo.type = Image.Type.Sliced;
        fondo.color = new Color(0.02f, 0.05f, 0.12f, 0.93f);
        fondo.raycastTarget = false;

        Text t = CrearTextoPanel(rt, new Vector2(0f, 210f), 46f);
        t.text = titulo;
        t.color = new Color(1f, 0.95f, 0.5f, 1f);

        return panel;
    }

    private Text CrearBoton(GameObject panel, string etiqueta, float y, Action accion)
    {
        GameObject go = new GameObject("Boton_" + etiqueta, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(panel.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(560f, 76f);

        Image fondo = go.GetComponent<Image>();
        fondo.sprite = spritePanelRedondeado;
        fondo.type = Image.Type.Sliced;
        fondo.color = colorBotonNormal;
        fondo.raycastTarget = false;

        Text texto = CrearTextoHijo(rt, 32f);

        botonesPanel.Add(new BotonPanel { zona = rt, fondo = fondo, panel = panel, accion = accion });
        botonesPanel[botonesPanel.Count - 1].zona.name = "Boton_" + etiqueta;

        // El texto guarda la etiqueta base en su nombre para refrescar "SONIDO: ON/OFF".
        texto.name = etiqueta;
        texto.text = etiqueta;
        return texto;
    }

    private Text CrearTextoPanel(Transform padre, Vector2 pos, float tamano)
    {
        GameObject go = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(680f, 60f);

        Text t = go.GetComponent<Text>();
        t.font = ObtenerFuente();
        t.fontSize = Mathf.RoundToInt(tamano);
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;

        Outline sombra = go.AddComponent<Outline>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.85f);
        sombra.effectDistance = new Vector2(2f, -2f);

        return t;
    }

    private Text CrearTextoHijo(RectTransform padre, float tamano)
    {
        GameObject go = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Text t = go.GetComponent<Text>();
        t.font = ObtenerFuente();
        t.fontSize = Mathf.RoundToInt(tamano);
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    // ------------------------------------------------------------------
    // Posicionamiento de las zonas sobre el arte
    // ------------------------------------------------------------------

    private void ColocarZonas()
    {
        ultimoAncho = Screen.width;
        ultimoAlto = Screen.height;

        if (canvasRect == null && canvas != null) canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect != null) ultimoTamCanvas = canvasRect.rect.size;

        for (int i = 0; i < zonas.Count; i++)
        {
            ColocarZona(zonas[i].zona, zonas[i].datos.posicionNormalizada, zonas[i].datos.tamanoNormalizado);
        }
    }

    /// <summary>
    /// Coloca una zona sobre la imagen de fondo. Para que sea independiente de la
    /// resolución y del "scaleFactor" del Canvas, la posición de pantalla se convierte
    /// a coordenadas locales del Canvas con <see cref="RectTransformUtility"/>.
    /// </summary>
    private void ColocarZona(RectTransform rt, Vector2 posNorm, Vector2 tamNorm)
    {
        if (rt == null) return;

        if (canvasRect == null && canvas != null) canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null) return;

        if (fondoSR != null && fondoSR.sprite != null && camara != null)
        {
            Bounds b = fondoSR.bounds;
            Vector3 mundo = new Vector3(b.min.x + posNorm.x * b.size.x, b.min.y + posNorm.y * b.size.y, 0f);

            Vector3 mundoTam = new Vector3(tamNorm.x * b.size.x, tamNorm.y * b.size.y, 0f);

            Vector2 centroPantalla = camara.WorldToScreenPoint(mundo);
            Vector2 esquinaPantalla = camara.WorldToScreenPoint(mundo + mundoTam);

            Vector2 centroLocal, esquinaLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, centroPantalla, null, out centroLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, esquinaPantalla, null, out esquinaLocal);

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = centroLocal;
            rt.sizeDelta = new Vector2(
                Mathf.Abs(esquinaLocal.x - centroLocal.x),
                Mathf.Abs(esquinaLocal.y - centroLocal.y));
        }
        else
        {
            // Sin fondo: usamos las coordenadas normalizadas directamente sobre el lienzo.
            rt.anchorMin = rt.anchorMax = posNorm;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(
                tamNorm.x * canvasRect.rect.width,
                tamNorm.y * canvasRect.rect.height);
        }
    }

    // ------------------------------------------------------------------
    // Entrada
    // ------------------------------------------------------------------

    private void ActualizarPuntero()
    {
        Vector2 posPuntero = Vector2.zero;
        bool hayPuntero = false;
        bool presionado = false;

        Mouse raton = Mouse.current;
        if (raton != null)
        {
            posPuntero = raton.position.ReadValue();
            hayPuntero = true;
            if (raton.leftButton.wasPressedThisFrame) presionado = true;
        }

        Touchscreen tactil = Touchscreen.current;
        if (tactil != null)
        {
            var toque = tactil.primaryTouch;
            if (toque.press.isPressed)
            {
                posPuntero = toque.position.ReadValue();
                hayPuntero = true;
            }
            if (toque.press.wasPressedThisFrame) presionado = true;
        }

        indiceHover = -1;
        if (hayPuntero)
        {
            for (int i = 0; i < zonas.Count; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(zonas[i].zona, posPuntero, null))
                {
                    indiceHover = i;
                    break;
                }
            }
        }

        if (indiceHover >= 0) seleccionada = indiceHover;

        if (presionado && indiceHover >= 0) Ejecutar(zonas[indiceHover].datos.accion);
    }

    private void ActualizarTeclado()
    {
        if (zonas.Count == 0) return;

        Keyboard teclado = Keyboard.current;
        if (teclado != null)
        {
            bool derecha = teclado.rightArrowKey.wasPressedThisFrame || teclado.dKey.wasPressedThisFrame;
            bool izquierda = teclado.leftArrowKey.wasPressedThisFrame || teclado.aKey.wasPressedThisFrame;
            bool abajo = teclado.downArrowKey.wasPressedThisFrame || teclado.sKey.wasPressedThisFrame;
            bool arriba = teclado.upArrowKey.wasPressedThisFrame || teclado.wKey.wasPressedThisFrame;

            if (derecha || abajo) CambiarSeleccion(1);
            if (izquierda || arriba) CambiarSeleccion(-1);

            if (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame || teclado.spaceKey.wasPressedThisFrame)
            {
                Ejecutar(zonas[seleccionada].datos.accion);
            }
        }

        Gamepad mando = Gamepad.current;
        if (mando != null)
        {
            if (mando.dpad.right.wasPressedThisFrame || mando.dpad.down.wasPressedThisFrame) CambiarSeleccion(1);
            if (mando.dpad.left.wasPressedThisFrame || mando.dpad.up.wasPressedThisFrame) CambiarSeleccion(-1);
            if (mando.buttonSouth.wasPressedThisFrame) Ejecutar(zonas[seleccionada].datos.accion);
        }
    }

    private void CambiarSeleccion(int paso)
    {
        if (zonas.Count == 0) return;
        seleccionada = (seleccionada + paso + zonas.Count) % zonas.Count;
    }

    private void ActualizarResalte()
    {
        int resaltado = -1;
        if (panelActivo == null)
        {
            resaltado = indiceHover >= 0 ? indiceHover : seleccionada;
        }

        float pulso = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * velocidadParpadeo * Mathf.PI);

        for (int i = 0; i < zonas.Count; i++)
        {
            Image r = zonas[i].resalte;
            if (r == null) continue;

            bool activo = (i == resaltado);
            r.enabled = activo;
            if (activo)
            {
                Color c = colorResalte;
                c.a = colorResalte.a * (0.55f + 0.45f * pulso);
                r.color = c;
            }
        }
    }

    // ------------------------------------------------------------------
    // Paneles
    // ------------------------------------------------------------------

    private void ManejarPaneles()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado != null && (teclado.escapeKey.wasPressedThisFrame || teclado.backspaceKey.wasPressedThisFrame))
        {
            CerrarPaneles();
            return;
        }
        Gamepad mando = Gamepad.current;
        if (mando != null && mando.buttonEast.wasPressedThisFrame)
        {
            CerrarPaneles();
            return;
        }

        Vector2 posPuntero = Vector2.zero;
        bool hayPuntero = false;
        bool presionado = false;

        Mouse raton = Mouse.current;
        if (raton != null)
        {
            posPuntero = raton.position.ReadValue();
            hayPuntero = true;
            if (raton.leftButton.wasPressedThisFrame) presionado = true;
        }
        Touchscreen tactil = Touchscreen.current;
        if (tactil != null)
        {
            var toque = tactil.primaryTouch;
            if (toque.press.isPressed)
            {
                posPuntero = toque.position.ReadValue();
                hayPuntero = true;
            }
            if (toque.press.wasPressedThisFrame) presionado = true;
        }

        for (int i = 0; i < botonesPanel.Count; i++)
        {
            BotonPanel b = botonesPanel[i];
            if (b.panel != panelActivo) continue;

            bool encima = hayPuntero && RectTransformUtility.RectangleContainsScreenPoint(b.zona, posPuntero, null);
            if (b.fondo != null) b.fondo.color = encima ? colorBotonHover : colorBotonNormal;

            if (encima && presionado)
            {
                b.accion?.Invoke();
                return;
            }
        }
    }

    private void MostrarPanel(GameObject panel)
    {
        if (panel == null) return;
        panelActivo = panel;
        panelOpciones.SetActive(panel == panelOpciones);
        panelPuntajes.SetActive(panel == panelPuntajes);

        if (panel == panelOpciones) RefrescarOpciones();
        if (panel == panelPuntajes) RefrescarPuntajes();
    }

    private void CerrarPaneles()
    {
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelPuntajes != null) panelPuntajes.SetActive(false);
        panelActivo = null;
        indiceHover = -1;
    }

    private void Ejecutar(AccionPortada accion)
    {
        switch (accion)
        {
            case AccionPortada.IniciarJuego:
                EmpezarJuego();
                break;
            case AccionPortada.Opciones:
                MostrarPanel(panelOpciones);
                break;
            case AccionPortada.Puntajes:
                MostrarPanel(panelPuntajes);
                break;
            case AccionPortada.Salir:
                SalirDelJuego();
                break;
        }
    }

    // ------------------------------------------------------------------
    // Acciones
    // ------------------------------------------------------------------

    /// <summary>Arranca la escena del juego. También se puede llamar desde un botón.</summary>
    public void EmpezarJuego()
    {
        if (string.IsNullOrEmpty(nombreEscenaJuego))
        {
            Debug.LogError("[MenuInicio] No hay 'nombreEscenaJuego' configurado.");
            return;
        }
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    private const string ClaveSonido = "op_sonido";
    private const string ClavePantalla = "op_pantalla_completa";

    private void AplicarSonidoGuardado()
    {
        bool sonido = PlayerPrefs.GetInt(ClaveSonido, 1) == 1;
        AudioListener.volume = sonido ? 1f : 0f;
    }

    private void AlternarSonido()
    {
        bool sonido = PlayerPrefs.GetInt(ClaveSonido, 1) == 1;
        sonido = !sonido;
        PlayerPrefs.SetInt(ClaveSonido, sonido ? 1 : 0);
        PlayerPrefs.Save();
        AudioListener.volume = sonido ? 1f : 0f;
        RefrescarOpciones();
    }

    private void AlternarMusica()
    {
        if (MusicaFondo.Instancia != null)
        {
            MusicaFondo.Instancia.Alternar();
        }
        else
        {
            bool activa = PlayerPrefs.GetInt(MusicaFondo.ClaveMusica, 1) == 1;
            PlayerPrefs.SetInt(MusicaFondo.ClaveMusica, activa ? 0 : 1);
            PlayerPrefs.Save();
        }
        RefrescarOpciones();
    }

    private void AlternarPantallaCompleta()
    {
        bool pantallaCompleta = PlayerPrefs.GetInt(ClavePantalla, Screen.fullScreen ? 1 : 0) == 1;
        pantallaCompleta = !pantallaCompleta;
        PlayerPrefs.SetInt(ClavePantalla, pantallaCompleta ? 1 : 0);
        PlayerPrefs.Save();
        Screen.fullScreen = pantallaCompleta;
        RefrescarOpciones();
    }

    private void RefrescarOpciones()
    {
        if (textoBotonSonido != null)
        {
            bool sonido = PlayerPrefs.GetInt(ClaveSonido, 1) == 1;
            textoBotonSonido.text = "SONIDO: " + (sonido ? "ON" : "OFF");
        }
        if (textoBotonMusica != null)
        {
            bool musica = PlayerPrefs.GetInt(MusicaFondo.ClaveMusica, 1) == 1;
            textoBotonMusica.text = "MÚSICA: " + (musica ? "ON" : "OFF");
        }
        if (textoBotonPantalla != null)
        {
            bool pantallaCompleta = PlayerPrefs.GetInt(ClavePantalla, Screen.fullScreen ? 1 : 0) == 1;
            textoBotonPantalla.text = "PANTALLA COMPLETA: " + (pantallaCompleta ? "SÍ" : "NO");
        }
    }

    private void RefrescarPuntajes()
    {
        if (lineasPuntajes == null) return;

        int[] puntajes = RankingPuntajes.Obtener();
        for (int i = 0; i < lineasPuntajes.Length; i++)
        {
            if (lineasPuntajes[i] == null) continue;

            if (i < puntajes.Length)
            {
                lineasPuntajes[i].text = string.Format("{0}.   {1}", i + 1, puntajes[i].ToString("D6"));
                lineasPuntajes[i].color = Color.white;
            }
            else if (puntajes.Length == 0 && i == 0)
            {
                lineasPuntajes[i].text = "Aún no hay puntajes guardados";
                lineasPuntajes[i].color = new Color(1f, 1f, 1f, 0.7f);
            }
            else
            {
                lineasPuntajes[i].text = "";
            }
        }
    }

    private static void SalirDelJuego()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------------
    // Utilidades
    // ------------------------------------------------------------------

    private static Font ObtenerFuente()
    {
        Font f = null;
        try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (f == null)
        {
            try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
        }
        return f;
    }

    /// <summary>Crea un sprite blanco con esquinas redondeadas para usar con Image (Sliced).</summary>
    private static Sprite CrearSpriteRedondeado(int lado, int radio)
    {
        int n = Mathf.Max(lado, radio * 2 + 2);
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] pixeles = new Color32[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Max(radio - x, x - (n - 1 - radio), 0);
                float dy = Mathf.Max(radio - y, y - (n - 1 - radio), 0);
                float distancia = Mathf.Sqrt(dx * dx + dy * dy);
                float alfa = Mathf.Clamp01(radio - distancia + 0.5f);
                pixeles[y * n + x] = new Color32(255, 255, 255, (byte)(alfa * 255f));
            }
        }
        tex.SetPixels32(pixeles);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radio, radio, radio, radio));
    }

    private void AjustarFondoALaPantalla()
    {
        Camera cam = Camera.main;
        if (cam == null || objetoFondo == null) return;

        SpriteRenderer sr = objetoFondo.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        float altoMundo = cam.orthographicSize * 2f;
        float anchoMundo = altoMundo * cam.aspect;
        Vector2 tam = sr.sprite.bounds.size;
        if (tam.x <= 0f || tam.y <= 0f) return;

        float escala = Mathf.Max(anchoMundo / tam.x, altoMundo / tam.y);
        objetoFondo.transform.localScale = new Vector3(escala, escala, 1f);
    }
}
