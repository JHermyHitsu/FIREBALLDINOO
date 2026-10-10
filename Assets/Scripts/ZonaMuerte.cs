using UnityEngine;

// Este script se coloca en los precipicios (Killzone) con un BoxCollider2D en modo "Is Trigger"
public class ZonaMuerte : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D otro)
    {
        // Si el objeto que cayó en el precipicio es el jugador
        if (otro.CompareTag("Player"))
        {
            Movimiento dino = otro.GetComponent<Movimiento>();
            if (dino != null)
                dino.Morir();
        }
    }
}