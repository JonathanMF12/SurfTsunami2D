using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla al jugador "Surfista1":
///  - Movimiento con teclado/mando usando el NUEVO Input System (el proyecto no usa el viejo).
///  - Físicas sin gravedad: el surfista flota sobre la ola y NO se cae.
///  - Límites automáticos según el encuadre de la cámara: nunca sale de pantalla.
///  - Salto automático al llegar a la copa de la ola (también con la Barra Espaciadora).
///  - La animación de sprites la controla el componente AnimadorSurfista.
///
/// Los límites del "área de juego" salen de la foto 'area-de-juego-surfista':
/// ajusta minY (borde inferior) y maxY (copa de la ola) en el Inspector.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SurfistaController : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Velocidad máxima de desplazamiento (unidades por segundo).")]
    public float velocidad = 7f;

    [Tooltip("Qué tan rápido alcanza la velocidad máxima. Más alto = más reactivo.")]
    public float aceleracion = 45f;

    [Header("Área de juego (según 'area-de-juego-surfista')")]
    [Tooltip("Si está activo, los límites izquierdo/derecho se calculan con el ancho de la cámara: el surfista nunca sale del encuadre.")]
    public bool usarAnchoDeCamara = true;

    [Tooltip("Margen interior respecto a los bordes de la cámara.")]
    public float margenLateral = 0.6f;

    [Tooltip("Límite izquierdo manual (solo si 'usarAnchoDeCamara' está apagado).")]
    public float minX = -8f;

    [Tooltip("Límite derecho manual (solo si 'usarAnchoDeCamara' está apagado).")]
    public float maxX = 8f;

    [Tooltip("Borde inferior del área. Evita que el surfista se caiga.")]
    public float minY = -4f;

    [Tooltip("Copa de la ola: cuando el surfista llega aquí, salta.")]
    public float maxY = -0.8f;

    [Header("Salto en la copa de la ola")]
    [Tooltip("Salta automáticamente al alcanzar la copa de la ola subiendo.")]
    public bool saltoAutomaticoEnCopa = true;

    [Tooltip("Altura máxima del salto sobre la cresta.")]
    public float alturaSalto = 2.2f;

    [Tooltip("Duración total del salto en segundos.")]
    public float duracionSalto = 0.75f;

    [Tooltip("Giro (truco aéreo) en grados durante el salto.")]
    public float rotacionAerea = 360f;

    [Tooltip("Tiempo mínimo entre saltos.")]
    public float enfriamientoSalto = 0.2f;

    [Header("Vidas e invulnerabilidad")]
    public int vidasMaximas = 3;
    public float tiempoInvulnerabilidad = 1.5f;

    // Propiedades públicas consultadas por los obstáculos y el GameManager.
    public int VidasActuales { get; private set; }
    public bool EstaSaltando { get; private set; }
    public bool EsInvulnerable { get; private set; }

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private AnimadorSurfista animador;
    private Camera camara;

    private Vector2 entrada;
    private Vector2 velocidadActual;
    private float tiempoUltimoSalto = -10f;
    private Quaternion rotacionOriginal;

    void Awake()
    {
        VidasActuales = vidasMaximas;
        rotacionOriginal = transform.rotation;

        sr = GetComponent<SpriteRenderer>();

        animador = GetComponent<AnimadorSurfista>();
        if (animador == null)
        {
            animador = gameObject.AddComponent<AnimadorSurfista>();
        }

        // Físicas: sin gravedad para que flote sobre la ola y no se caiga.
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.None;

        // Colisionador para detectar obstáculos (los obstáculos son Triggers).
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            CapsuleCollider2D capsula = gameObject.AddComponent<CapsuleCollider2D>();
            capsula.direction = CapsuleDirection2D.Horizontal;
            capsula.isTrigger = true;
        }
    }

    void Start()
    {
        camara = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();

        // Empezar dentro del área de juego en Z = 0.
        transform.position = new Vector3(transform.position.x, Mathf.Clamp(transform.position.y, minY, maxY), 0f);

        if (GameManager.Instancia != null)
        {
            GameManager.Instancia.ActualizarVidas(VidasActuales);
        }
    }

    void Update()
    {
        if (GameManager.Instancia != null && GameManager.Instancia.juegoTerminado) return;
        if (EstaSaltando) return;

        LeerEntrada();
        DetectarSalto();
        ActualizarAnimacion();
    }

    void FixedUpdate()
    {
        if (GameManager.Instancia != null && GameManager.Instancia.juegoTerminado) return;
        if (EstaSaltando) return;

        // Aceleración suave hacia la velocidad objetivo.
        Vector2 velocidadObjetivo = entrada * velocidad;
        velocidadActual = Vector2.MoveTowards(velocidadActual, velocidadObjetivo, aceleracion * Time.fixedDeltaTime);

        // Mover y recortar SIEMPRE dentro del área de juego.
        Vector2 nuevaPosicion = rb.position + velocidadActual * Time.fixedDeltaTime;
        nuevaPosicion.x = Mathf.Clamp(nuevaPosicion.x, LimiteIzquierdo, LimiteDerecho);
        nuevaPosicion.y = Mathf.Clamp(nuevaPosicion.y, minY, maxY);
        rb.MovePosition(nuevaPosicion);
    }

    /// <summary>Lee teclado (WASD / flechas) y mando (stick izquierdo / cruceta).</summary>
    private void LeerEntrada()
    {
        Vector2 v = Vector2.zero;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
        }

        Gamepad gp = Gamepad.current;
        if (gp != null)
        {
            Vector2 stick = gp.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.04f) v += stick;

            Vector2 cruceta = gp.dpad.ReadValue();
            if (cruceta.sqrMagnitude > 0.04f) v += cruceta;
        }

        // Controles en pantalla (móvil/táctil). En escritorio suelen estar ocultos y
        // la entrada es cero, así que no afectan al teclado ni al mando.
        ControladorTactil tactil = ControladorTactil.Instancia;
        if (tactil != null && tactil.Entrada.sqrMagnitude > 0.0001f)
        {
            v += tactil.Entrada;
        }

        entrada = Vector2.ClampMagnitude(v, 1f);
    }

    /// <summary>Salta al llegar a la copa de la ola (y también con la Barra Espaciadora o el botón táctil).</summary>
    private void DetectarSalto()
    {
        bool enLaCopa = transform.position.y >= maxY - 0.08f;
        bool subiendo = entrada.y > 0.1f;

        bool saltoTactil = ControladorTactil.Instancia != null && ControladorTactil.Instancia.ConsumirSalto();

        bool saltoManual = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                        || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                        || saltoTactil;

        bool pedirSalto = (saltoAutomaticoEnCopa && subiendo && enLaCopa) || saltoManual;

        if (pedirSalto && Time.time > tiempoUltimoSalto + duracionSalto + enfriamientoSalto)
        {
            StartCoroutine(RealizarSalto());
        }
    }

    private void ActualizarAnimacion()
    {
        if (animador == null) return;

        if (Mathf.Abs(entrada.x) > 0.1f)
        {
            animador.VoltearSegunDireccion(entrada.x);
        }

        animador.ReproducirSurf();
    }

    /// <summary>Salto aéreo con trayectoria parabólica y giro de truco.</summary>
    private IEnumerator RealizarSalto()
    {
        EstaSaltando = true;
        tiempoUltimoSalto = Time.time;
        velocidadActual = Vector2.zero;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        if (animador != null)
        {
            animador.ReproducirSalto();
        }

        float xInicial = transform.position.x;
        float tiempo = 0f;

        while (tiempo < duracionSalto)
        {
            tiempo += Time.deltaTime;
            float progreso = Mathf.Clamp01(tiempo / duracionSalto);

            float elevacion = Mathf.Sin(progreso * Mathf.PI) * alturaSalto;
            float avanceX = xInicial + (entrada.x * 1.5f * progreso);
            avanceX = Mathf.Clamp(avanceX, LimiteIzquierdo, LimiteDerecho);

            transform.position = new Vector3(avanceX, maxY + elevacion, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(progreso * Mathf.PI) * rotacionAerea);

            yield return null;
        }

        // Aterrizaje suave en la cresta.
        transform.position = new Vector3(transform.position.x, maxY, 0f);
        transform.rotation = rotacionOriginal;

        if (animador != null)
        {
            animador.ReproducirSurf();
        }

        EstaSaltando = false;
    }

    /// <summary>Aplica daño al surfista. Desde los obstáculos o por contacto directo.</summary>
    public void RecibirDaño(GameObject origenObstaculo)
    {
        if (EsInvulnerable || EstaSaltando || VidasActuales <= 0) return;

        VidasActuales--;
        Debug.Log($"¡IMPACTO con {(origenObstaculo != null ? origenObstaculo.name : "obstáculo")}! Vidas restantes: {VidasActuales}");

        if (GameManager.Instancia != null)
        {
            GameManager.Instancia.ActualizarVidas(VidasActuales);
        }

        if (VidasActuales <= 0)
        {
            if (GameManager.Instancia != null)
            {
                GameManager.Instancia.GameOver();
            }
            StartCoroutine(EfectoDerrota());
        }
        else
        {
            StartCoroutine(RutinaInvulnerabilidad());
        }
    }

    private IEnumerator RutinaInvulnerabilidad()
    {
        EsInvulnerable = true;
        float transcurrido = 0f;
        const float intervaloParpadeo = 0.12f;

        while (transcurrido < tiempoInvulnerabilidad)
        {
            if (sr != null) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(intervaloParpadeo);
            transcurrido += intervaloParpadeo;
        }

        if (sr != null) sr.enabled = true;
        EsInvulnerable = false;
    }

    private IEnumerator EfectoDerrota()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Reproduce la animación de caída (Assets/Resources/Surfista1/Caida).
        if (animador != null)
        {
            animador.ReproducirCaida();
        }

        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            transform.position += Vector3.down * (2f * Time.deltaTime);
            transform.Rotate(0f, 0f, 180f * Time.deltaTime);
            yield return null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        // Respaldo por si el objeto no tiene el script Obstaculo.
        string nombre = other.gameObject.name.ToLower();
        if (nombre.Contains("pulpo") || nombre.Contains("tiburon") || nombre.Contains("tiburón") ||
            nombre.Contains("delfin") || nombre.Contains("delfín") ||
            nombre.Contains("bañista") || nombre.Contains("banista"))
        {
            RecibirDaño(other.gameObject);
        }
    }

    // Límites calculados a partir de la cámara para que el surfista nunca salga del encuadre.
    private float LimiteIzquierdo
    {
        get
        {
            if (!usarAnchoDeCamara || camara == null) return minX;
            return camara.transform.position.x - camara.orthographicSize * camara.aspect + margenLateral;
        }
    }

    private float LimiteDerecho
    {
        get
        {
            if (!usarAnchoDeCamara || camara == null) return maxX;
            return camara.transform.position.x + camara.orthographicSize * camara.aspect - margenLateral;
        }
    }

    // Dibuja el área de juego en la vista de Escena para compararla con tu foto.
    private void OnDrawGizmosSelected()
    {
        Camera cam = camara != null ? camara : (Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>());
        float izquierda = minX;
        float derecha = maxX;

        if (usarAnchoDeCamara && cam != null)
        {
            izquierda = cam.transform.position.x - cam.orthographicSize * cam.aspect + margenLateral;
            derecha = cam.transform.position.x + cam.orthographicSize * cam.aspect - margenLateral;
        }

        Vector3 centro = new Vector3((izquierda + derecha) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector3 tamano = new Vector3(derecha - izquierda, maxY - minY, 0.1f);

        Gizmos.color = new Color(0.1f, 1f, 0.4f, 0.8f);
        Gizmos.DrawWireCube(centro, tamano);

        // Línea de la copa de la ola.
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(izquierda, maxY, 0f), new Vector3(derecha, maxY, 0f));
    }
}
