using UnityEngine;

/// <summary>
/// Animación por fotogramas del pulpo. Los fotogramas se cargan automáticamente desde
/// Assets/Resources/Pulpo (pulpo_00, pulpo_01, ...), que a su vez se generaron a partir
/// de la tira "pulpo completo.jpg" quitándole el fondo de cuadros (transparencia).
///
/// No hace falta crear un Animator: igual que AnimadorSurfista, el script cambia el
/// sprite del SpriteRenderer en bucle.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AnimadorPulpo : MonoBehaviour
{
    [Header("Fotogramas del pulpo")]
    [Tooltip("Se rellena solo desde Assets/Resources/Pulpo. Puedes arrastrar imágenes a mano.")]
    public Sprite[] fotogramas;

    [Header("Velocidad de la animación")]
    [Tooltip("Fotogramas por segundo del nado del pulpo.")]
    public float fps = 8f;

    [Header("Tamaño automático")]
    [Tooltip("Reescala el objeto para que el pulpo tenga la altura indicada en unidades del mundo.")]
    public bool ajustarTamanoAutomatico = true;
    [Tooltip("Altura que tendrá el pulpo en pantalla.")]
    public float alturaDeseada = 1.4f;

    private const string CarpetaRecursos = "Pulpo";

    private SpriteRenderer sr;
    private float temporizador;
    private int indice;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        if (fotogramas == null || fotogramas.Length == 0)
        {
            fotogramas = Resources.LoadAll<Sprite>(CarpetaRecursos);
            if (fotogramas != null)
            {
                System.Array.Sort(fotogramas, (a, b) => string.CompareOrdinal(a.name, b.name));
            }
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

    /// <summary>Deja el pulpo del mismo tamaño que los demás obstáculos, sin importar la resolución del PNG.</summary>
    private void AjustarEscala()
    {
        if (!ajustarTamanoAutomatico || fotogramas == null || fotogramas.Length == 0) return;

        Sprite referencia = fotogramas[0];
        if (referencia == null) return;

        float altoOriginal = referencia.bounds.size.y;
        if (altoOriginal <= 0.0001f || alturaDeseada <= 0.0001f) return;

        float escala = alturaDeseada / altoOriginal;
        transform.localScale = new Vector3(escala, escala, 1f);
    }
}
