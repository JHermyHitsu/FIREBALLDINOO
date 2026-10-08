using UnityEngine;

public class ZonaMuerte : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Player"))
        {
            Movimiento dino = otro.GetComponent<Movimiento>();
            if (dino != null)
                dino.Morir();
        }
    }
}