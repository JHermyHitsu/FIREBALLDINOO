using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(Animator))]
public class DinoSeguidor : MonoBehaviour
{
    [Header("Ajustes de Seguimiento Simple")]
    [Tooltip("Distancia entre cada dinosaurio de la fila.")]
    public float distanciaDetras = 0.75f;
    [Tooltip("Rapidez con la que sigue al de adelante (mayor = más rápido).")]
    public float suavizadoHorizontal = 12f;
    [Tooltip("Ajuste de altura opcional respecto al jugador.")]
    public float offsetVertical = 0f;

    [Header("Sonido")]
    [Tooltip("Efecto de sonido al morir o ser sacrificado.")]
    public AudioClip sonidoMuerte;

    [Header("Estado")]
    public bool reclutado = false;

    // Mantenido para compatibilidad con el Inspector del Prefab
    [HideInInspector] public int pasosSeparacion = 8;

    private Movimiento lider;
    private SpriteRenderer srLider;
    private Collider2D col;
    private SpriteRenderer sr;
    private Animator animator;
    private Rigidbody2D rb;

    private int indiceFila = 0;
    private bool cayendo = false;
    private float velocidadX = 0f;
    private float velocidadY = 0f;

    private static readonly int HashVelocidad = Animator.StringToHash("Velocidad");
    private static readonly int HashEnSuelo = Animator.StringToHash("EnSuelo");
    private static readonly int HashMuerto = Animator.StringToHash("Muerto");
    private static readonly int HashMorir = Animator.StringToHash("Morir");

    void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (Mathf.Approximately(transform.localScale.z, 0f))
        {
            Vector3 s = transform.localScale;
            s.z = 1f;
            transform.localScale = s;
        }

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }
    }

    void Start()
    {
        if (animator != null)
        {
            animator.SetBool(HashEnSuelo, true);
            animator.SetFloat(HashVelocidad, 0f);
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
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void ReVincular(Movimiento jugador) => Reclutar(jugador);

    public void AsignarIndice(int indice)
    {
        indiceFila = Mathf.Max(0, indice);
        if (sr != null && srLider != null)
            sr.sortingOrder = Mathf.Max(1, srLider.sortingOrder - 1 - indiceFila);
    }

    void LateUpdate()
    {
        if (cayendo)
        {
            if (transform.position.y < -15f)
                Destroy(gameObject);
            return;
        }

        if (!reclutado || lider == null) return;

        // 1. Seguir en cadena: el primero sigue al jugador; los demás siguen al dino de adelante
        Transform objetivo = lider.transform;
        if (indiceFila > 0 && indiceFila - 1 < lider.seguidores.Count)
        {
            DinoSeguidor anterior = lider.seguidores[indiceFila - 1];
            if (anterior != null && anterior != this && anterior.reclutado)
                objetivo = anterior.transform;
        }

        // 2. Calcular el punto justo detrás del objetivo actual
        float ladoDetras = (srLider != null && srLider.flipX) ? 1f : -1f;
        float destinoX = objetivo.position.x + (ladoDetras * distanciaDetras);
        float destinoY = objetivo.position.y + (objetivo == lider.transform ? offsetVertical : 0f);

        // 3. Moverse suavemente hacia ese punto (crea el efecto ola en cadena de forma natural)
        float tiempoSuaveX = 1f / Mathf.Max(1f, suavizadoHorizontal);
        float tiempoSuaveY = 0.055f;

        float nuevoX = Mathf.SmoothDamp(transform.position.x, destinoX, ref velocidadX, tiempoSuaveX);
        float nuevoY = Mathf.SmoothDamp(transform.position.y, destinoY, ref velocidadY, tiempoSuaveY);

        transform.position = new Vector3(nuevoX, nuevoY, transform.position.z);

        // 4. Mirar hacia donde camina; al frenar, mirar igual que el líder
        if (sr != null)
        {
            if (Mathf.Abs(velocidadX) > 0.25f)
                sr.flipX = velocidadX < 0f;
            else if (srLider != null)
                sr.flipX = srLider.flipX;
        }

        // 5. Animación simple de caminar y correr (con boost de carne)
        if (animator != null)
        {
            bool enMovimiento = Mathf.Abs(velocidadX) > 0.2f;
            float valorVelocidad = 0f;
            if (enMovimiento)
            {
                valorVelocidad = (lider != null && lider.TieneBoost()) ? 1.5f : 1f;
            }
            bool enSuelo = Mathf.Abs(velocidadY) < 1.2f && lider.EstaEnSuelo();

            animator.SetFloat(HashVelocidad, valorVelocidad);
            animator.SetBool(HashEnSuelo, enSuelo);
        }
    }

    public void Sacrificar()
    {
        if (cayendo) return;
        cayendo = true;
        reclutado = false;

        if (col != null) col.enabled = false;

        AudioClip clip = sonidoMuerte != null ? sonidoMuerte : (lider != null ? lider.sonidoMuerte : null);
        if (clip != null)
        {
            Vector3 posAudio = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(clip, posAudio);
        }

        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 2f;
        rb.linearVelocity = new Vector2(0f, 10f);

        if (animator != null)
        {
            animator.SetBool(HashEnSuelo, false);
            animator.SetFloat(HashVelocidad, 0f);
            animator.SetBool(HashMuerto, true);
            animator.SetTrigger(HashMorir);
        }

        Destroy(gameObject, 1.5f);
    }

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

    // Mantenido por compatibilidad con TransicionManager y Temporizador
    public static void LimpiarHistorial() { }
}