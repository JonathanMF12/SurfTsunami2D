using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Controles en pantalla para móvil/táctil:
///   - Un <b>joystick flotante</b>: toca en la mitad izquierda (donde quieras) y arrastra
///     para mover al surfista en las 4 direcciones. Suelta para detenerte.
///   - Un <b>botón de SALTO</b> abajo a la derecha. También sirve tocar en cualquier punto
///     de la mitad derecha de la pantalla.
///
/// La jugabilidad es exactamente la misma que con teclado/mando: solo cambia cómo se
/// introduce la dirección y el salto. El joystick y el salto son multitáctiles, así que
/// puedes moverte y saltar a la vez.
///
/// El componente se crea automáticamente en la escena de juego (ver <see cref="AutoConfiguradorJuego"/>)
/// y solo se muestra en dispositivos táctiles. Para probarlo en el editor, activa
/// <see cref="mostrarEnEscritorio"/> o pulsa la tecla <b>T</b> durante el juego.
/// </summary>
[DisallowMultipleComponent]
public class ControladorTactil : MonoBehaviour
{
    /// <summary>Instancia activa en la escena de juego.</summary>
    public static ControladorTactil Instancia { get; private set; }

    [Header("Visibilidad")]
    [Tooltip("Muestra los controles también en escritorio/editor (útil para probarlos). " +
             "En el juego real aparecen solos en dispositivos táctiles.")]
    public bool mostrarEnEscritorio = false;

    [Tooltip("Permite mostrar/ocultar los controles con la tecla T (para probar en el editor).")]
    public bool alternarConTeclaT = true;

    [Header("Aspecto (coordenadas de referencia 1280x720)")]
    public float radioJoystick = 120f;
    public float radioKnob = 52f;
    public float diametroBotonSalto = 160f;
    public float margen = 40f;

    public Color colorBase = new Color(1f, 1f, 1f, 0.16f);
    public Color colorKnob = new Color(1f, 1f, 1f, 0.55f);
    public Color colorSalto = new Color(1f, 0.95f, 0.4f, 0.30f);

    /// <summary>Dirección de movimiento actual (-1..1 en cada eje).</summary>
    public Vector2 Entrada { get; private set; }

    /// <summary>Si los controles están visibles.</summary>
    public bool Visible { get; private set; }

    private GameObject raiz;
    private Canvas canvas;
    private RectTransform baseJoystick;
    private RectTransform knob;
    private RectTransform baseIdle;
    private RectTransform botonSalto;

    private Sprite spriteAnillo;
    private Sprite spriteCirculo;
    private Font fuente;

    private int idToqueJoy = -1;
    private bool ratonJoy;
    private Vector2 inicioJoy;
    private bool saltoPendiente;
    private float tiempoSaltoPendiente;

    /// <summary>True si la plataforma o el dispositivo admiten toque.</summary>
    public static bool HaySoporteTactil()
    {
        return Application.isMobilePlatform || Touchscreen.current != null;
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        fuente = ObtenerFuente();

        Visible = mostrarEnEscritorio || HaySoporteTactil();
        Construir();
        ActualizarVisibilidad();
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    void Update()
    {
        if (alternarConTeclaT && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            Visible = !Visible;
        }

        ActualizarVisibilidad();

        if (raiz == null || !raiz.activeSelf)
        {
            ReiniciarEntrada();
            return;
        }

        ProcesarToques();
        ProcesarRaton();

        // Un toque de salto no consumido en un cuarto de segundo se descarta, para que
        // no se acumule y dispare un salto raro al aterrizar.
        if (saltoPendiente && Time.unscaledTime - tiempoSaltoPendiente > 0.25f)
        {
            saltoPendiente = false;
        }

        bool joyActivo = idToqueJoy >= 0 || ratonJoy;
        if (baseIdle != null) baseIdle.gameObject.SetActive(!joyActivo);
        if (baseJoystick != null) baseJoystick.gameObject.SetActive(joyActivo);
    }

    /// <summary>Devuelve true una sola vez cuando el jugador pidió saltar.</summary>
    public bool ConsumirSalto()
    {
        if (!saltoPendiente) return false;
        saltoPendiente = false;
        return true;
    }

    // ------------------------------------------------------------------
    // Construcción del HUD táctil
    // ------------------------------------------------------------------

    private void Construir()
    {
        spriteCirculo = CrearCirculo(128, false);
        spriteAnillo = CrearCirculo(256, true);

        raiz = new GameObject("CanvasControlesTactiles", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        raiz.transform.SetParent(transform, false);

        canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        CanvasScaler scaler = raiz.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Pista del joystick (se ve cuando no lo estás usando).
        baseIdle = CrearImagen("PistaJoystick", spriteAnillo, colorBase, radioJoystick * 2f);
        baseIdle.anchorMin = baseIdle.anchorMax = new Vector2(0.17f, 0.30f);
        baseIdle.anchoredPosition = Vector2.zero;

        // Base del joystick (aparece donde tocas).
        baseJoystick = CrearImagen("BaseJoystick", spriteAnillo, colorBase, radioJoystick * 2f);
        baseJoystick.gameObject.SetActive(false);

        knob = CrearImagen("Knob", spriteCirculo, colorKnob, radioKnob * 2f);
        knob.SetParent(baseJoystick, false);
        knob.anchorMin = knob.anchorMax = new Vector2(0.5f, 0.5f);
        knob.anchoredPosition = Vector2.zero;

        // Botón de salto.
        botonSalto = CrearImagen("BotonSalto", spriteCirculo, colorSalto, diametroBotonSalto);
        botonSalto.anchorMin = botonSalto.anchorMax = new Vector2(1f, 0f);
        botonSalto.pivot = new Vector2(1f, 0f);
        botonSalto.anchoredPosition = new Vector2(-margen, margen);
        CrearTexto(botonSalto, "SALTO", 30f);
    }

    private RectTransform CrearImagen(string nombre, Sprite sprite, Color color, float tamano)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(tamano, tamano);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    private Text CrearTexto(Transform padre, string contenido, float tamano)
    {
        GameObject go = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(padre, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Text t = go.GetComponent<Text>();
        t.font = fuente;
        t.text = contenido;
        t.fontSize = Mathf.RoundToInt(tamano);
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(1f, 1f, 1f, 0.9f);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;

        Outline sombra = t.gameObject.AddComponent<Outline>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.7f);
        sombra.effectDistance = new Vector2(2f, -2f);

        return t;
    }

    // ------------------------------------------------------------------
    // Entrada
    // ------------------------------------------------------------------

    private void ActualizarVisibilidad()
    {
        if (raiz == null) return;

        bool fin = GameManager.Instancia != null && GameManager.Instancia.juegoTerminado;
        raiz.SetActive(Visible && !fin);
    }

    private void ReiniciarEntrada()
    {
        Entrada = Vector2.zero;
        saltoPendiente = false;
        idToqueJoy = -1;
        ratonJoy = false;
    }

    private void ProcesarToques()
    {
        Touchscreen tactil = Touchscreen.current;
        if (tactil == null) return;

        var toques = tactil.touches;
        for (int i = 0; i < toques.Count; i++)
        {
            var toque = toques[i];
            int id = toque.touchId.ReadValue();
            Vector2 pos = toque.position.ReadValue();

            if (toque.press.wasPressedThisFrame)
            {
                if (pos.x < Screen.width * 0.5f)
                {
                    if (idToqueJoy < 0)
                    {
                        idToqueJoy = id;
                        inicioJoy = pos;
                        ActualizarKnob(pos);
                    }
                }
                else
                {
                    saltoPendiente = true;
                    tiempoSaltoPendiente = Time.unscaledTime;
                }
            }
            else if (toque.press.isPressed && id == idToqueJoy)
            {
                ActualizarKnob(pos);
            }
            else if (toque.press.wasReleasedThisFrame && id == idToqueJoy)
            {
                idToqueJoy = -1;
                Entrada = Vector2.zero;
            }
        }
    }

    /// <summary>Respaldo con ratón para poder probar los controles en el editor.</summary>
    private void ProcesarRaton()
    {
        Mouse raton = Mouse.current;
        if (raton == null || idToqueJoy >= 0) return;

        Vector2 pos = raton.position.ReadValue();

        if (raton.leftButton.wasPressedThisFrame)
        {
            if (pos.x < Screen.width * 0.5f)
            {
                ratonJoy = true;
                inicioJoy = pos;
                ActualizarKnob(pos);
            }
            else
            {
                saltoPendiente = true;
                tiempoSaltoPendiente = Time.unscaledTime;
            }
        }
        else if (raton.leftButton.isPressed && ratonJoy)
        {
            ActualizarKnob(pos);
        }
        else if (raton.leftButton.wasReleasedThisFrame && ratonJoy)
        {
            ratonJoy = false;
            Entrada = Vector2.zero;
        }
    }

    private void ActualizarKnob(Vector2 posPantalla)
    {
        float escala = canvas != null ? canvas.scaleFactor : 1f;
        if (escala <= 0.0001f) escala = 1f;

        float radioPx = radioJoystick * escala;
        Vector2 deltaPx = posPantalla - inicioJoy;

        if (deltaPx.magnitude > radioPx)
        {
            deltaPx = deltaPx.normalized * radioPx;
        }

        Entrada = deltaPx / radioPx;

        if (baseJoystick != null)
        {
            baseJoystick.anchoredPosition = inicioJoy / escala;
        }
        if (knob != null)
        {
            knob.anchoredPosition = deltaPx / escala;
        }
    }

    // ------------------------------------------------------------------
    // Utilidades de sprites y fuente
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

    /// <summary>Crea un sprite de círculo blanco (o un anillo si <paramref name="anillo"/> es true).</summary>
    private static Sprite CrearCirculo(int lado, bool anillo)
    {
        lado = Mathf.Max(8, lado);
        Texture2D tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float r = lado * 0.5f;
        Color32[] pixeles = new Color32[lado * lado];

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float dx = x + 0.5f - r;
                float dy = y + 0.5f - r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                float alfa = Mathf.Clamp01(r - d); // borde exterior suavizado
                if (anillo)
                {
                    float interior = Mathf.Clamp01(d - r * 0.72f);
                    alfa = Mathf.Min(alfa, interior);
                }

                pixeles[y * lado + x] = new Color32(255, 255, 255, (byte)(alfa * 255f));
            }
        }

        tex.SetPixels32(pixeles);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), 100f);
    }
}
