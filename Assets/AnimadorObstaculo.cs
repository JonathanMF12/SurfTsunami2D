using UnityEngine;

public class AnimadorObstaculo : MonoBehaviour
{
    public float amplitudRotacion = 8f;
    public float frecuenciaRotacion = 1.5f;
    public float amplitudEscalaY = 0.05f;
    public float frecuenciaEscala = 2f;
    public bool oscilarX = false;
    public float amplitudX = 0.1f;
    public float frecuenciaX = 1f;

    private Vector3 escalaBase;
    private Vector3 posBase;
    private float offset;

    void Awake()
    {
        posBase = transform.position;
        offset = Random.Range(0f, 10f);
    }

    void Start()
    {
        // Se captura aquí (y no en Awake) para respetar la escala que fije un
        // animador por fotogramas, como AnimadorPulpo, que la ajusta en su Awake.
        escalaBase = transform.localScale;

        string nombre = gameObject.name.ToLower();
        if (nombre.Contains("tiburon"))
        {
            amplitudRotacion = 4f;
            frecuenciaRotacion = 1.2f;
            amplitudEscalaY = 0.03f;
            frecuenciaEscala = 1.8f;
        }
        else if (nombre.Contains("delfin"))
        {
            amplitudRotacion = 6f;
            frecuenciaRotacion = 2.5f;
            amplitudEscalaY = 0.08f;
            frecuenciaEscala = 3f;
            oscilarX = true;
            amplitudX = 0.05f;
            frecuenciaX = 2.2f;
        }
        else if (nombre.Contains("pulpo"))
        {
            amplitudRotacion = 5f;
            frecuenciaRotacion = 1.8f;
            amplitudEscalaY = 0.04f;
            frecuenciaEscala = 2.2f;
            oscilarX = true;
            amplitudX = 0.03f;
            frecuenciaX = 1.5f;
        }
        else if (nombre.Contains("bañista") || nombre.Contains("banista"))
        {
            amplitudRotacion = 2f;
            frecuenciaRotacion = 0.9f;
            amplitudEscalaY = 0.02f;
            frecuenciaEscala = 1.2f;
        }
    }

    void Update()
    {
        float t = Time.time + offset;
        float rot = Mathf.Sin(t * frecuenciaRotacion) * amplitudRotacion;
        transform.rotation = Quaternion.Euler(0f, 0f, rot);

        float scaleY = escalaBase.y * (1f + Mathf.Sin(t * frecuenciaEscala) * amplitudEscalaY);
        transform.localScale = new Vector3(escalaBase.x, scaleY, escalaBase.z);

        if (oscilarX)
        {
            Vector3 p = posBase;
            // Oscilación suave relativa (se puede combinar con movimiento de Obstaculo)
            // pero mejor usar offset local
        }
    }
}