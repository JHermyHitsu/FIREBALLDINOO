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

    [Header("Sistema de Vidas Extra (Compañeros)")]
    public int maxSeguidores = 4;
    public System.Collections.Generic.List<DinoSeguidor> seguidores = new System.Collections.Generic.List<DinoSeguidor>();

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
    private bool esInvencible = false;

    // Boost de velocidad (Muslo de carne)
    private float boostMult = 1f;
    private float boostTiempo = 0f;
    private Vector3 posicionInicial;
    private Vector3 ultimoSueloSeguro;

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
        ultimoSueloSeguro = transform.position;
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

        // 4. Parámetros para el Animator y guardar suelo seguro
        if (estaEnSuelo)
        {
            ultimoSueloSeguro = transform.position;
        }

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

    // Devuelve si el jugador está pisando el suelo (para sincronizar al acompañante)
    public bool EstaEnSuelo()
    {
        return estaEnSuelo;
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

    // Método ejecutado al caer en la Zona de Muerte o tocar peligro
    public void Morir()
    {
        if (!vivo || esInvencible) return;

        // Si tenemos dinos acompañantes (vidas extra):
        if (seguidores.Count > 0)
        {
            int ultimoIndice = seguidores.Count - 1;
            DinoSeguidor dinoSacrificado = seguidores[ultimoIndice];
            seguidores.RemoveAt(ultimoIndice);

            if (dinoSacrificado != null)
                dinoSacrificado.Sacrificar();

            // Reorganizar las posiciones de la fila para los que quedan
            for (int i = 0; i < seguidores.Count; i++)
            {
                if (seguidores[i] != null)
                    seguidores[i].AsignarIndice(i);
            }

            // Sonido de daño
            if (sfx != null && sonidoMuerte != null)
                sfx.PlayOneShot(sonidoMuerte);

            // Reaparecer al jugador en el último suelo seguro (Last Safe Ground)
            transform.position = ultimoSueloSeguro;
            rb.linearVelocity = Vector2.zero;
            estaEnSuelo = true;

            // Invencibilidad temporal (parpadeo)
            StartCoroutine(InvencibilidadTemporal(1.5f));

            Debug.Log($"[Vida Extra] ¡Un acompañante te salvó! Reapareciendo en el último suelo seguro. Vidas restantes: {seguidores.Count}");
            return;
        }

        // Si no queda ningún acompañante: Muerte definitiva
        MuerteDefinitiva();
    }

    // Rutina de parpadeo para indicar invencibilidad tras perder una vida
    private System.Collections.IEnumerator InvencibilidadTemporal(float duracion)
    {
        esInvencible = true;
        float parpadeo = 0.1f;
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(parpadeo);
            tiempo += parpadeo;
        }

        spriteRenderer.enabled = true;
        esInvencible = false;
    }

    // Muerte definitiva cuando no te quedan más vidas
    private void MuerteDefinitiva()
    {
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
        // Limpiar acompañantes al reaparecer
        for (int i = 0; i < seguidores.Count; i++)
        {
            if (seguidores[i] != null)
                Destroy(seguidores[i].gameObject);
        }
        seguidores.Clear();

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

    // Método llamado por DinoSeguidor cuando el jugador lo toca
    public bool AgregarSeguidor(DinoSeguidor amigo)
    {
        if (seguidores.Count >= maxSeguidores)
        {
            Debug.Log("[Horda] Fila llena (4/4).");
            return false;
        }

        seguidores.Add(amigo);
        amigo.AsignarIndice(seguidores.Count - 1);
        Debug.Log($"[Horda] ¡Amigo rescatado como vida extra! Tienes {seguidores.Count} de {maxSeguidores}");
        return true;
    }
}