using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// HUD del juego: vidas (tablas de surf), puntaje y pantalla de "Game Over".
///
/// Usa las imágenes de <c>Assets/Sprites/mensajes</c> (que son hojas de sprites):
///   - vidas_0                    -> la palabra "VIDAS" del HUD.
///   - vidas_2, vidas_3, vidas_4  -> las 3 tablas de surf (una por vida).
///   - puntaje_0                  -> la palabra "PUNTAJE" (HUD y cartel de Game Over).
///   - game over_0                -> el cartel "GAME OVER".
///
/// Nota: el sub-sprite puntaje_3 ("000000") de puntaje.png no se usa: el número final se
/// dibuja con texto, así que la tira de ceros solo se veía fea por detrás del marcador.
///
/// Este componente se coloca en el objeto <b>Canvas</b> de la escena. Al dar Play construye
/// (por código) los iconos de vidas, el número del marcador y el panel de Game Over, y luego
/// los mantiene sincronizados con el <see cref="GameManager"/>.
///
/// Los sprites se pueden arrastrar en el Inspector. Si se dejan vacíos, se intentan cargar
/// automáticamente desde una carpeta <c>Resources/mensajes</c>.
/// </summary>
[DisallowMultipleComponent]
public class HUDMensajes : MonoBehaviour
{
    [Header("Sprites de las vidas (tablas de surf)")]
    public Sprite tablaVida1;
    public Sprite tablaVida2;
    public Sprite tablaVida3;

    [Header("Sprites del puntaje y Game Over")]
    [Tooltip("La palabra 'PUNTAJE' (se usa en el HUD y en el cartel de Game Over).")]
    public Sprite spriteEtiquetaPuntaje;
    [Tooltip("El cartel 'GAME OVER'.")]
    public Sprite spriteGameOver;

    [Header("Etiquetas del HUD en juego (PUNTAJE / VIDAS)")]
    [Tooltip("La palabra 'VIDAS' que acompaña a las tablas de surf durante la partida.")]
    public Sprite spriteEtiquetaVidas;
    [Tooltip("Escala de las palabras 'PUNTAJE' y 'VIDAS' durante la partida.")]
    public float escalaEtiquetaHud = 0.30f;

    [Header("Posición y tamaño del HUD")]
    [Tooltip("Ancla el HUD a la parte SUPERIOR de la pantalla (vidas a la derecha, " +
             "puntaje a la izquierda). Desactívalo para volver al modo centrado antiguo.")]
    public bool anclarArriba = true;
    [Tooltip("Distancia (px) desde el borde superior de la pantalla.")]
    public float margenSuperior = 40f;
    [Tooltip("Distancia (px) desde los bordes laterales de la pantalla.")]
    public float margenLateral = 70f;

    [Tooltip("Posición CENTRADA de las vidas (solo si 'anclarArriba' está apagado).")]
    public Vector2 posicionVidas = new Vector2(270f, 85f);
    public float separacionVidas = 55f;
    public float escalaVidas = 0.35f;
    [Tooltip("Posición CENTRADA del puntaje (solo si 'anclarArriba' está apagado).")]
    public Vector2 posicionPuntaje = new Vector2(-270f, 85f);
    public float tamanoFuentePuntaje = 40f;
    public Color colorTexto = Color.white;

    [Header("Game Over")]
    [Tooltip("Color del cuadrado transparente que cubre la pantalla.")]
    public Color colorFondoGameOver = new Color(0f, 0f, 0f, 0.65f);
    public float escalaGameOver = 0.62f;
    public float escalaEtiquetaPuntaje = 0.36f;
    public float tamanoFuentePuntajeFinal = 56f;
    public string textoReiniciar = "Pulsa R para volver a surfear";

    [Tooltip("Muestra también los botones REINTENTAR / MENÚ en el editor/escritorio, " +
             "para poder probarlos sin un dispositivo táctil.")]
    public bool mostrarBotonesTactilesEnEscritorio = false;

    // --- Referencias construidas en tiempo de ejecución ---
    private Image[] iconosVida;
    private Text textoPuntaje;
    private GameObject panelGameOver;
    private Text textoPuntajeFinal;
    private RectTransform botonReintentar;
    private RectTransform botonMenu;
    private Sprite spriteBotonTactil;
    private Font fuente;
    private bool construido;

    void Awake()
    {
        Construir();
    }

    void Update()
    {
        GameManager gm = GameManager.Instancia;
        if (gm == null) return;

        if (!construido) Construir();

        // --- Vidas: una tabla de surf por cada vida restante ---
        if (iconosVida != null)
        {
            for (int i = 0; i < iconosVida.Length; i++)
            {
                if (iconosVida[i] != null)
                {
                    iconosVida[i].gameObject.SetActive(i < gm.vidasActuales);
                }
            }
        }

        // --- Puntaje en juego ---
        if (textoPuntaje != null)
        {
            textoPuntaje.text = gm.puntaje.ToString();
        }

        // --- Game Over ---
        if (panelGameOver != null && gm.juegoTerminado && !panelGameOver.activeSelf)
        {
            if (textoPuntajeFinal != null)
            {
                textoPuntajeFinal.text = gm.puntaje.ToString();
            }
            panelGameOver.SetActive(true);
        }

        if (panelGameOver != null && panelGameOver.activeSelf)
        {
            ProcesarBotonesTactiles();
        }
    }

    /// <summary>Comprueba los botones REINTENTAR / MENÚ de la pantalla de Game Over (táctil).</summary>
    private void ProcesarBotonesTactiles()
    {
        if (botonReintentar == null && botonMenu == null) return;

        Vector2 pos = Vector2.zero;
        bool presionado = false;

        Touchscreen tactil = Touchscreen.current;
        if (tactil != null)
        {
            var toque = tactil.primaryTouch;
            if (toque.press.isPressed) pos = toque.position.ReadValue();
            if (toque.press.wasPressedThisFrame) presionado = true;
        }

        if (!presionado)
        {
            Mouse raton = Mouse.current;
            if (raton != null)
            {
                pos = raton.position.ReadValue();
                if (raton.leftButton.wasPressedThisFrame) presionado = true;
            }
        }

        if (!presionado) return;

        if (botonReintentar != null &&
            RectTransformUtility.RectangleContainsScreenPoint(botonReintentar, pos, null))
        {
            if (GameManager.Instancia != null) GameManager.Instancia.ReiniciarPartida();
        }
        else if (botonMenu != null &&
                 RectTransformUtility.RectangleContainsScreenPoint(botonMenu, pos, null))
        {
            Time.timeScale = 1f;
            string escena = GameManager.Instancia != null ? GameManager.Instancia.nombreEscenaMenu : null;
            if (!string.IsNullOrEmpty(escena)) SceneManager.LoadScene(escena);
        }
    }

    /// <summary>Construye todos los elementos del HUD una sola vez.</summary>
    private void Construir()
    {
        if (construido) return;
        construido = true;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[HUDMensajes] No se encontró ningún Canvas en la escena.");
            return;
        }

        fuente = ObtenerFuente();
        CargarSpritesSiFaltan();
        OcultarHudAntiguo(canvas.transform);

        ConstruirVidas(canvas.transform);
        ConstruirPuntaje(canvas.transform);
        ConstruirGameOver(canvas.transform);
    }

    /// <summary>
    /// Desactiva los objetos "score" y "vidas" que quedaron en la escena (restos de una
    /// versión anterior del HUD) para no duplicar los iconos que crea este componente.
    /// </summary>
    private void OcultarHudAntiguo(Transform padre)
    {
        if (padre == null) return;

        for (int i = 0; i < padre.childCount; i++)
        {
            Transform hijo = padre.GetChild(i);
            if (hijo == null) continue;

            string n = hijo.name.ToLowerInvariant();
            if (n == "score" || n == "vidas")
            {
                hijo.gameObject.SetActive(false);
            }
        }
    }

    private void ConstruirVidas(Transform padre)
    {
        const int totalVidas = 3;
        iconosVida = new Image[totalVidas];

        Sprite[] tablas = { tablaVida1, tablaVida2, tablaVida3 };

        for (int i = 0; i < totalVidas; i++)
        {
            Sprite sprite = tablas[i] != null ? tablas[i] : tablaVida1;
            if (sprite == null) continue;

            Vector2 pos;
            Vector2 ancla;

            if (anclarArriba)
            {
                // Fila de tablas pegada a la esquina superior derecha (i=0 a la izquierda).
                float ancho = sprite.rect.width * escalaVidas;
                float alto = sprite.rect.height * escalaVidas;
                float sep = Mathf.Max(separacionVidas, ancho * 0.6f);

                float centroX = -(margenLateral + ancho * 0.5f + (totalVidas - 1 - i) * sep);
                float centroY = -(margenSuperior + alto * 0.5f);
                pos = new Vector2(centroX, centroY);
                ancla = new Vector2(1f, 1f);
            }
            else
            {
                float desplazamientoX = (i - (totalVidas - 1) * 0.5f) * separacionVidas;
                pos = new Vector2(posicionVidas.x + desplazamientoX, posicionVidas.y);
                ancla = new Vector2(0.5f, 0.5f);
            }

            iconosVida[i] = CrearImagen("Vida_" + (i + 1), sprite, padre, pos, escalaVidas, ancla);
        }

        // Etiqueta "VIDAS" a la izquierda de las tablas (solo en el HUD anclado arriba).
        if (anclarArriba && spriteEtiquetaVidas != null)
        {
            Sprite primera = tablas[0] != null ? tablas[0] : tablaVida1;
            if (primera != null)
            {
                float ancho = primera.rect.width * escalaVidas;
                float sep = Mathf.Max(separacionVidas, ancho * 0.6f);
                float bordeIzquierdo = -(margenLateral + ancho + (totalVidas - 1) * sep);

                CrearImagen("EtiquetaVidas", spriteEtiquetaVidas, padre,
                    new Vector2(bordeIzquierdo - 24f, -margenSuperior), escalaEtiquetaHud, new Vector2(1f, 1f));
            }
        }
    }

    private void ConstruirPuntaje(Transform padre)
    {
        Vector2 pos = anclarArriba
            ? new Vector2(margenLateral, -margenSuperior)
            : posicionPuntaje;
        Vector2 ancla = anclarArriba ? new Vector2(0f, 1f) : new Vector2(0.5f, 0.5f);

        // Etiqueta "PUNTAJE" encima del número (solo en el HUD anclado arriba).
        if (anclarArriba && spriteEtiquetaPuntaje != null)
        {
            CrearImagen("EtiquetaPuntaje", spriteEtiquetaPuntaje, padre,
                new Vector2(margenLateral, -margenSuperior), escalaEtiquetaHud, new Vector2(0f, 1f));

            float altoEtiqueta = spriteEtiquetaPuntaje.rect.height * escalaEtiquetaHud;
            pos = new Vector2(margenLateral, -margenSuperior - altoEtiqueta - 6f);
        }

        textoPuntaje = CrearTexto(
            "PuntajeTexto", padre, pos,
            new Vector2(360f, 80f), tamanoFuentePuntaje, TextAnchor.MiddleLeft, ancla);
        textoPuntaje.text = "0";
    }

    private void ConstruirGameOver(Transform padre)
    {
        // Cuadrado transparente que cubre toda la pantalla.
        GameObject panel = new GameObject("GameOver_Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.layer = padre.gameObject.layer;
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image fondo = panel.GetComponent<Image>();
        fondo.color = colorFondoGameOver;
        fondo.raycastTarget = false;

        // Cartel "GAME OVER".
        if (spriteGameOver != null)
        {
            CrearImagen("GameOver_Cartel", spriteGameOver, panel.transform, new Vector2(0f, 170f), escalaGameOver);
        }

        // Palabra "PUNTAJE".
        if (spriteEtiquetaPuntaje != null)
        {
            CrearImagen("GameOver_EtiquetaPuntaje", spriteEtiquetaPuntaje, panel.transform, new Vector2(0f, -60f), escalaEtiquetaPuntaje);
        }

        // Número final (sin la tira "000000" detrás; queda solo el texto del puntaje).
        textoPuntajeFinal = CrearTexto(
            "GameOver_Puntaje", panel.transform, new Vector2(0f, -150f),
            new Vector2(420f, 110f), tamanoFuentePuntajeFinal, TextAnchor.MiddleCenter);
        textoPuntajeFinal.text = "0";

        // Pista para reiniciar.
        Text pista = CrearTexto(
            "GameOver_Pista", panel.transform, new Vector2(0f, -255f),
            new Vector2(860f, 60f), 30f, TextAnchor.MiddleCenter);
        pista.text = textoReiniciar;

        // En móvil/táctil no hay tecla R: añadimos botones para reintentar o volver al menú.
        bool tactil = mostrarBotonesTactilesEnEscritorio || ControladorTactil.HaySoporteTactil();
        if (tactil)
        {
            pista.text = "Toca un botón para continuar";

            botonReintentar = CrearBotonTactil(
                panel.transform, "REINTENTAR", new Vector2(-185f, -355f),
                new Color(0.10f, 0.55f, 0.30f, 0.92f));

            botonMenu = CrearBotonTactil(
                panel.transform, "MENÚ", new Vector2(185f, -355f),
                new Color(0.16f, 0.22f, 0.44f, 0.92f));
        }

        panelGameOver = panel;
        panel.SetActive(false);
    }

    // ------------------------------------------------------------------
    // Utilidades de construcción
    // ------------------------------------------------------------------

    private Image CrearImagen(string nombre, Sprite sprite, Transform padre, Vector2 posicion, float escala,
                              Vector2? anclaRect = null)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = padre.gameObject.layer;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        Vector2 ancla = anclaRect ?? new Vector2(0.5f, 0.5f);
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = ancla;
        rt.anchoredPosition = posicion;

        if (sprite != null)
        {
            // Usa el tamaño nativo del sprite para no deformarlo.
            rt.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
        }
        else
        {
            rt.sizeDelta = new Vector2(100f, 100f);
        }

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        go.transform.localScale = new Vector3(escala, escala, 1f);
        return img;
    }

    private Text CrearTexto(string nombre, Transform padre, Vector2 posicion, Vector2 tamano, float fontSize, TextAnchor anclaje,
                            Vector2? anclaRect = null)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.layer = padre.gameObject.layer;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        Vector2 ancla = anclaRect ?? new Vector2(0.5f, 0.5f);
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = ancla;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        Text texto = go.GetComponent<Text>();
        texto.font = fuente;
        texto.fontSize = Mathf.RoundToInt(fontSize);
        texto.fontStyle = FontStyle.Bold;
        texto.alignment = anclaje;
        texto.color = colorTexto;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        texto.raycastTarget = false;

        // Sombra para que el número se lea sobre cualquier fondo.
        Outline sombra = go.AddComponent<Outline>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.85f);
        sombra.effectDistance = new Vector2(2f, -2f);

        return texto;
    }

    /// <summary>Crea un botón táctil rectangular redondeado dentro del panel de Game Over.</summary>
    private RectTransform CrearBotonTactil(Transform padre, string etiqueta, Vector2 posicion, Color color)
    {
        GameObject go = new GameObject("Boton_" + etiqueta, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = padre.gameObject.layer;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = new Vector2(300f, 84f);

        if (spriteBotonTactil == null) spriteBotonTactil = CrearSpriteRedondeado(64, 20);

        Image img = go.GetComponent<Image>();
        img.sprite = spriteBotonTactil;
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;

        Text t = CrearTexto("Texto", rt, Vector2.zero, new Vector2(300f, 84f), 30f, TextAnchor.MiddleCenter);
        t.text = etiqueta;
        t.color = Color.white;

        return rt;
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

    /// <summary>
    /// Rellena los sprites que falten desde <c>Resources/mensajes</c> (por si no se
    /// arrastraron en el Inspector). Empareja por nombre de sub-sprite.
    /// </summary>
    private void CargarSpritesSiFaltan()
    {
        if (tablaVida1 != null && spriteGameOver != null && spriteEtiquetaPuntaje != null &&
            spriteEtiquetaVidas != null) return;

        Sprite[] sprites = Resources.LoadAll<Sprite>("mensajes");
        if (sprites == null || sprites.Length == 0) return;

        foreach (Sprite s in sprites)
        {
            if (s == null) continue;
            switch (s.name)
            {
                case "vidas_0": if (spriteEtiquetaVidas == null) spriteEtiquetaVidas = s; break;
                case "vidas_2": if (tablaVida1 == null) tablaVida1 = s; break;
                case "vidas_3": if (tablaVida2 == null) tablaVida2 = s; break;
                case "vidas_4": if (tablaVida3 == null) tablaVida3 = s; break;
                case "puntaje_0": if (spriteEtiquetaPuntaje == null) spriteEtiquetaPuntaje = s; break;
                case "game over_0": if (spriteGameOver == null) spriteGameOver = s; break;
            }
        }
    }
}
