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

        // Solo avanza, nunca retrocede como Mario
        float targetX = Mathf.Max(transform.position.x, personaje.position.x + adelantoX);

        Vector3 deseada = new Vector3(targetX, yFija, -10f);
        transform.position = Vector3.Lerp(transform.position, deseada, smoothSpeed * Time.deltaTime);
    }
}