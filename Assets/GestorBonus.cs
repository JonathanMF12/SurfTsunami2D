using UnityEngine;

/// <summary>
/// Controla <b>cuándo</b> aparece un objeto de bonus y con <b>qué sprite</b>.
///
/// A diferencia de antes (donde el bonus reaparecía nada más recogerlo o salir de
/// pantalla), este gestor hace que aparezca <b>de vez en cuando</b> (por defecto
/// ~1 vez por minuto) y va <b>alternando</b> entre los sprites que le asignes
/// (las 5 tablas de <c>Assets/Sprites/Bonus</c>) para que se vea colorido.
///
/// Crea su propio objeto de bonus en tiempo de ejecución, así que no hace falta
/// colocar nada más en la escena: solo este componente con la lista de sprites.
/// </summary>
[DisallowMultipleComponent]
public class GestorBonus : MonoBehaviour
{
    [Header("Sprites (se van alternando)")]
    [Tooltip("Las 5 tablas de Assets/Sprites/Bonus. Si lo dejas vacío se cargan de Resources/Bonus.")]
    public Sprite[] sprites;

    [Header("Ritmo de aparición")]
    [Tooltip("Segundos que espera antes del primer bonus de la partida.")]
    public float retrasoInicial = 30f;

    [Tooltip("Segundos entre un bonus y el siguiente (~1 minuto).")]
    public float intervaloAparicion = 60f;

    [Tooltip("Variación aleatoria del intervalo para que no sea exacto (segundos).")]
    public float variacionIntervalo = 8f;

    [Header("Puntos y movimiento")]
    public int puntosBonus = 250;
    [Tooltip("Velocidad a la que cruza la pantalla.")]
    public float velocidad = 3.5f;

    [Header("Zona donde aparece/cruza")]
    public float limiteDerechoMin = 14f;
    public float limiteDerechoMax = 22f;
    public float minY = -3.6f;
    public float maxY = -1.0f;
    public float limiteIzquierdo = -14f;

    [Header("Tamaño")]
    [Tooltip("Escala del sprite (los PNG de bonus vienen a 100 px por unidad).")]
    public float escalaVisual = 0.1f;
    [Tooltip("Qué proporción del sprite ocupa el colisionador de recogida.")]
    public float factorColisionador = 0.8f;

    private GameObject objeto;
    private SpriteRenderer sr;
    private CircleCollider2D col;
    private ItemBonus item;

    private float temporizador;
    private int indice;

    void Start()
    {
        if (sprites == null || sprites.Length == 0)
        {
            sprites = Resources.LoadAll<Sprite>("Bonus");
            if (sprites != null)
            {
                System.Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
            }
        }

        CrearObjeto();

        temporizador = Mathf.Max(1f, retrasoInicial);
        Ocultar();
    }

    void Update()
    {
        if (GameManager.Instancia != null && GameManager.Instancia.juegoTerminado) return;

        temporizador -= Time.deltaTime;
        if (temporizador > 0f) return;

        Mostrar();

        float variacion = Random.Range(-Mathf.Abs(variacionIntervalo), Mathf.Abs(variacionIntervalo));
        temporizador = Mathf.Max(5f, intervaloAparicion + variacion);
    }

    /// <summary>Crea el objeto de bonus (una sola vez) y lo deja configurado.</summary>
    private void CrearObjeto()
    {
        objeto = new GameObject("Bonus");
        objeto.SetActive(false);

        sr = objeto.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 6;

        col = objeto.AddComponent<CircleCollider2D>();
        col.isTrigger = true;

        item = objeto.AddComponent<ItemBonus>();
        item.reaparecerAutomaticamente = false; // Lo controla este gestor.
        item.puntosBonus = puntosBonus;
        item.velocidad = velocidad;
        item.limiteIzquierdo = limiteIzquierdo;

        objeto.transform.localScale = Vector3.one * escalaVisual;

        if (sprites != null && sprites.Length > 0)
        {
            sr.sprite = sprites[0];
            AjustarCollider();
        }
    }

    /// <summary>Ajusta el colisionador al tamaño del sprite visible.</summary>
    private void AjustarCollider()
    {
        if (col == null || sr == null || sr.sprite == null) return;

        float ladoMenor = Mathf.Min(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
        col.offset = Vector2.zero;
        col.radius = Mathf.Max(0.2f, ladoMenor * 0.5f * factorColisionador);
    }

    /// <summary>Saca el siguiente sprite (alternando) y muestra el bonus a la derecha.</summary>
    private void Mostrar()
    {
        if (objeto == null) return;

        objeto.SetActive(true);

        if (sprites != null && sprites.Length > 0 && sr != null)
        {
            sr.sprite = sprites[indice];
            indice = (indice + 1) % sprites.Length;
            AjustarCollider();
        }

        float x = Random.Range(limiteDerechoMin, limiteDerechoMax);
        float y = Random.Range(minY, maxY);
        objeto.transform.position = new Vector3(x, y, 0f);
        objeto.transform.rotation = Quaternion.identity;

        if (sr != null) sr.enabled = true;
        if (col != null) col.enabled = true;
        if (item != null)
        {
            item.puntosBonus = puntosBonus;
            item.velocidad = velocidad;
            item.enabled = true;
        }
    }

    /// <summary>Oculta el bonus sin apagar este gestor (para que siga contando el tiempo).</summary>
    private void Ocultar()
    {
        if (sr != null) sr.enabled = false;
        if (col != null) col.enabled = false;
        if (item != null) item.enabled = false;
    }
}
