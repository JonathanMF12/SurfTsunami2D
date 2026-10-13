using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Música de fondo del juego. Se crea sola al arrancar (no hay que colocarla en ninguna
/// escena) y se mantiene entre escenas con <see cref="Object.DontDestroyOnLoad(Object)"/>,
/// así el tema no se corta al pasar del menú al juego ni al reiniciar con R.
///
/// Cómo usar TU propia música (opcional):
///   1. Crea la carpeta <c>Assets/Resources/Audio</c>.
///   2. Mete tu archivo ahí y llámalo <c>musica</c> (por ejemplo <c>musica.ogg</c>,
///      <c>musica.mp3</c> o <c>musica.wav</c>).
///   3. Listo: el juego lo detecta y deja de generar el tema por código.
///
/// Si no hay archivo, se compone un tema playero en bucle con <see cref="GeneradorMusica"/>.
///
/// El estado ON/OFF se guarda en PlayerPrefs con la clave <see cref="ClaveMusica"/> y se
/// controla desde el panel de OPCIONES de la portada (botón "MÚSICA").
/// </summary>
[DisallowMultipleComponent]
public class MusicaFondo : MonoBehaviour
{
    /// <summary>Clave de PlayerPrefs donde se guarda si la música está activada (1/0).</summary>
    public const string ClaveMusica = "op_musica";

    /// <summary>Instancia activa (solo hay una en toda la partida).</summary>
    public static MusicaFondo Instancia { get; private set; }

    [Header("Archivo de música (opcional)")]
    [Tooltip("Arrastra aquí tu AudioClip si prefieres asignarlo a mano. Si lo dejas vacío " +
             "se busca un archivo en Resources con la ruta de abajo.")]
    public AudioClip clipPersonalizado;

    [Tooltip("Ruta dentro de la carpeta Resources donde se busca tu archivo de música.")]
    public string rutaResources = "Audio/musica";

    [Range(0f, 1f)]
    [Tooltip("Volumen general de la música.")]
    public float volumen = 0.5f;

    [Header("Tema generado (si no hay archivo)")]
    [Tooltip("Velocidad del tema principal compuesto por código (más alto = más rápido y alegre).")]
    public float bpm = 138f;

    private AudioSource fuente;
    private AudioClip clipJuego;
    private AudioClip clipGameOver;
    private bool activa = true;
    private bool enGameOver;

    // Se ejecuta una sola vez por partida, antes de cargar la primera escena, para que
    // la música empiece ya en la portada y sobreviva a los cambios de escena.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CrearAutomaticamente()
    {
        if (FindAnyObjectByType<MusicaFondo>() != null) return;

        GameObject go = new GameObject("MusicaFondo");
        go.AddComponent<MusicaFondo>();
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        DontDestroyOnLoad(gameObject);

        fuente = gameObject.GetComponent<AudioSource>();
        if (fuente == null) fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = true;
        fuente.spatialBlend = 0f; // 2D: se oye igual en todo el escenario.
        fuente.volume = volumen;

        PrepararClips();

        activa = PlayerPrefs.GetInt(ClaveMusica, 1) == 1;
        AplicarEstado();

        SceneManager.sceneLoaded += AlCargarEscena;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        if (Instancia == this) Instancia = null;
    }

    private void PrepararClips()
    {
        AudioClip clip = clipPersonalizado;

        if (clip == null && !string.IsNullOrEmpty(rutaResources))
        {
            clip = Resources.Load<AudioClip>(rutaResources);
        }

        if (clip != null)
        {
            // Si el jugador aporta su propia música, se usa para el juego y no se
            // pisa con el jingle generado de Game Over.
            clipJuego = clip;
            clipGameOver = null;
        }
        else
        {
            clipJuego = GeneradorMusica.CrearTemaJuego(bpm);
            clipGameOver = GeneradorMusica.CrearTemaGameOver();
        }

        if (clipJuego != null) fuente.clip = clipJuego;
    }

    /// <summary>
    /// Repone el tema principal del juego (rápido y alegre). Se llama al empezar la partida
    /// y cada vez que se carga una escena (reinicio o vuelta al menú).
    /// </summary>
    public void TocarTemaJuego()
    {
        enGameOver = false;
        if (fuente == null || clipJuego == null) return;

        fuente.loop = true;
        if (fuente.clip != clipJuego)
        {
            fuente.clip = clipJuego;
            fuente.Stop();
        }
        AplicarEstado();
    }

    /// <summary>
    /// Cambia al jingle cómico de "perdiste" (una sola vez, sin bucle). Si el jugador
    /// aportó su propia música no hay jingle y se deja sonar la suya.
    /// </summary>
    public void TocarGameOver()
    {
        if (clipGameOver == null || fuente == null) return;

        enGameOver = true;
        fuente.loop = false;
        fuente.clip = clipGameOver;
        fuente.Stop();
        if (activa) fuente.Play();
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        // Al reiniciar o volver al menú, el jingle de Game Over deja paso al tema del juego.
        if (enGameOver) TocarTemaJuego();
    }

    /// <summary>True si la música está activada.</summary>
    public bool EstaActiva => activa;

    /// <summary>Enciende o apaga la música y guarda la preferencia.</summary>
    public void EstablecerActiva(bool valor)
    {
        activa = valor;
        PlayerPrefs.SetInt(ClaveMusica, activa ? 1 : 0);
        PlayerPrefs.Save();
        AplicarEstado();
    }

    /// <summary>Alterna la música (ON <-> OFF).</summary>
    public void Alternar()
    {
        EstablecerActiva(!activa);
    }

    private void AplicarEstado()
    {
        if (fuente == null || fuente.clip == null) return;

        if (activa)
        {
            if (!fuente.isPlaying) fuente.Play();
        }
        else if (fuente.isPlaying)
        {
            fuente.Stop();
        }
    }
}
