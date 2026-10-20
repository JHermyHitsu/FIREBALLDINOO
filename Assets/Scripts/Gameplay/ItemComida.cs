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
        // Si ya fue recogido o no es el jugador con tag "Player", ignoramos
        if (yaFueComido || !otro.CompareTag("Player")) return;

        Movimiento dino = otro.GetComponent<Movimiento>();
        if (dino != null)
        {
            yaFueComido = true;

            // 1. Activar el aumento de velocidad temporal en el jugador
            dino.ActivarBoost(multiplicador, duracion);

            // 2. Reproducir el efecto de sonido
            if (sonidoComer != null)
            {
                Vector3 posAudio = Camera.main != null ? Camera.main.transform.position : transform.position;
                AudioSource.PlayClipAtPoint(sonidoComer, posAudio);
            }

            // 3. Eliminar este muslo de la escena
            Destroy(gameObject);
        }
    }
}
