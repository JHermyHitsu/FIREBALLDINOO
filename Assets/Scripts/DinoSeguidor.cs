using UnityEngine;

// Script para los dinos acompañantes que funcionan como vidas extra / escudos
public class DinoSeguidor : MonoBehaviour
{
    [Header("Ajustes")]
    public float distanciaDetras = 1.4f; // Distancia detrás del líder
    public float suavizadoHorizontal = 12f; // Velocidad de persecución en X
    public float offsetVertical = 0f; // Altura relativa al líder

    [Header("Estado")]
    public bool reclutado = false;

    private Movimiento lider;
    private Collider2D col;
    private SpriteRenderer sr;
    private Animator animator;
    private Rigidbody2D rb;
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
            {
                Reclutar(jugador);
            }
        }
    }

    public void Reclutar(Movimiento jugador)
    {
        reclutado = true;
        lider = jugador;

        // Desactivar collider físico para que sea un compañero que no empuje ni estorbe
        if (col != null)
            col.enabled = false;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        Debug.Log("[DinoSeguidor] ¡Compañero reclutado como vida extra!");
    }

    public void AsignarIndice(int indice)
    {
        indiceFila = indice;
    }

    // LateUpdate para seguir al líder después de que se hayan calculado todas las físicas
    void LateUpdate()
    {
        if (!reclutado || lider == null) return;

        // 1. Determinar hacia dónde está mirando el líder
        bool liderMiraIzq = lider.GetComponent<SpriteRenderer>().flipX;
        float dirDetras = liderMiraIzq ? 1f : -1f;

        // 2. Posición objetivo: en X lo sigue suavemente, en Y se sincroniza exacto para evitar gravedad lunar
        float targetX = lider.transform.position.x + (dirDetras * distanciaDetras * (indiceFila + 1));
        float nuevaX = Mathf.Lerp(transform.position.x, targetX, suavizadoHorizontal * Time.deltaTime);
        float nuevaY = lider.transform.position.y + offsetVertical;

        transform.position = new Vector3(nuevaX, nuevaY, transform.position.z);

        // 3. Orientación visual
        if (sr != null)
        {
            sr.flipX = liderMiraIzq;
        }

        // 4. Animaciones
        if (animator != null)
        {
            float velocidadMov = Mathf.Abs(targetX - transform.position.x);
            animator.SetFloat("Velocidad", velocidadMov > 0.08f ? 1f : 0f);
            animator.SetBool("EnSuelo", lider.EstaEnSuelo());
        }
    }

    // Llamado cuando el jugador recibe daño y este dino se sacrifica para salvarlo
    public void Sacrificar()
    {
        reclutado = false;
        Destroy(gameObject);
    }
}