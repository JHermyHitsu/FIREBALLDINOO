using UnityEngine;

public class Camara : MonoBehaviour
{
    [Header("Objetivo a seguir")]
    public Transform personaje;

    [Header("Configuración de seguimiento")]
    public float smoothSpeed = 5f;  // Suavidad del movimiento
    public float adelantoX = 2f;    // Distancia que la cámara mira hacia adelante
    public float yFija = 2.68f;     // Altura fija de la cámara para no marear al saltar

    [Header("Límites del nivel (anti-vacío azul)")]
    [Tooltip("Tag del suelo del que se sacan los bordes. Si no se encuentra, la cámara no se limita.")]
    public string tagSuelo = "SUELO";

    Camera camara;
    Collider2D bordesSuelo;

    void Awake()
    {
        camara = GetComponent<Camera>();
        BuscarSuelo();
    }

    // Los bordes salen del collider del suelo: funciona en cada bioma sin configurar nada.
    void BuscarSuelo()
    {
        GameObject suelo = GameObject.FindGameObjectWithTag(tagSuelo);
        if (suelo != null)
            bordesSuelo = suelo.GetComponent<Collider2D>();
    }

    // LateUpdate se ejecuta después de que el personaje se mueve, evitando tirones visuales
    void LateUpdate()
    {
        if (personaje == null) return;
        if (bordesSuelo == null) BuscarSuelo();

        // Calculamos la posición horizontal deseada (adelantada un poco para ver obstáculos)
        float targetX = personaje.position.x + adelantoX;

        // Limitamos para no mostrar el vacío fuera del nivel.
        if (bordesSuelo != null && camara != null)
        {
            float mitadAncho = camara.orthographicSize * camara.aspect;
            float minX = bordesSuelo.bounds.min.x + mitadAncho;
            float maxX = bordesSuelo.bounds.max.x - mitadAncho;
            if (minX <= maxX)
                targetX = Mathf.Clamp(targetX, minX, maxX);
            else
                targetX = (bordesSuelo.bounds.min.x + bordesSuelo.bounds.max.x) * 0.5f;
        }

        // Mantenemos la altura Y fija y Z en -10 (estándar para cámaras 2D)
        Vector3 posicionDeseada = new Vector3(targetX, yFija, -10f);

        // Interpolamos suavemente hacia la posición deseada
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, smoothSpeed * Time.deltaTime);
    }
}