using UnityEngine;

/// <summary>
/// Animación por fotogramas de las criaturas marinas (delfín, tiburón y bañista).
///
/// Es el mismo sistema que <see cref="AnimadorPulpo"/>: el script cambia el sprite del
/// SpriteRenderer en bucle, así que NO hace falta crear un Animator. Los fotogramas se
/// cargan solos desde Assets/Resources a partir del nombre del objeto:
///   - "Delfin"  -> Assets/Resources/Delfin   (delfin_00, delfin_01, ...)
///   - "Tiburon" -> Assets/Resources/Tiburon  (tiburon_00, ...)
///   - "Bañista" -> Assets/Resources/Banista  (banista_00, ...)
///
/// Esos PNG se generaron desde las tiras "&lt;criatura&gt; completo.jpg" quitándoles el
/// fondo de cuadros (transparencia). En el Inspector puedes reemplazar los fotogramas
/// a mano si quieres otro orden o velocidad.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AnimadorCriaturaMarina : MonoBehaviour
{
    [Header("Carpeta dentro de Assets/Resources")]
    [Tooltip("Se rellena sola según el nombre del objeto (Delfin, Tiburon o Banista).")]
    public string carpetaRecursos = "";

    [Header("Fotogramas")]
    [Tooltip("Se rellenan solos desde la carpeta. Puedes arrastrar imágenes a mano.")]
    public Sprite[] fotogramas;

    [Header("Velocidad de la animación")]
    [Tooltip("Fotogramas por segundo del nado de la criatura.")]
    public float fps = 8f;

    [Header("Tamaño automático")]
    [Tooltip("Reescala el objeto para que la criatura tenga un tamaño coherente en pantalla.")]
    public bool ajustarTamanoAutomatico = true;
    [Tooltip("Altura en unidades del mundo. 0 = se elige sola según la criatura.")]
    public float alturaDeseada = 0f;

    private SpriteRenderer sr;
    private float temporizador;
    private int indice;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        if (string.IsNullOrEmpty(carpetaRecursos))
        {
            carpetaRecursos = DetectarCarpeta();
        }

        if (fotogramas == null || fotogramas.Length == 0)
        {
            CargarFotogramas();
        }

        if (alturaDeseada <= 0f)
        {
            alturaDeseada = AlturaSugerida(carpetaRecursos);
        }

        AjustarEscala();
        MostrarFotograma(0);
    }

    void Update()
    {
        if (fotogramas == null || fotogramas.Length <= 1) return;

        temporizador += Time.deltaTime;
        float duracionFotograma = 1f / Mathf.Max(0.01f, fps);

        while (temporizador >= duracionFotograma)
        {
            temporizador -= duracionFotograma;
            indice = (indice + 1) % fotogramas.Length;
        }

        MostrarFotograma(indice);
    }

    private void MostrarFotograma(int i)
    {
        if (sr == null || fotogramas == null || fotogramas.Length == 0) return;
        sr.sprite = fotogramas[Mathf.Clamp(i, 0, fotogramas.Length - 1)];
    }

    /// <summary>Carga los fotogramas de la carpeta de Resources y los ordena por nombre.</summary>
    public void CargarFotogramas()
    {
        if (string.IsNullOrEmpty(carpetaRecursos)) return;

        fotogramas = Resources.LoadAll<Sprite>(carpetaRecursos);
        if (fotogramas != null)
        {
            System.Array.Sort(fotogramas, (a, b) => string.CompareOrdinal(a.name, b.name));
        }
    }

    /// <summary>Deja la criatura con un tamaño parecido al resto, sin importar la resolución del PNG.</summary>
    public void AjustarEscala()
    {
        if (!ajustarTamanoAutomatico || fotogramas == null || fotogramas.Length == 0) return;

        Sprite referencia = fotogramas[0];
        if (referencia == null) return;

        float altoOriginal = referencia.bounds.size.y;
        if (altoOriginal <= 0.0001f || alturaDeseada <= 0.0001f) return;

        float escala = alturaDeseada / altoOriginal;
        transform.localScale = new Vector3(escala, escala, 1f);
    }

    /// <summary>Deduce la carpeta de Resources a partir del nombre del objeto.</summary>
    private string DetectarCarpeta()
    {
        string nombre = gameObject.name.ToLower();

        if (nombre.Contains("delfin") || nombre.Contains("delfín")) return "Delfin";
        if (nombre.Contains("tiburon") || nombre.Contains("tiburón")) return "Tiburon";
        if (nombre.Contains("bañista") || nombre.Contains("banista")) return "Banista";

        return "";
    }

    /// <summary>Altura razonable por criatura para que el tiburón se vea mayor que el bañista.</summary>
    private static float AlturaSugerida(string carpeta)
    {
        switch (carpeta)
        {
            case "Delfin":  return 1.4f;
            case "Tiburon": return 1.5f;
            case "Banista": return 1.5f;
            default:        return 1.4f;
        }
    }
}
