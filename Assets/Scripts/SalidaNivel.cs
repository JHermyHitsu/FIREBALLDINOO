using UnityEngine;

// Trigger invisible al borde del bioma. Al entrar caminando, pide el cambio de escena.
// Mismo patrón que ZonaMuerte: BoxCollider2D en IsTrigger + tag Player.
[RequireComponent(typeof(Collider2D))]
public class SalidaNivel : MonoBehaviour
{
    [Header("Destino")]
    [Tooltip("Nombre exacto de la escena (sin .unity). Debe estar en Build Profiles.")]
    public string escenaDestino;
    [Tooltip("Nombre exacto del Empty de spawn en la escena destino.")]
    public string spawnDestino;

    void OnValidate()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!otro.CompareTag("Player")) return;
        if (TransicionManager.Instance == null)
        {
            Debug.LogError("[SalidaNivel] no hay TransicionManager en escena.");
            return;
        }
        TransicionManager.Instance.IrA(escenaDestino, spawnDestino);
    }

    // Cajita visible solo en el editor para colocarla bien al borde.
    void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        if (col is BoxCollider2D box)
            Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
        else
            Gizmos.DrawSphere(transform.position, 1f);
    }
}
