using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Configura automáticamente la escena al presionar Play:
/// - Asigna SurfistaController a Surfista1 con sus físicas y límites.
/// - Asigna Obstaculo a Pulpo, Tiburón, Delfín y Bañista, y los distribuye a la derecha para que no inicien colisionando.
/// - Añade la animación por fotogramas: AnimadorPulpo para el pulpo y AnimadorCriaturaMarina
///   (carga sola los PNG de Assets/Resources) para el delfín, el tiburón y el bañista.
/// - Asigna ItemBonus a las tablas de bonus.
/// - Inicializa el GameManager con las vidas y puntaje.
/// </summary>
public static class AutoConfiguradorJuego
{
    // Nos suscribimos ANTES de que cargue la primera escena para poder configurar la
    // escena de juego CADA VEZ que se carga (también al reiniciar con la tecla R).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarCargaDeEscenas()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        InicializarEscenaAutomaticamente();
    }

    // Red de seguridad por si el evento sceneLoaded no se dispara para la primera escena.
    // La configuración es idempotente, así que ejecutarla dos veces no causa problemas.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ConfigurarPrimeraEscena()
    {
        InicializarEscenaAutomaticamente();
    }

    static void InicializarEscenaAutomaticamente()
    {
        // Identificamos al jugador. Si no existe, no es la escena de juego (p. ej. el menú).
        GameObject surfistaGO = GameObject.Find("Surfista1");
        if (surfistaGO == null) surfistaGO = GameObject.FindWithTag("Player");
        if (surfistaGO == null) return;

        // 1. Asegurar GameManager
        _ = GameManager.Instancia;

        // 2. Configurar el Surfista (Player 1)
        if (surfistaGO != null)
        {
            if (surfistaGO.GetComponent<SurfistaController>() == null)
            {
                surfistaGO.AddComponent<SurfistaController>();
            }

            // Animación de sprites: agregar el animador y rellenar los fotogramas
            // automáticamente desde Assets/Resources/Surfista1.
            AnimadorSurfista animador = surfistaGO.GetComponent<AnimadorSurfista>();
            if (animador == null)
            {
                animador = surfistaGO.AddComponent<AnimadorSurfista>();
            }
            RellenarFotogramasSurfista(animador);
            AjustarTamanoSurfista(surfistaGO, animador);

            SpriteRenderer sr = surfistaGO.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 10; // Que el surfista siempre esté al frente
            }

            // Colocar al surfista a la izquierda de la pantalla preparado para surfear
            if (Mathf.Abs(surfistaGO.transform.position.x) < 0.1f)
            {
                surfistaGO.transform.position = new Vector3(-4f, -2.5f, 0f);
            }
        }

        // 2b. Controles en pantalla para móvil/táctil (joystick + botón de salto).
        //     Solo se muestran en dispositivos táctiles; en escritorio quedan ocultos.
        if (Object.FindAnyObjectByType<ControladorTactil>() == null)
        {
            GameObject controles = new GameObject("ControlesTactiles");
            controles.AddComponent<ControladorTactil>();
        }

        // 3. Configurar Obstáculos (Pulpo, Bañista, Delfín, Tiburón)
        GameObject[] todosLosObjetos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
        int indiceObstaculo = 0;
        int indiceBonus = 0;

        foreach (GameObject go in todosLosObjetos)
        {
            if (go == null) continue;

            // IMPORTANTE: nunca tratar al jugador como obstáculo.
            if (go == surfistaGO) continue;

            string nombre = go.name.ToLower();

            // Identificar obstáculos colisionadores
            if (nombre.Contains("pulpo") || nombre.Contains("tiburon") || nombre.Contains("delfin") || 
                nombre.Contains("bañista") || nombre.Contains("banista"))
            {
                if (go.GetComponent<Obstaculo>() == null)
                {
                    go.AddComponent<Obstaculo>();
                }
                if (go.GetComponent<AnimadorObstaculo>() == null)
                {
                    go.AddComponent<AnimadorObstaculo>();
                }

                // Animación por fotogramas:
                //  - El pulpo usa su propio componente (AnimadorPulpo).
                //  - El delfín, el tiburón y el bañista usan AnimadorCriaturaMarina,
                //    que carga los PNG de Assets/Resources/<criatura> en su Awake.
                if (nombre.Contains("pulpo"))
                {
                    if (go.GetComponent<AnimadorPulpo>() == null)
                    {
                        go.AddComponent<AnimadorPulpo>();
                    }
                }
                else if (go.GetComponent<AnimadorCriaturaMarina>() == null)
                {
                    go.AddComponent<AnimadorCriaturaMarina>();
                }

                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 5;
                }

                // Si está en el origen (0, -3), distribuirlo hacia la derecha fuera de pantalla
                if (Mathf.Abs(go.transform.position.x) < 0.5f)
                {
                    // Espaciar un poco más para evitar huecos pero dar tiempo a esquivar
                    float inicioX = 10f + (indiceObstaculo * 8.5f);
                    float inicioY = Random.Range(-3.5f, -1.2f);
                    go.transform.position = new Vector3(inicioX, inicioY, 0f);
                    indiceObstaculo++;
                }
            }
            // Identificar tablas de bonus
            else if (nombre.Contains("bonus"))
            {
                if (go.GetComponent<ItemBonus>() == null)
                {
                    go.AddComponent<ItemBonus>();
                }

                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 6;
                }

                if (Mathf.Abs(go.transform.position.x) < 0.5f)
                {
                    float inicioX = 13f + (indiceBonus * 9f);
                    float inicioY = Random.Range(-3.5f, -1.2f);
                    go.transform.position = new Vector3(inicioX, inicioY, 0f);
                    indiceBonus++;
                }
            }
        }

        Debug.Log("¡Escena configurada automáticamente con Surfista, Obstáculos distribuidos, Bonus y GameManager!");
    }

    /// <summary>
    /// Carga los fotogramas del surfista desde Assets/Resources/Surfista1:
    ///   - Surfista1/Surf  -> animación de surf.
    ///   - Surfista1/Caida -> animación de caída.
    /// Funciona tanto en el Editor como en las compilaciones (builds).
    /// En el Inspector puedes reemplazar los arrays a mano si quieres otro orden.
    /// </summary>
    private static void RellenarFotogramasSurfista(AnimadorSurfista animador)
    {
        if (animador == null) return;

        if (animador.fotogramasSurf == null || animador.fotogramasSurf.Length == 0)
        {
            animador.fotogramasSurf = CargarFotogramas("Surfista1/Surf");
            Debug.Log($"[AutoConfig] Fotogramas de surf: {animador.fotogramasSurf.Length}.");
        }

        if (animador.fotogramasCaida == null || animador.fotogramasCaida.Length == 0)
        {
            animador.fotogramasCaida = CargarFotogramas("Surfista1/Caida");
            Debug.Log($"[AutoConfig] Fotogramas de caída: {animador.fotogramasCaida.Length}.");
        }
    }

    /// <summary>
    /// Devuelve los sprites de una carpeta de Resources, ordenados por nombre
    /// (surf_00, surf_01, ...). Funciona en el Editor y en la compilación.
    /// </summary>
    private static Sprite[] CargarFotogramas(string carpetaResources)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(carpetaResources);
        if (sprites == null) return new Sprite[0];

        System.Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites;
    }

    /// <summary>
    /// Ajusta la escala del surfista para que el personaje tenga un tamaño parecido al
    /// de los obstáculos, y encoge su colisionador para que coincida con el sprite.
    /// Así da igual si los fotogramas son de 240 px o de 1000 px.
    /// </summary>
    private static void AjustarTamanoSurfista(GameObject surfistaGO, AnimadorSurfista animador)
    {
        if (surfistaGO == null || animador == null) return;
        if (animador.fotogramasSurf == null || animador.fotogramasSurf.Length == 0) return;

        Sprite s = animador.fotogramasSurf[0];
        if (s == null) return;

        float altoOriginal = s.bounds.size.y; // unidades del mundo a escala 1
        if (altoOriginal > 0.0001f)
        {
            const float alturaDeseada = 1.6f;
            float escala = alturaDeseada / altoOriginal;
            surfistaGO.transform.localScale = new Vector3(escala, escala, 1f);
            animador.alturaDeseada = alturaDeseada;
        }

        // Colisionador acorde al sprite (el de la escena era enorme: 10x10).
        Collider2D col = surfistaGO.GetComponent<Collider2D>();
        if (col != null)
        {
            Vector2 tam = new Vector2(s.bounds.size.x * 0.6f, s.bounds.size.y * 0.6f);
            BoxCollider2D caja = col as BoxCollider2D;
            CapsuleCollider2D capsula = col as CapsuleCollider2D;

            if (caja != null) caja.size = tam;
            else if (capsula != null) capsula.size = tam;
        }
    }
}
