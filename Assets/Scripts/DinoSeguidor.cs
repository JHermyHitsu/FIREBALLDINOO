using UnityEngine;
using System.Collections.Generic;

public class DinoSeguidor : MonoBehaviour
{
    [Header("Ajustes")]
    public float distanciaDetras = 0.7f;
    public float suavizadoHorizontal = 14f;
    public float offsetVertical = 0f;

    [Header("Estado")]
    public bool reclutado = false;

    private Movimiento lider;
    private Collider2D col;
    private SpriteRenderer sr;
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer srLider;
    private int indiceFila = 0;

    // ── Historial de posiciones del líder (compartido entre todos los seguidores) ──
    // Funciona como una "estela": cada seguidor lee la posición que el líder
    // tenía N pasos atrás, produciendo una fila tipo serpiente.
    private static List<Vector3> historialPosiciones = new List<Vector3>();
    private static float intervaloRegistro = 0.05f;
    private static float timerRegistro = 0f;
    private static int maxHistorial = 300;
    private static Transform ultimoLiderRegistrado;

    // Cuántos registros de separación hay entre cada seguidor en la fila
    [Header("Separación en fila")]
    public int pasosSeparacion = 3;

    [Header("Caída al vacío")]
    public float distanciaRayoSuelo = 1.0f;
    public float tiempoLimiteSinSuelo = 0.25f;

    private bool cayendo = false;
    private float tiempoSinSuelo = 0f;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
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
        if (cayendo) return;
        reclutado = true;
        lider = jugador;
        srLider = lider.GetComponent<SpriteRenderer>();
        tiempoSinSuelo = 0f;

        if (col != null) col.enabled = false;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        // Si el historial está vacío, llenarlo con la posición actual del líder
        // para que el seguidor no salte al origen
        if (historialPosiciones.Count == 0)
            RellenarHistorial(lider.transform.position);
    }

    /// <summary>
    /// Re-vincula este seguidor a un jugador después de una transición de escena.
    /// </summary>
    public void ReVincular(Movimiento jugador)
    {
        if (cayendo) return;
        lider = jugador;
        srLider = lider.GetComponent<SpriteRenderer>();
        tiempoSinSuelo = 0f;
    }

    public void AsignarIndice(int indice) => indiceFila = indice;

    void Update()
    {
        if (cayendo)
        {
            if (transform.position.y < -12f)
                Destroy(gameObject);
            return;
        }

        if (!reclutado || lider == null) return;

        // ── Comprobar si está en el aire mientras el líder está en suelo firme ──
        if (lider.EstaEnSuelo())
        {
            if (!TieneSueloDebajo())
            {
                tiempoSinSuelo += Time.deltaTime;
                if (tiempoSinSuelo >= tiempoLimiteSinSuelo)
                {
                    CaerYMorir();
                    return;
                }
            }
            else
            {
                tiempoSinSuelo = 0f;
            }
        }
        else
        {
            // Mientras el líder salta, no cuenta como caída
            tiempoSinSuelo = 0f;
        }

        // Red de seguridad si cae por debajo del nivel
        if (transform.position.y < -12f)
        {
            CaerYMorir();
            return;
        }

        // Si el líder cambió (ej: transición de escena), reiniciar el historial
        if (ultimoLiderRegistrado != lider.transform)
        {
            ultimoLiderRegistrado = lider.transform;
            RellenarHistorial(lider.transform.position);
        }

        // Registrar la posición del líder a intervalos regulares
        timerRegistro -= Time.deltaTime;
        if (timerRegistro <= 0f)
        {
            timerRegistro = intervaloRegistro;
            historialPosiciones.Add(lider.transform.position);
            if (historialPosiciones.Count > maxHistorial)
                historialPosiciones.RemoveAt(0);
        }
    }

    void LateUpdate()
    {
        if (cayendo || !reclutado || lider == null) return;
        if (historialPosiciones.Count == 0) return;

        // ── Leer la posición pasada correspondiente a este seguidor ──
        int indiceHistorial = historialPosiciones.Count - 1 - (pasosSeparacion * (indiceFila + 1));
        indiceHistorial = Mathf.Clamp(indiceHistorial, 0, historialPosiciones.Count - 1);

        Vector3 posObjetivo = historialPosiciones[indiceHistorial];

        // ── Separación compacta estilo horda (Zombie Tsunami) ──
        float distMin = distanciaDetras * (indiceFila + 1);
        bool miraIzq = srLider.flipX;
        float dirDetras = miraIzq ? 1f : -1f;
        float xMinima = lider.transform.position.x + (dirDetras * distMin);

        // Asegurar que el seguidor se mantenga pegado detrás del líder sin adelantarse ni alejarse de más
        if (!miraIzq)
        {
            if (posObjetivo.x > xMinima) posObjetivo.x = xMinima;
        }
        else
        {
            if (posObjetivo.x < xMinima) posObjetivo.x = xMinima;
        }

        // ── Aplicar posición suavizada ──
        float nuevaX = Mathf.Lerp(transform.position.x, posObjetivo.x, suavizadoHorizontal * Time.deltaTime);
        float nuevaY = posObjetivo.y + offsetVertical;
        transform.position = new Vector3(nuevaX, nuevaY, transform.position.z);

        // ── Orientar el sprite ──
        if (sr != null) sr.flipX = srLider.flipX;

        // ── Animator ──
        if (animator != null)
        {
            float vel = Mathf.Abs(posObjetivo.x - transform.position.x);
            animator.SetFloat("Velocidad", vel > 0.05f ? 1f : 0f);
            animator.SetBool("EnSuelo", lider.EstaEnSuelo());
        }
    }

    /// <summary>
    /// Comprueba si hay una plataforma sólida debajo de los pies del dino.
    /// </summary>
    public bool TieneSueloDebajo()
    {
        Vector2 origenCentro = (Vector2)transform.position + Vector2.up * 0.3f;

        // Revisar el centro y un punto ligeramente hacia adelante
        float dirFrente = (sr != null && sr.flipX) ? -0.25f : 0.25f;
        Vector2 origenFrente = origenCentro + new Vector2(dirFrente, 0f);

        return HaySueloEn(origenCentro) || HaySueloEn(origenFrente);
    }

    private bool HaySueloEn(Vector2 origen)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origen, Vector2.down, distanciaRayoSuelo);
        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (hit.collider.gameObject == gameObject) continue;
            if (hit.collider.CompareTag("Player")) continue;

            // Cualquier collider sólido con tag "SUELO" o capa de suelo
            if (hit.collider.CompareTag("SUELO") || hit.collider.gameObject.layer == LayerMask.NameToLayer("SUELO"))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Llamado cuando el dino no tiene plataforma debajo y debe caer al precipicio y morir.
    /// </summary>
    public void CaerYMorir()
    {
        if (cayendo) return;
        cayendo = true;
        reclutado = false;

        // 1. Desvincular del líder y sacarlo de la lista de vidas extra
        if (lider != null)
        {
            if (lider.sonidoMuerte != null)
            {
                Vector3 posAudio = Camera.main != null ? Camera.main.transform.position : transform.position;
                AudioSource.PlayClipAtPoint(lider.sonidoMuerte, posAudio);
            }
            lider.RemoverSeguidor(this);
            lider = null;
        }

        // 2. Desactivar collider para que caiga libremente por el precipicio
        if (col != null)
            col.enabled = false;

        // 3. Activar física dinámica y gravedad para la caída
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.linearVelocity = new Vector2(0f, -2.5f);

        // 4. Animación de muerte/caída
        if (animator != null)
        {
            animator.SetBool("EnSuelo", false);
            animator.SetFloat("Velocidad", 0f);
            animator.SetBool("Muerto", true);
            animator.SetTrigger("Morir");
        }

        // 5. Destruir tras caer
        Destroy(gameObject, 1.5f);
    }

    // ── Utilidades estáticas ──

    /// <summary>
    /// Rellena el historial con una posición fija (evita saltos al inicio o tras transición).
    /// </summary>
    private static void RellenarHistorial(Vector3 pos)
    {
        historialPosiciones.Clear();
        for (int i = 0; i < maxHistorial; i++)
            historialPosiciones.Add(pos);
        timerRegistro = 0f;
    }

    /// <summary>
    /// Limpia el historial estático. Llamar al reiniciar el juego por completo.
    /// </summary>
    public static void LimpiarHistorial()
    {
        historialPosiciones.Clear();
        timerRegistro = 0f;
        ultimoLiderRegistrado = null;
    }

    // Llamado cuando el jugador recibe daño y este dino se sacrifica para salvarlo
    public void Sacrificar()
    {
        if (cayendo) return;
        cayendo = true;
        reclutado = false;

        // 1. Desactivar collider para que atraviese el suelo al caer (igual que el verde)
        if (col != null)
            col.enabled = false;

        // 2. Darle gravedad y salto de muerte (idéntico al dino verde)
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 2f;
        rb.linearVelocity = new Vector2(0f, 10f);

        // 3. Activar animación de muerte en el Animator
        if (animator != null)
        {
            animator.SetBool("Muerto", true);
            animator.SetTrigger("Morir");
        }

        // 4. Destruir tras caer fuera de la pantalla
        Destroy(gameObject, 1.5f);
    }
}