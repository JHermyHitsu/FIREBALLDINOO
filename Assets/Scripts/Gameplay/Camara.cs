using UnityEngine;

public class Camara : MonoBehaviour
{
    [Header("Objetivo a seguir")]
    public Transform personaje;

    [Header("Configuración de seguimiento")]
    public float smoothSpeed = 5f;  // Suavidad del movimiento
    public float adelantoX = 2f;    // Distancia que la cámara mira hacia adelante
    public float yFija = 2.68f;     // Altura fija de la cámara para no marear al saltar

    // Temblor de cámara
    private bool temblando = false;
    private float intensidadTemblor = 0f;

    public void ActivarTemblor(bool activo, float intensidad)
    {
        temblando = activo;
        intensidadTemblor = activo ? intensidad : 0f;
    }

    // LateUpdate se ejecuta después de que el personaje se mueve, evitando tirones visuales
    void LateUpdate()
    {
        if (personaje == null) return;

        // Calculamos la posición horizontal deseada (adelantada un poco para ver obstáculos)
        float targetX = personaje.position.x + adelantoX;

        // Mantenemos la altura Y fija y Z en -10 (estándar para cámaras 2D)
        Vector3 posicionDeseada = new Vector3(targetX, yFija, -10f);

        // Temblor de cámara (cuando el tiempo está bajo)
        Vector3 shake = Vector3.zero;
        if (temblando)
        {
            shake = new Vector3(
                Random.Range(-intensidadTemblor, intensidadTemblor),
                Random.Range(-intensidadTemblor * 0.8f, intensidadTemblor * 0.8f),
                0f);
        }

        // Interpolamos suavemente hacia la posición deseada (+ temblor)
        transform.position = Vector3.Lerp(transform.position, posicionDeseada + shake, smoothSpeed * Time.deltaTime);
    }
}