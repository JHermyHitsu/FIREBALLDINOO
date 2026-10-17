using UnityEngine;
using System.Collections.Generic;

public class DinoSeguidor : MonoBehaviour
{
    [Header("Ajustes de Seguimiento")]
    public float distanciaDetras = 0.75f;
    public float suavizadoHorizontal = 12f;
    public float offsetVertical = 0f;

    [Header("Estado")]
    public bool reclutado = false;

    [Header("Separación en fila")]
    public int pasosSeparacion = 9;

    private Movimiento lider;
    private Collider2D col;
    private SpriteRenderer sr;
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer srLider;
    private int indiceFila = 0;

    private bool cayendo = false;
    private float velocidadX = 0f;
    private float velocidadY = 0f;

    // ── Historial continuo del líder (compartido y actualizado 1 sola vez por frame) ──
    private struct MuestraLider
    {
        public Vector3 posicion;
        public bool enSuelo;
        public float tiempo;
    }

    private static readonly List<MuestraLider> historial = new List<MuestraLider>();
    private static Transform ultimoLiderRegistrado;
    private static int ultimoFrameRegistrado = -1;
    private const float DuracionMaxHistorial = 4f;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // Corrige escala Z en 0 si algún prefab o instancia la tenía mal configurada
        if (Mathf.Approximately(transform.localScale.z, 0f))
        {
            Vector3 s = transform.localScale;
            s.z = 1f;
            transform.localScale = s;
        }
    }

    void Start()
    {
        if (animator != null)
        {
            animator.SetBool("EnSuelo", true);
            animator.SetFloat("Velocidad", 0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (reclutado || cayendo) return;
        if (otro.CompareTag("Player"))
        {
            Movimiento jugador = otro.GetComponent<Movimiento>();
            if (jugador != null && jugador.AgregarSeguidor(this))
                Reclutar(jugador);
        }
    }

    public void Reclutar(Movimiento jugador)
    {
        if (cayendo || jugador == null) return;

        reclutado = true;
        lider = jugador;
        srLider = lider.GetComponent<SpriteRenderer>();
        velocidadX = 0f;
        velocidadY = 0f;

        if (col != null) col.enabled = false;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        if (historial.Count == 0 || ultimoLiderRegistrado != lider.transform)
        {
            ultimoLiderRegistrado = lider.transform;
            InicializarHistorial(lider.transform.position, lider.EstaEnSuelo());
        }
    }

    /// <summary>
    /// Re-vincula este seguidor después de una transición de escena.
    /// </summary>
    public void ReVincular(Movimiento jugador)
    {
        if (cayendo || jugador == null) return;

        reclutado = true;
        lider = jugador;
        srLider = lider.GetComponent<SpriteRenderer>();
        velocidadX = 0f;
        velocidadY = 0f;

        if (col != null) col.enabled = false;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void AsignarIndice(int indice) => indiceFila = indice;

    void Update()
    {
        if (cayendo)
        {
            if (transform.position.y < -15f)
                Destroy(gameObject);
            return;
        }

        if (!reclutado || lider == null) return;

        RegistrarMuestraLider();
    }

    void LateUpdate()
    {
        if (cayendo || !reclutado || lider == null) return;
        if (historial.Count == 0)
        {
            RegistrarMuestraLider();
            if (historial.Count == 0) return;
        }

        // 1. Obtener posición e información de salto con retraso interpolado por seguidor
        float retrasoPorSeguidor = Mathf.Max(0.08f, pasosSeparacion * 0.018f) * (indiceFila + 1);
        MuestraLider muestra = ObtenerMuestraInterpolada(Time.time - retrasoPorSeguidor);

        Vector3 posObjetivo = muestra.posicion;
        float xLider = lider.transform.position.x;
        float sepDeseada = Mathf.Max(0.4f, distanciaDetras) * (indiceFila + 1);

        // 2. Determinar el lado natural en el que se encuentra el seguidor respecto al líder.
        //    No se fuerza un cambio de lado solo por girar (flipX), evitando tirones y caídas en esquinas.
        float diffX = transform.position.x - xLider;
        float ladoActual;
        if (Mathf.Abs(diffX) > 0.05f)
            ladoActual = Mathf.Sign(diffX);
        else
            ladoActual = (srLider != null && srLider.flipX) ? 1f : -1f;

        // 3. Mantener distancia cómoda detrás del líder sin encimarse cuando el líder se detiene
        if (Mathf.Abs(posObjetivo.x - xLider) < sepDeseada)
        {
            posObjetivo.x = xLider + ladoActual * sepDeseada;
        }

        // 4. Protección inteligente en esquinas/bordes de plataformas:
        //    Si el líder está en el suelo pero la posición de descanso del seguidor queda
        //    asomada al vacío en una esquina, acercamos suavemente al seguidor hacia el líder
        //    para que se mantenga firme sobre la plataforma (nunca muere en esquinas).
        if (lider.EstaEnSuelo() && muestra.enSuelo)
        {
            posObjetivo.y = lider.transform.position.y;
            if (!HaySueloDebajoDe(new Vector2(posObjetivo.x, posObjetivo.y)))
            {
                float sepSegura = Mathf.Min(sepDeseada, 0.22f * (indiceFila + 1));
                posObjetivo.x = xLider + ladoActual * sepSegura;
            }
        }

        // 5. Movimiento orgánico con SmoothDamp (aceleración y frenado suaves, sin vibración)
        float tiempoSuavizadoX = Mathf.Clamp(1.4f / Mathf.Max(1f, suavizadoHorizontal), 0.05f, 0.25f);
        float tiempoSuavizadoY = muestra.enSuelo ? 0.06f : 0.035f;

        float newX = Mathf.SmoothDamp(transform.position.x, posObjetivo.x, ref velocidadX, tiempoSuavizadoX);
        float newY = Mathf.SmoothDamp(transform.position.y, posObjetivo.y + offsetVertical, ref velocidadY, tiempoSuavizadoY);

        transform.position = new Vector3(newX, newY, transform.position.z);

        // 6. Orientación natural: mira hacia donde camina; al detenerse, mira como el líder
        if (sr != null)
        {
            if (Mathf.Abs(velocidadX) > 0.25f)
                sr.flipX = velocidadX < 0f;
            else if (srLider != null)
                sr.flipX = srLider.flipX;
        }

        // 7. Animación sincronizada con la velocidad y salto reales del seguidor
        if (animator != null)
        {
            bool enMovimiento = Mathf.Abs(velocidadX) > 0.2f;
            animator.SetFloat("Velocidad", enMovimiento ? 1f : 0f);
            animator.SetBool("EnSuelo", muestra.enSuelo);
        }
    }

    // ────────────────────────────────────────────────────────────
    //  Detección auxiliar de borde (solo para acomodar en esquinas)
    // ────────────────────────────────────────────────────────────

    private bool HaySueloDebajoDe(Vector2 punto)
    {
        Vector2 origen = punto + Vector2.up * 0.25f;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origen, Vector2.down, 1.2f);
        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (hit.collider.gameObject == gameObject) continue;
            if (hit.collider.CompareTag("Player")) continue;
            if (hit.collider.GetComponent<DinoSeguidor>() != null) continue;
            return true;
        }
        return false;
    }

    // ────────────────────────────────────────────────────────────
    //  Sacrificio (cuando el jugador recibe daño o cae al vacío)
    // ────────────────────────────────────────────────────────────

    public void Sacrificar()
    {
        if (cayendo) return;
        cayendo = true;
        reclutado = false;

        if (col != null) col.enabled = false;

        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 2f;
        rb.linearVelocity = new Vector2(0f, 10f);

        if (animator != null)
        {
            animator.SetBool("EnSuelo", false);
            animator.SetFloat("Velocidad", 0f);
            animator.SetBool("Muerto", true);
            animator.SetTrigger("Morir");
        }

        Destroy(gameObject, 1.5f);
    }

    // Mantenido por compatibilidad si algún script externo lo invoca
    public void CaerYMorir()
    {
        if (!reclutado) return;
        if (lider != null)
        {
            lider.RemoverSeguidor(this);
            lider = null;
        }
        Sacrificar();
    }

    // ────────────────────────────────────────────────────────────
    //  Gestión de historial interpolado
    // ────────────────────────────────────────────────────────────

    private void RegistrarMuestraLider()
    {
        if (ultimoLiderRegistrado != lider.transform)
        {
            ultimoLiderRegistrado = lider.transform;
            InicializarHistorial(lider.transform.position, lider.EstaEnSuelo());
            return;
        }

        if (ultimoFrameRegistrado == Time.frameCount) return;
        ultimoFrameRegistrado = Time.frameCount;

        float ahora = Time.time;
        historial.Add(new MuestraLider
        {
            posicion = lider.transform.position,
            enSuelo = lider.EstaEnSuelo(),
            tiempo = ahora
        });

        // Eliminar muestras antiguas fuera de la ventana de tiempo
        float tiempoMinimo = ahora - DuracionMaxHistorial;
        while (historial.Count > 2 && historial[1].tiempo < tiempoMinimo)
        {
            historial.RemoveAt(0);
        }
    }

    private static MuestraLider ObtenerMuestraInterpolada(float tiempoObjetivo)
    {
        int count = historial.Count;
        if (count == 1 || tiempoObjetivo <= historial[0].tiempo)
            return historial[0];

        if (tiempoObjetivo >= historial[count - 1].tiempo)
            return historial[count - 1];

        // Búsqueda desde el final (las consultas suelen estar cerca del tiempo actual)
        for (int i = count - 2; i >= 0; i--)
        {
            MuestraLider a = historial[i];
            MuestraLider b = historial[i + 1];
            if (tiempoObjetivo >= a.tiempo && tiempoObjetivo <= b.tiempo)
            {
                float dt = b.tiempo - a.tiempo;
                float t = dt > 0.0001f ? (tiempoObjetivo - a.tiempo) / dt : 1f;
                return new MuestraLider
                {
                    posicion = Vector3.Lerp(a.posicion, b.posicion, t),
                    enSuelo = t < 0.5f ? a.enSuelo : b.enSuelo,
                    tiempo = tiempoObjetivo
                };
            }
        }

        return historial[count - 1];
    }

    private static void InicializarHistorial(Vector3 pos, bool enSuelo)
    {
        historial.Clear();
        historial.Add(new MuestraLider
        {
            posicion = pos,
            enSuelo = enSuelo,
            tiempo = Time.time
        });
        ultimoFrameRegistrado = Time.frameCount;
    }

    public static void LimpiarHistorial()
    {
        historial.Clear();
        ultimoLiderRegistrado = null;
        ultimoFrameRegistrado = -1;
    }
}