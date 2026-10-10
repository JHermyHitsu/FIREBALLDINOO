using UnityEngine;
using UnityEngine.Tilemaps;

public class CarneTilemap : MonoBehaviour
{
    [Header("Efecto del boost")]
    public float multiplicador = 1.5f;
    public float duracion = 4f;
    [Header("Sonido")]
    public AudioClip sonidoComer;

    private Tilemap tilemap;
    private AudioSource sfx;

    void Start()
    {
        tilemap = GetComponent<Tilemap>();
        sfx = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        IntentarComer(otro);
    }

    private void OnTriggerStay2D(Collider2D otro)
    {
        IntentarComer(otro);
    }

    private void IntentarComer(Collider2D otro)
    {
        if (!otro.CompareTag("Player")) return;
        Movimiento dino = otro.GetComponent<Movimiento>();
        if (dino == null) return;

        // 1. Escanear todo el volumen del collider del Dino (con 1 celda de margen)
        Bounds bounds = otro.bounds;
        Vector3Int minCelda = tilemap.WorldToCell(bounds.min) - new Vector3Int(1, 1, 0);
        Vector3Int maxCelda = tilemap.WorldToCell(bounds.max) + new Vector3Int(1, 1, 0);

        for (int x = minCelda.x; x <= maxCelda.x; x++)
        {
            for (int y = minCelda.y; y <= maxCelda.y; y++)
            {
                Vector3Int celda = new Vector3Int(x, y, 0);
                if (tilemap.HasTile(celda))
                {
                    ConsumirMuslo(celda, dino);
                    return;
                }
            }
        }

        // 2. Respaldo por distancia física al centro del tile
        Vector3 centroDino = bounds.center;
        float radioMaximo = Mathf.Max(bounds.extents.x, bounds.extents.y) + 1.2f;

        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos))
            {
                Vector3 posMundoTile = tilemap.GetCellCenterWorld(pos);
                if (Vector2.Distance(centroDino, posMundoTile) <= radioMaximo)
                {
                    ConsumirMuslo(pos, dino);
                    return;
                }
            }
        }
    }

    private void ConsumirMuslo(Vector3Int celda, Movimiento dino)
    {
        Debug.Log($"[Carne] ¡Muslo comido en {celda}! Aplicando boost.");
        tilemap.SetTile(celda, null);
        dino.ActivarBoost(multiplicador, duracion);

        if (sfx != null && sonidoComer != null)
            sfx.PlayOneShot(sonidoComer);
    }
}
