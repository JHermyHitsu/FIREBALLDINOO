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
    private static List<Vector3> historialPosiciones = new List<Vector3>();
    private static float intervaloRegistro = 0.05f;
    private static float timerRegistro = 0f;
    private static int maxHistorial = 300;
    private static Transform ultimoLiderRegistrado;

    [Header("Separación en fila")]
    public int pasosSeparacion = 9;

    [Header("Caída al vacío")]
    public float distanciaRayoSuelo = 1.2f;
    public float tiempoLimiteSinSuelo = 1.5f;  // Coyote en bordes — más tolerante en esquinas

    private bool cayendo = false;
    private float tiempoSinSuelo = 0f;

    // ── Gracia de reclutamiento: durante este tiempo NO se comprueba suelo
    //    (evita falsos positivos al recrutar un dino que aún está en el aire
    //    en su posición inicial antes de que el historial lo lleve al suelo)
    private float tiempoGraciaReclutamiento = 0.8f;
    private float gracia = 0f;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        sr  = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rb  = GetComponent<Rigidbody2D>();
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
        reclutado     = true;
        lider         = jugador;
        srLider       = lider.GetComponent<SpriteRenderer>();
        tiempoSinSuelo = 0f;
        gracia         = tiempoGraciaReclutamiento;   // ← gracia activa

        if (col != null) col.enabled = false;
        if (rb  != null)
        {
            rb.bodyType      = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        // Rellenar siempre al reclutar para que el nuevo dino
        // arranque pegado al líder y no salte al origen.
        RellenarHistorial(lider.transform.position);
    }

    /// <summary>
    /// Re-vincula este seguidor después de una transición de escena.
    /// </summary>
    public void ReVincular(Movimiento jugador)
    {
        if (cayendo) return;
        lider          = jugador;
        srLider        = lider.GetComponent<SpriteRenderer>();
        tiempoSinSuelo = 0f;
        gracia         = tiempoGraciaReclutamiento;
    }

    public void AsignarIndice(int indice) => indiceFila = indice;

    void Update()
    {
        // ── Modo caída libre ──
        if (cayendo)
        {
            if (transform.position.y < -12f)
                Destroy(gameObject);
            return;
        }

        if (!reclutado || lider == null) return;

        // ── Contador de gracia ──
        if (gracia > 0f)
        {
            gracia -= Time.deltaTime;
            tiempoSinSuelo = 0f;   // No acumular mientras hay gracia
        }
        else
        {
            // ── Comprobar plataforma solo cuando el líder está en suelo
            //    y el período de gracia ya terminó ──
            if (lider.EstaEnSuelo())
            {
                // Solo activar detección si el dino ya está lejos del líder.
                // Cuando el historial lo arrastra por una esquina, la distancia
                // al líder es corta → no es un precipicio real, es un giro.
                float distAlLider = Vector2.Distance(transform.position, lider.transform.position);
                bool enEsquina = distAlLider < 2.5f;

                if (!TieneSueloDebajo() && !enEsquina)
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
                tiempoSinSuelo = 0f;  // Líder en el aire → no se cuenta
            }
        }

        // Red de seguridad absoluta
        if (transform.position.y < -12f)
        {
            CaerYMorir();
            return;
        }

        // ── Registrar historial del líder ──
        if (ultimoLiderRegistrado != lider.transform)
        {
            ultimoLiderRegistrado = lider.transform;
            RellenarHistorial(lider.transform.position);
        }

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

        // ── Anti-adelanto (evita que el dino pase al líder, sin forzar distancia exacta) ──
        float xLider = lider.transform.position.x;
        bool  miraIzq = srLider.flipX;

        if (!miraIzq)
        { if (posObjetivo.x > xLider) posObjetivo.x = xLider; }
        else
        { if (posObjetivo.x < xLider) posObjetivo.x = xLider; }

        // ── Suavizado en X y Y para eliminar temblor ──
        // Se interpola TANTO X como Y para que no haya saltos bruscos
        // cuando el historial actualiza su entrada cada 0.05 s.
        float t    = suavizadoHorizontal * Time.deltaTime;
        float newX = Mathf.Lerp(transform.position.x, posObjetivo.x, t);
        float newY = Mathf.Lerp(transform.position.y, posObjetivo.y + offsetVertical, t);

        // ── Separación suave detrás (no teletransporta en esquinas) ──
        // Si ya está del lado correcto pero muy pegado, se aleja poco a poco.
        // Si quedó del lado contrario por un giro reciente, NO se fuerza:
        // se deja que el historial lo traiga solo para no lanzarlo al vacío.
        float sepDeseada = distanciaDetras * (indiceFila + 1);
        float ladoDetras = miraIzq ? 1f : -1f;
        float offsetActual = newX - xLider;
        bool ladoCorrecto = Mathf.Sign(offsetActual) == Mathf.Sign(ladoDetras);
        bool casiEncima = Mathf.Abs(offsetActual) < 0.05f;
        if ((ladoCorrecto || casiEncima) && Mathf.Abs(offsetActual) < sepDeseada)
        {
            float objetivoSeparado = xLider + ladoDetras * sepDeseada;
            newX = Mathf.MoveTowards(newX, objetivoSeparado, suavizadoHorizontal * Time.deltaTime * 0.5f);
        }

        transform.position = new Vector3(newX, newY, transform.position.z);

        // ── Orientación ──
        if (sr != null) sr.flipX = srLider.flipX;

        // ── Animator ──
        if (animator != null)
        {
            float vel = Mathf.Abs(posObjetivo.x - transform.position.x);
            animator.SetFloat("Velocidad", vel > 0.05f ? 1f : 0f);
            animator.SetBool("EnSuelo", lider.EstaEnSuelo());
        }
    }

    // ────────────────────────────────────────────────────────────
    //  Detección de plataforma bajo los pies
    // ────────────────────────────────────────────────────────────

    public bool TieneSueloDebajo()
    {
        Vector2 baseOrigen = (Vector2)transform.position + Vector2.up * 0.3f;

        // 5 rayos: centro + bordes medios + bordes extremos del sprite.
        // Cubre mejor las esquinas de plataformas angostas.
        return HaySueloEn(baseOrigen)
            || HaySueloEn(baseOrigen + Vector2.left  * 0.20f)
            || HaySueloEn(baseOrigen + Vector2.right * 0.20f)
            || HaySueloEn(baseOrigen + Vector2.left  * 0.42f)
            || HaySueloEn(baseOrigen + Vector2.right * 0.42f);
    }

    private bool HaySueloEn(Vector2 origen)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origen, Vector2.down, distanciaRayoSuelo);
        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger)              continue;
            if (hit.collider.gameObject == gameObject)                       continue;
            if (hit.collider.CompareTag("Player"))                           continue;

            if (hit.collider.CompareTag("SUELO") ||
                hit.collider.gameObject.layer == LayerMask.NameToLayer("SUELO"))
                return true;
        }
        return false;
    }

    // ────────────────────────────────────────────────────────────
    //  Caída al precipicio
    // ────────────────────────────────────────────────────────────

    public void CaerYMorir()
    {
        if (cayendo) return;
        cayendo  = true;
        reclutado = false;

        if (lider != null)
        {
            if (lider.sonidoMuerte != null)
            {
                Vector3 posAudio = Camera.main != null
                    ? Camera.main.transform.position
                    : transform.position;
                AudioSource.PlayClipAtPoint(lider.sonidoMuerte, posAudio);
            }
            lider.RemoverSeguidor(this);
            lider = null;
        }

        if (col != null) col.enabled = false;

        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType       = RigidbodyType2D.Dynamic;
        rb.gravityScale   = 3f;
        rb.linearVelocity = new Vector2(0f, -2.5f);

        if (animator != null)
        {
            animator.SetBool("EnSuelo", false);
            animator.SetFloat("Velocidad", 0f);
            animator.SetBool("Muerto", true);
            animator.SetTrigger("Morir");
        }

        Destroy(gameObject, 1.5f);
    }

    // ────────────────────────────────────────────────────────────
    //  Sacrificio (cuando el jugador recibe daño)
    // ────────────────────────────────────────────────────────────

    public void Sacrificar()
    {
        if (cayendo) return;
        cayendo  = true;
        reclutado = false;

        if (col != null) col.enabled = false;

        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType       = RigidbodyType2D.Dynamic;
        rb.gravityScale   = 2f;
        rb.linearVelocity = new Vector2(0f, 10f);

        if (animator != null)
        {
            animator.SetBool("Muerto", true);
            animator.SetTrigger("Morir");
        }

        Destroy(gameObject, 1.5f);
    }

    // ────────────────────────────────────────────────────────────
    //  Utilidades estáticas
    // ────────────────────────────────────────────────────────────

    private static void RellenarHistorial(Vector3 pos)
    {
        historialPosiciones.Clear();
        for (int i = 0; i < maxHistorial; i++)
            historialPosiciones.Add(pos);
        timerRegistro = 0f;
    }

    public static void LimpiarHistorial()
    {
        historialPosiciones.Clear();
        timerRegistro         = 0f;
        ultimoLiderRegistrado = null;
    }
}