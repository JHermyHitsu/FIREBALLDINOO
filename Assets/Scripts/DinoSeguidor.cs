using UnityEngine;

public class DinoSeguidor : MonoBehaviour
{
    [Header("Ajustes")]
    public float distanciaDetras = 1.4f;
    public float suavizadoHorizontal = 12f;
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
        if (reclutado) return;
        if (otro.CompareTag("Player"))
        {
            Movimiento jugador = otro.GetComponent<Movimiento>();
            if (jugador != null && jugador.AgregarSeguidor(this))
                Reclutar(jugador);
        }
    }

    public void Reclutar(Movimiento jugador)
    {
        reclutado = true;
        lider = jugador;
        srLider = lider.GetComponent<SpriteRenderer>();

        if (col != null) col.enabled = false;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void AsignarIndice(int indice) => indiceFila = indice;

    void LateUpdate()
    {
        if (!reclutado || lider == null) return;

        bool liderMiraIzq = srLider.flipX;
        float dirDetras = liderMiraIzq ? 1f : -1f;

        float targetX = lider.transform.position.x + (dirDetras * distanciaDetras * (indiceFila + 1));
        float nuevaX = Mathf.Lerp(transform.position.x, targetX, suavizadoHorizontal * Time.deltaTime);
        float nuevaY = lider.transform.position.y + offsetVertical;

        transform.position = new Vector3(nuevaX, nuevaY, transform.position.z);

        if (sr != null) sr.flipX = liderMiraIzq;

        if (animator != null)
        {
            float vel = Mathf.Abs(targetX - transform.position.x);
            animator.SetFloat("Velocidad", vel > 0.08f ? 1f : 0f);
            animator.SetBool("EnSuelo", lider.EstaEnSuelo());
        }
    }

    // Llamado cuando el jugador recibe daño y este dino se sacrifica para salvarlo
    public void Sacrificar()
    {
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