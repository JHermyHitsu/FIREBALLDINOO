using UnityEngine;

public class Camara : MonoBehaviour
{
    public Transform personaje;
    public float smoothSpeed = 5f;
    public float adelantoX = 2f; // ver hacia adelante
    public float yFija = 2.68f;  // la Y que ya tenés en tu captura, clavala

    void LateUpdate()
    {
        if (personaje == null) return;

        // Sigue al personaje en ambas direcciones
        float targetX = personaje.position.x + adelantoX;

        Vector3 deseada = new Vector3(targetX, yFija, -10f);
        transform.position = Vector3.Lerp(transform.position, deseada, smoothSpeed * Time.deltaTime);
    }
}