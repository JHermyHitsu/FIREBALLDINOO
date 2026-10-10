using UnityEngine;

public class ItemComida : MonoBehaviour
{
    [Header("Efecto de velocidad")]
    public float multiplicador = 1.5f;
    public float duracion = 4f;

    [Header("Sonido")]
    public AudioClip sonidoComer;

    private bool yaFueComido = false;

    private void OnTriggerEnter2D(Collider2D otro)
    {
        // Si ya se comió en este frame o no es el jugador, no hacer nada
        if (yaFueComido || !otro.CompareTag("Player")) return;

        Movimiento dino = otro.GetComponent<Movimiento>();
        if (dino != null)
        {
            yaFueComido = true;

            // 1. Activar el boost en el Dino
            dino.ActivarBoost(multiplicador, duracion);

            // 2. Reproducir el sonido en la posición del muslo
            if (sonidoComer != null)
            {
                AudioSource.PlayClipAtPoint(sonidoComer, Camera.main != null ? Camera.
main.transform.position : transform.position);
            }

            // 3. Destruir el muslo de la escena
            Destroy(gameObject);
        }
    }
}