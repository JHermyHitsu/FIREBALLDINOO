using UnityEngine;

public class Movimiento : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float velocidad = 5f;
    public float fuerzaSalto = 14f;

    [Header("Feel del salto (Tolerancia)")]
    public float coyoteTime = 0.1f;         // Tiempo extra para saltar tras dejar el borde
    public float jumpBufferTime = 0.15f;    // Recuerda si presionaste salto antes de tocar suelo

    [Header("Better Jump (Gravedad realista)")]
    public float fallMultiplier = 2.5f;     // Cae más rápido para evitar salto flotante
    public float lowJumpMultiplier = 2f;    // Si sueltas Espacio antes, el salto es más corto
    public float maxFallSpeed = -18f;       // Velocidad máxima de caída

    [Header("Animación")]
    public Animator animator;

    [Header("Sonidos")]
    public AudioClip sonidoSalto;
    public AudioClip sonidoMuerte;

    // Componentes internos
    private Rigidbody2D rb;
    private AudioSource sfx;
    private Collider2D colDino;
    private SpriteRenderer spriteRenderer;

    // Variables de estado
    private float entradaX;
    private bool estaEnSuelo;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool saltoMantenido;
    private bool vivo = true;

    // Boost de velocidad (Muslo de carne)
    private float boostMult = 1f;
    private float boostTiempo = 0f;
    private Vector3 posicionInicial;

    void Start()
    {
        // Obtenemos las referencias a los componentes del mismo GameObject
        rb = GetComponent<Rigidbody2D>();
        sfx = GetComponent<AudioSource>();
        colDino = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        posicionInicial = transform.position;
    }

    void Update()
    {
        if (!vivo) return;

        // 1. Lectura del movimiento horizontal (-1 = izquierda, 1 = derecha)
        entradaX = Input.GetAxisRaw("Horizontal");

        // Girar el sprite según la dirección
        if (entradaX > 0) spriteRenderer.flipX = false;
        else if (entradaX < 0) spriteRenderer.flipX = true;

        // 2. Temporizadores de asistencia de salto (Jump Buffer y Coyote Time)
        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        if (estaEnSuelo)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        saltoMantenido = Input.GetKey(KeyCode.Space);

        // 3. Temporizador del boost de carne
        if (boostTiempo > 0f)
        {
            boostTiempo -= Time.deltaTime;
            if (boostTiempo <= 0f)
                boostMult = 1f;
        }

        // 4. Parámetros para el Animator
        if (animator != null)
        {
            animator.SetFloat("Velocidad", Mathf.Abs(entradaX));
            animator.SetBool("EnSuelo", estaEnSuelo);
        }
    }

    void FixedUpdate()
    {
        if (!vivo) return;

        // Aplicar velocidad horizontal (multiplicada por boost si está activo)
        rb.linearVelocity = new Vector2(entradaX * velocidad * boostMult, rb.linearVelocity.y);

        // Ejecutar salto si ambas condiciones de tolerancia se cumplen
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            estaEnSuelo = false;

            // Sonido de salto
            if (sfx != null && sonidoSalto != null)
                sfx.PlayOneShot(sonidoSalto);
        }

        // Modificadores de gravedad para un salto agradable ("Better Jump")
        if (rb.linearVelocity.y < 0)
        {
            // Cayendo: aplicar mayor gravedad
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !saltoMantenido)
        {
            // Saltando pero soltó el botón antes: cortar salto
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }

        // Limitar velocidad máxima de caída
        if (rb.linearVelocity.y < maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
    }

    // Detección de suelo por colisión
    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("SUELO") && TocaDesdeArriba(col))
            estaEnSuelo = true;
    }

    private void OnCollisionStay2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("SUELO") && TocaDesdeArriba(col))
            estaEnSuelo = true;
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("SUELO"))
            estaEnSuelo = false;
    }

    // Verifica que el contacto sea con los pies y no con los lados o cabeza
    private bool TocaDesdeArriba(Collision2D col)
    {
        foreach (var c in col.contacts)
        {
            if (c.normal.y > 0.5f) return true;
        }
        return false;
    }

    // Método público llamado por el prefab ItemComida
    public void ActivarBoost(float mult, float duracion)
    {
        if (!vivo) return;
        boostMult = mult;
        boostTiempo = duracion;
    }

    // Método ejecutado al caer en la Zona de Muerte
    public void Morir()
    {
        if (!vivo) return;
        vivo = false;

        if (sfx != null && sonidoMuerte != null)
            sfx.PlayOneShot(sonidoMuerte);

        if (animator != null)
        {
            animator.SetBool("Muerto", true);
            animator.SetTrigger("Morir");
        }

        // Pequeño salto hacia arriba al morir (estilo Mario Bros)
        rb.linearVelocity = new Vector2(0f, 12f);

        // Desactivamos el collider para que atraviese el suelo limpiamente al caer
        if (colDino != null)
            colDino.enabled = false;

        Invoke(nameof(Respawnear), 2f);
    }

    private void Respawnear()
    {
        // Restaurar posición y collider
        transform.position = posicionInicial;
        rb.linearVelocity = Vector2.zero;
        estaEnSuelo = false;

        if (colDino != null)
            colDino.enabled = true;

        if (animator != null)
            animator.SetBool("Muerto", false);

        vivo = true;
    }
}