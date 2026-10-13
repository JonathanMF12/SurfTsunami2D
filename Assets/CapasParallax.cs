using UnityEngine;

public class CapasParallax : MonoBehaviour
{
    [Header("Configuración de Parallax")]
    [Tooltip("Positivo = Se mueve hacia la izquierda | Negativo = Se mueve hacia la derecha")]
    public float velocidad = 2f;

    [Header("Modo de Bucle")]
    [Tooltip("Si es verdadero, mantiene dos copias empalmadas sin fin. Si es falso, el objeto reaparece por el otro lado de la pantalla al salir (ideal para objetos únicos como el Sol).")]
    public bool bucleContinuo = true;

    [Tooltip("Si está activo y no asignas 'otraCapa', crea automáticamente la segunda copia para empalmar sin cortes.")]
    public bool autoDuplicar = true;

    [Header("Referencia a la Segunda Copia (Opcional)")]
    [Tooltip("Arrastra aquí la copia (ej: islas (1)). Si se deja vacío, el script la detectará o creará automáticamente.")]
    public Transform otraCapa;

    [HideInInspector] public bool esClon = false;

    private float anchoSprite;
    private Camera cam;
    private SpriteRenderer sr;

    void Start()
    {
        cam = Camera.main;
        if (cam == null)
        {
            cam = FindAnyObjectByType<Camera>();
        }

        sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            anchoSprite = sr.bounds.size.x;
        }
        else
        {
            anchoSprite = 19.2f;
        }

        // Si el elemento es pequeño (como el Sol) y no tiene otra copia asignada, tratarlo como objeto único
        if (anchoSprite < 4f && otraCapa == null)
        {
            bucleContinuo = false;
        }

        if (bucleContinuo && !esClon)
        {
            // 1. Si no se asignó otraCapa, buscar si ya existe una copia en la escena (ej: islas y islas (1))
            if (otraCapa == null)
            {
                string baseName = gameObject.name.Replace(" (1)", "").Trim();
                GameObject posibleCopia = null;
                if (gameObject.name.EndsWith(" (1)"))
                {
                    posibleCopia = GameObject.Find(baseName);
                }
                else
                {
                    posibleCopia = GameObject.Find(baseName + " (1)");
                }

                if (posibleCopia != null && posibleCopia != gameObject)
                {
                    otraCapa = posibleCopia.transform;
                }
            }

            // 2. Si encontramos o se asignó otraCapa:
            if (otraCapa != null)
            {
                CapasParallax otraScript = otraCapa.GetComponent<CapasParallax>();
                if (otraScript != null)
                {
                    otraScript.otraCapa = transform;
                    otraScript.velocidad = velocidad; // Sincronizar velocidad
                    otraScript.bucleContinuo = true;
                }

                // Alinear perfectamente para que no queden huecos
                float dir = velocidad >= 0 ? 1f : -1f;
                if (!gameObject.name.EndsWith(" (1)"))
                {
                    otraCapa.position = new Vector3(transform.position.x + dir * anchoSprite, otraCapa.position.y, otraCapa.position.z);
                }
            }
            // 3. Si no hay otra capa y autoDuplicar está activo, generar el clon automáticamente
            else if (autoDuplicar)
            {
                GameObject copia = Instantiate(gameObject, transform.position, transform.rotation, transform.parent);
                copia.name = gameObject.name + "_CopiaLoop";

                CapasParallax copiaScript = copia.GetComponent<CapasParallax>();
                if (copiaScript != null)
                {
                    copiaScript.esClon = true;
                    copiaScript.otraCapa = transform;
                    copiaScript.velocidad = velocidad;
                    copiaScript.bucleContinuo = true;
                }

                float dir = velocidad >= 0 ? 1f : -1f;
                copia.transform.position = new Vector3(transform.position.x + dir * anchoSprite, transform.position.y, transform.position.z);
                otraCapa = copia.transform;
            }
        }
    }

    void Update()
    {
        // Movimiento horizontal en espacio mundial
        transform.position += Vector3.left * (velocidad * Time.deltaTime);

        if (cam == null)
        {
            cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        }

        // Límites horizontales de la cámara
        float camHalfWidth = (cam != null ? cam.orthographicSize * cam.aspect : 8.89f);
        float camCenterX = (cam != null ? cam.transform.position.x : 0f);
        float camLeft = camCenterX - camHalfWidth;
        float camRight = camCenterX + camHalfWidth;

        float anchoActual = (sr != null && sr.sprite != null) ? sr.bounds.size.x : anchoSprite;
        float rightEdge = (sr != null) ? sr.bounds.max.x : (transform.position.x + anchoActual / 2f);
        float leftEdge = (sr != null) ? sr.bounds.min.x : (transform.position.x - anchoActual / 2f);

        if (bucleContinuo && otraCapa != null)
        {
            // Movimiento hacia la izquierda (velocidad > 0): sale por la izquierda
            if (velocidad > 0 && rightEdge < camLeft)
            {
                transform.position = new Vector3(otraCapa.position.x + anchoActual, transform.position.y, transform.position.z);
            }
            // Movimiento hacia la derecha (velocidad < 0): sale por la derecha
            else if (velocidad < 0 && leftEdge > camRight)
            {
                transform.position = new Vector3(otraCapa.position.x - anchoActual, transform.position.y, transform.position.z);
            }
        }
        else
        {
            // Modo objeto único (ej: Sol)
            if (velocidad > 0 && rightEdge < camLeft)
            {
                float offsetPivot = transform.position.x - leftEdge;
                transform.position = new Vector3(camRight + offsetPivot, transform.position.y, transform.position.z);
            }
            else if (velocidad < 0 && leftEdge > camRight)
            {
                float offsetPivot = rightEdge - transform.position.x;
                transform.position = new Vector3(camLeft - offsetPivot, transform.position.y, transform.position.z);
            }
        }
    }
}