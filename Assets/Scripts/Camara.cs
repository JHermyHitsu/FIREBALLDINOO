using UnityEngine;

public class Camara : MonoBehaviour
{
    [Header("Objetivo a seguir")]
    public Transform personaje;

    [Header("Configuración de seguimiento")]
    public float smoothSpeed = 5f;  // Suavidad del movimiento
    public float adelantoX = 2f;    // Distancia que la cámara mira hacia adelante
    public float yFija = 2.68f;     // Altura fija de la cámara para no marear al saltar

    // LateUpdate se ejecuta después de que el personaje se mueve, evitando tirones visuales
    void LateUpdate()
    {
        if (personaje == null) return;

        // Calculamos la posición horizontal deseada (adelantada un poco para ver obstáculos)
        float targetX = personaje.position.x + adelantoX;

        // Mantenemos la altura Y fija y Z en -10 (estándar para cámaras 2D)
        Vector3 posicionDeseada = new Vector3(targetX, yFija, -10f);

        // Interpolamos suavemente hacia la posición deseada
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, smoothSpeed * Time.deltaTime);
    }
}