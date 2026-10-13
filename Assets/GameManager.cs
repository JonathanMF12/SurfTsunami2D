using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Administrador del estado del juego adaptado a UI por Canvas con Sprites personalizados.
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instancia;
    public static GameManager Instancia
    {
        get
        {
            if (_instancia == null)
            {
                _instancia = FindAnyObjectByType<GameManager>();
                if (_instancia == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instancia = go.AddComponent<GameManager>();
                }
            }
            return _instancia;
        }
    }

    [Header("Estado del Juego")]
    public int vidasIniciales = 3;
    public int vidasActuales = 3;
    public int puntaje = 0;
    public bool juegoTerminado = false;

    [Tooltip("Escena de la portada a la que se vuelve con ESC/M desde el Game Over.")]
    public string nombreEscenaMenu = "Inicio";

    [Header("Referencias de UI en Canvas")]
    public Text textoVidas;
    public Text textoPuntaje;
    public GameObject panelGameOver;
    public Text textoPuntajeFinal;
    public Image imagenGameOver;
    public Image fondoGameOver;
    public Color colorFondoGameOver = new Color(0f, 0f, 0f, 0.6f);

    [Header("Opcional: Corazones por Imagen (Sprites)")]
    public Image[] imagenesCorazones; // Arrastra las 3 imágenes de corazones del Canvas aquí

    void Awake()
    {
        if (_instancia == null)
        {
            _instancia = this;
        }
        else if (_instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        vidasActuales = vidasIniciales;
        juegoTerminado = false;
        Time.timeScale = 1f;
    }

    void Start()
    {
        ActualizarUI();
    }

    void Update()
    {
        // El proyecto usa el nuevo Input System, por eso se lee desde Keyboard.current.
        bool reiniciar = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        bool irAlMenu = Keyboard.current != null &&
            (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.mKey.wasPressedThisFrame);

        if (juegoTerminado && reiniciar)
        {
            ReiniciarPartida();
        }
        else if (juegoTerminado && irAlMenu && !string.IsNullOrEmpty(nombreEscenaMenu))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(nombreEscenaMenu);
        }
    }

    public void SumarPuntaje(int puntosASumar)
    {
        if (juegoTerminado) return;

        puntaje += puntosASumar;
        ActualizarUI();
    }

    public void ActualizarVidas(int nuevasVidas)
    {
        vidasActuales = nuevasVidas;
        ActualizarUI();
    }

    public void GameOver()
    {
        juegoTerminado = true;

        // Cambia la música por el jingle cómico de "perdiste".
        if (MusicaFondo.Instancia != null) MusicaFondo.Instancia.TocarGameOver();

        // Guarda el puntaje en el ranking de la portada (LEADERBOARDS).
        RankingPuntajes.Guardar(puntaje);

        if (panelGameOver != null)
        {
            panelGameOver.SetActive(true);
        }

        if (fondoGameOver != null)
        {
            Color c = colorFondoGameOver;
            fondoGameOver.color = c;
            fondoGameOver.gameObject.SetActive(true);
        }

        if (textoPuntajeFinal != null)
        {
            textoPuntajeFinal.text = puntaje.ToString("D6");
        }

        if (imagenGameOver != null)
        {
            imagenGameOver.gameObject.SetActive(true);
        }
    }

    public void ReiniciarPartida()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ActualizarUI()
    {
        // 1. Actualizar texto de puntaje
        if (textoPuntaje != null)
        {
            textoPuntaje.text = puntaje.ToString("D6");
        }

        // 2. Actualizar texto de vidas
        if (textoVidas != null)
        {
            textoVidas.text = vidasActuales.ToString();
        }

        // 3. Opcional: Ocultar o mostrar imágenes de corazones retro
        if (imagenesCorazones != null && imagenesCorazones.Length > 0)
        {
            for (int i = 0; i < imagenesCorazones.Length; i++)
            {
                if (imagenesCorazones[i] != null)
                {
                    imagenesCorazones[i].enabled = (i < vidasActuales);
                }
            }
        }
    }
}