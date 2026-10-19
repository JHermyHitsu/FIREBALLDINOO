using UnityEngine;

// Este script se coloca en los precipicios (Killzone) con un BoxCollider2D en modo "Is Trigger"
public class ZonaMuerte : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D otro)
    {
        DetectarCaida(otro);
    }

    private void OnTriggerStay2D(Collider2D otro)
    {
        DetectarCaida(otro);
    }

    private void DetectarCaida(Collider2D otro)
    {
        // Si el objeto que cayó en el precipicio es el jugador
        // (Si tiene seguidores, Movimiento.Morir sacrifica uno automáticamente para impulsarlo)
        if (otro.CompareTag("Player"))
        {
            Movimiento dino = otro.GetComponent<Movimiento>();
            if (dino != null)
                dino.Morir(esPrecipicio: true);
        }
    }
}