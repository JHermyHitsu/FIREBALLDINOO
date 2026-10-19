using UnityEngine;

// Meta final del juego (colocar al final de la Cueva).
// Mismo patrón que SalidaNivel/ZonaMuerte: BoxCollider2D en IsTrigger + tag Player.
// Al entrar muestra el mensaje de victoria con estadísticas.
[RequireComponent(typeof(Collider2D))]
public class MetaFinal : MonoBehaviour
{
    bool yaGanado = false;

    void OnValidate()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (yaGanado || !otro.CompareTag("Player")) return;
        yaGanado = true;
        Temporizador.EnsureExists().MostrarVictoria();
    }

    // Cajita visible solo en el editor para colocarla bien al final del nivel.
    void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.4f);
        if (col is BoxCollider2D box)
            Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
        else
            Gizmos.DrawSphere(transform.position, 1f);
    }
}
