using UnityEngine;

/// <summary>
/// Controla los objetos de bonificación (como las tablas de surf y estrellas) que suman puntos al recogerlos.
/// </summary>
public class ItemBonus : MonoBehaviour
{
    [Header("Configuración")]
    public float velocidad = 3.5f;
    public int puntosBonus = 250;

    public float limiteIzquierdo = -12f;
    public float limiteDerechoMin = 14f;
    public float limiteDerechoMax = 24f;
    public float minY = -3.8f;
    public float maxY = -1.2f;

    [Header("Modo de aparición")]
    [Tooltip("Si está activo, al recogerlo o salir de pantalla reaparece a la derecha. " +
             "Apágalo si lo controla un GestorBonus (aparece de vez en cuando).")]
    public bool reaparecerAutomaticamente = true;

    private float baseY;
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }

        baseY = transform.position.y;
    }

    void Update()
    {
        if (GameManager.Instancia != null && GameManager.Instancia.juegoTerminado) return;

        // Desplazamiento a la izquierda
        transform.position += Vector3.left * (velocidad * Time.deltaTime);

        // Ligero balanceo visual
        transform.Rotate(0f, 0f, Mathf.Sin(Time.time * 4f) * 0.5f);

        if (transform.position.x < limiteIzquierdo)
        {
            if (reaparecerAutomaticamente) Reaparecer();
            else Ocultar();
        }
    }

    public void Reaparecer()
    {
        baseY = Random.Range(minY, maxY);
        float nuevoX = Random.Range(limiteDerechoMin, limiteDerechoMax);
        transform.position = new Vector3(nuevoX, baseY, 0f);
        gameObject.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        SurfistaController surfista = other.GetComponent<SurfistaController>();
        if (surfista != null)
        {
            if (GameManager.Instancia != null)
            {
                GameManager.Instancia.SumarPuntaje(puntosBonus);
                Debug.Log($"¡BONUS RECOGIDO! +{puntosBonus} puntos.");
            }

            if (reaparecerAutomaticamente) Reaparecer();
            else Ocultar();
        }
    }

    /// <summary>
    /// Oculta el bonus (sin apagar el objeto, para no detener al GestorBonus que lo
    /// administra). Lo vuelve a mostrar el GestorBonus cuando toque.
    /// </summary>
    public void Ocultar()
    {
        if (sr != null) sr.enabled = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        enabled = false;
    }
}
