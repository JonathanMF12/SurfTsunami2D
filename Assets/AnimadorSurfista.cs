using UnityEngine;

/// <summary>
/// Animación por fotogramas para el surfista. Los fotogramas se cargan
/// automáticamente desde:
///   - Assets/Resources/Surfista1/Surf  -> animación de surf (bucle normal)
///   - Assets/Resources/Surfista1/Caida -> animación de caída (game over)
///
/// También puedes arrastrar las imágenes a mano a los arrays del Inspector.
/// No hace falta crear un Animator.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AnimadorSurfista : MonoBehaviour
{
    [Header("Fotogramas de Surf (bucle normal)")]
    [Tooltip("Se rellena solo desde Assets/Resources/Surfista1/Surf. Puedes arrastrar imágenes a mano.")]
    public Sprite[] fotogramasSurf;

    [Header("Fotogramas de Salto (opcional)")]
    [Tooltip("Si lo dejas vacío, durante el salto se siguen usando los fotogramas de surf.")]
    public Sprite[] fotogramasSalto;

    [Header("Fotogramas de Caída (game over)")]
    [Tooltip("Se rellena solo desde Assets/Resources/Surfista1/Caida. Si está vacío se usa el último fotograma de surf.")]
    public Sprite[] fotogramasCaida;

    [Header("Velocidad de la animación")]
    [Tooltip("Fotogramas por segundo en modo surf.")]
    public float fpsSurf = 8f;
    [Tooltip("Fotogramas por segundo en el salto.")]
    public float fpsSalto = 12f;
    [Tooltip("Fotogramas por segundo en la caída.")]
    public float fpsCaida = 6f;

    [Header("Tamaño automático")]
    [Tooltip("Reescala el objeto para que cada conjunto de fotogramas tenga la MISMA altura en pantalla. " +
             "Útil porque tus imágenes de surf y de caída tienen resoluciones distintas.")]
    public bool ajustarTamanoAutomatico = true;
    [Tooltip("Altura que tendrá el sprite en unidades del mundo.")]
    public float alturaDeseada = 1.6f;

    private SpriteRenderer sr;
    private Sprite[] fotogramasActuales;
    private float fpsActual = 8f;
    private float temporizador;
    private int indice;
    private bool enBucle = true;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        Cambiar(fotogramasSurf, fpsSurf, true);
    }

    /// <summary>Reproduce el ciclo de surf (se llama constantemente mientras surfea).</summary>
    public void ReproducirSurf()
    {
        Cambiar(fotogramasSurf, fpsSurf, true);
    }

    /// <summary>Reproduce el ciclo de salto (si no hay fotogramas de salto, mantiene el de surf).</summary>
    public void ReproducirSalto()
    {
        if (fotogramasSalto != null && fotogramasSalto.Length > 0)
        {
            Cambiar(fotogramasSalto, fpsSalto, true);
        }
    }

    /// <summary>Reproduce la caída (game over). Se queda en el último fotograma.</summary>
    public void ReproducirCaida()
    {
        if (fotogramasCaida != null && fotogramasCaida.Length > 0)
        {
            Cambiar(fotogramasCaida, fpsCaida, false);
        }
        else if (fotogramasSurf != null && fotogramasSurf.Length > 0)
        {
            // Sin fotogramas de caída: mantén el primero de surf congelado.
            MostrarFotograma(fotogramasSurf[0]);
        }
    }

    /// <summary>Voltea el sprite según la dirección horizontal del movimiento (-1 izquierda, 1 derecha).</summary>
    public void VoltearSegunDireccion(float direccionX)
    {
        if (sr != null && Mathf.Abs(direccionX) > 0.01f)
        {
            sr.flipX = direccionX < 0f;
        }
    }

    private void Cambiar(Sprite[] conjunto, float fps, bool bucle)
    {
        if (conjunto == null || conjunto.Length == 0) return;

        // Si ya estamos reproduciendo el mismo conjunto no reiniciamos el fotograma.
        if (fotogramasActuales == conjunto && enBucle == bucle) return;

        fotogramasActuales = conjunto;
        fpsActual = Mathf.Max(0.01f, fps);
        enBucle = bucle;
        indice = 0;
        temporizador = 0f;
        MostrarFotograma(fotogramasActuales[0]);
        AjustarEscala(fotogramasActuales[0]);
    }

    /// <summary>
    /// Reescala el objeto para que el fotograma de referencia tenga la altura deseada.
    /// Así los fotogramas de surf y de caída (que pueden tener distinta resolución)
    /// se ven del mismo tamaño.
    /// </summary>
    private void AjustarEscala(Sprite referencia)
    {
        if (!ajustarTamanoAutomatico || referencia == null) return;

        float altoOriginal = referencia.bounds.size.y;
        if (altoOriginal <= 0.0001f || alturaDeseada <= 0.0001f) return;

        float escala = alturaDeseada / altoOriginal;
        transform.localScale = new Vector3(escala, escala, 1f);
    }

    private void MostrarFotograma(Sprite s)
    {
        if (sr != null && s != null) sr.sprite = s;
    }

    void Update()
    {
        if (fotogramasActuales == null || fotogramasActuales.Length <= 1) return;

        temporizador += Time.deltaTime;
        float duracionFotograma = 1f / fpsActual;

        while (temporizador >= duracionFotograma)
        {
            temporizador -= duracionFotograma;

            if (enBucle)
            {
                indice = (indice + 1) % fotogramasActuales.Length;
            }
            else if (indice < fotogramasActuales.Length - 1)
            {
                indice++; // se detiene en el último fotograma
            }
            else
            {
                break;
            }
        }

        MostrarFotograma(fotogramasActuales[indice]);
    }
}
