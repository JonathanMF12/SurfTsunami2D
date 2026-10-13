using UnityEngine;

/// <summary>
/// Controla el movimiento, la oscilación en el agua, la colisión y la puntuación por esquivar los obstáculos marítimos.
/// </summary>
public class Obstaculo : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [Tooltip("Velocidad de avance hacia la izquierda")]
    public float velocidad = 4f;

    [Tooltip("Puntos que se otorgan al esquivar este obstáculo")]
    public int puntosPorEsquivar = 100;

    [Header("Efecto de Ondulación / Nado")]
    public bool ondularEnAgua = true;
    public float amplitudOndulacion = 0.25f;
    public float frecuenciaOndulacion = 2.5f;

    [Header("Límites de Pantalla y Reaparición")]
    public float limiteIzquierdo = -12f;
    public float limiteDerechoMin = 10f;
    public float limiteDerechoMax = 18f;
    public float minY = -3.8f;
    public float maxY = -1.2f;

    [Header("Colisionador")]
    [Tooltip("Tamaño del colisionador respecto al sprite visible. 1 = igual al dibujo; " +
             "un poco menos (0.7) evita que el daño ocurra cuando todavía hay un hueco.")]
    [Range(0.1f, 1.5f)]
    public float factorColisionador = 0.7f;

    private float baseY;
    private bool colisionadorAjustado = false;
    private float tiempoInicio;
    private bool esquivado = false;
    private bool colisionado = false;
    private Transform transformJugador;

    void Awake()
    {
        // Asegurar que tenga un Collider2D configurado como Trigger para no atascar físicamente
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.5f, 1f);
        }
        else
        {
            col.isTrigger = true;
        }

        baseY = transform.position.y;
        tiempoInicio = Random.Range(0f, 10f); // Desfase aleatorio para que no todos naden sincronizados
    }

    void Start()
    {
        BuscarJugador();

        // Ajustar velocidades específicas según el tipo de criatura si no se han personalizado
        string nombre = gameObject.name.ToLower();
        if (nombre.Contains("tiburon"))
        {
            velocidad = 4.2f;
            amplitudOndulacion = 0.12f;
            frecuenciaOndulacion = 2.2f;
        }
        else if (nombre.Contains("delfin"))
        {
            velocidad = 3.8f;
            amplitudOndulacion = 0.35f; // Los delfines ondulan un poco más
            frecuenciaOndulacion = 2.8f;
        }
        else if (nombre.Contains("pulpo"))
        {
            velocidad = 3.0f;
            amplitudOndulacion = 0.25f;
            frecuenciaOndulacion = 2.0f;
        }
        else if (nombre.Contains("bañista") || nombre.Contains("banista") || nombre.Contains("ista"))
        {
            velocidad = 3.0f;
            amplitudOndulacion = 0.08f;
            frecuenciaOndulacion = 1.8f;
        }

        // El colisionador de estas criaturas venía de la escena con un tamaño enorme
        // (más grande que el dibujo real), así que el surfista perdía vidas cuando la
        // criatura todavía parecía lejos. Lo ajustamos al sprite que se ve.
        AjustarColisionadorAlSprite();
    }

    /// <summary>
    /// Encaja el BoxCollider2D con el sprite que realmente se dibuja. Los fotogramas de
    /// las criaturas los coloca su animador en Awake, por eso se hace aquí (en Start) y no
    /// en Awake. Se aplica una sola vez.
    /// </summary>
    private void AjustarColisionadorAlSprite()
    {
        if (colisionadorAjustado) return;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        // sprite.bounds.size está en unidades del mundo a escala 1; como el colisionador
        // se escala igual que el sprite, basta con usar la misma medida (por el factor).
        Vector2 tamanoDibujo = sr.sprite.bounds.size;
        Vector2 tamanoColisionador = tamanoDibujo * factorColisionador;

        BoxCollider2D caja = GetComponent<BoxCollider2D>();
        if (caja != null)
        {
            caja.size = tamanoColisionador;
            caja.offset = Vector2.zero;
            colisionadorAjustado = true;
            return;
        }

        CapsuleCollider2D capsula = GetComponent<CapsuleCollider2D>();
        if (capsula != null)
        {
            capsula.size = tamanoColisionador;
            capsula.offset = Vector2.zero;
            colisionadorAjustado = true;
            return;
        }

        CircleCollider2D circulo = GetComponent<CircleCollider2D>();
        if (circulo != null)
        {
            circulo.radius = Mathf.Min(tamanoDibujo.x, tamanoDibujo.y) * 0.5f * factorColisionador;
            circulo.offset = Vector2.zero;
            colisionadorAjustado = true;
        }
    }

    void Update()
    {
        if (GameManager.Instancia != null && GameManager.Instancia.juegoTerminado) return;

        // 1. Desplazamiento hacia la izquierda
        float desplazamientoX = velocidad * Time.deltaTime;
        float nuevoX = transform.position.x - desplazamientoX;

        // 2. Ondulación vertical suave (efecto ola/nado)
        float nuevoY = baseY;
        if (ondularEnAgua)
        {
            nuevoY = baseY + Mathf.Sin((Time.time + tiempoInicio) * frecuenciaOndulacion) * amplitudOndulacion;
            nuevoY = Mathf.Clamp(nuevoY, minY, maxY + 0.5f);
        }

        transform.position = new Vector3(nuevoX, nuevoY, 0f);

        // 3. Comprobar si el jugador esquivó con éxito el obstáculo
        VerificarEsquive();

        // 4. Si el obstáculo salió de la pantalla por la izquierda, reciclarlo hacia la derecha
        if (transform.position.x < limiteIzquierdo)
        {
            Reaparecer();
        }
    }

    void BuscarJugador()
    {
        SurfistaController surfista = FindAnyObjectByType<SurfistaController>();
        if (surfista != null)
        {
            transformJugador = surfista.transform;
            return;
        }

        GameObject player = GameObject.Find("Surfista1");
        if (player == null) player = GameObject.FindWithTag("Player");
        if (player != null) transformJugador = player.transform;
    }

    void VerificarEsquive()
    {
        if (esquivado || colisionado) return;

        if (transformJugador == null)
        {
            BuscarJugador();
            if (transformJugador == null) return;
        }

        // Si el obstáculo superó al jugador hacia la izquierda por al menos 0.6 unidades sin haber chocado
        if (transform.position.x < transformJugador.position.x - 0.6f)
        {
            esquivado = true;
            if (GameManager.Instancia != null)
            {
                GameManager.Instancia.SumarPuntaje(puntosPorEsquivar);
            }
        }
    }

    public void Reaparecer()
    {
        esquivado = false;
        colisionado = false;
        baseY = Random.Range(minY, maxY);
        float nuevoX = Random.Range(limiteDerechoMin, limiteDerechoMax);
        transform.position = new Vector3(nuevoX, baseY, 0f);
        gameObject.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (colisionado) return;

        SurfistaController surfista = other.GetComponent<SurfistaController>();
        if (surfista != null)
        {
            // Solo impacta si el surfista NO está en el aire saltando
            if (!surfista.EstaSaltando && !surfista.EsInvulnerable)
            {
                colisionado = true;
                surfista.RecibirDaño(gameObject);

                // Reaparecer el obstáculo tras impactar para no quedarse estorbando
                Invoke(nameof(Reaparecer), 0.5f);
            }
        }
    }
}
