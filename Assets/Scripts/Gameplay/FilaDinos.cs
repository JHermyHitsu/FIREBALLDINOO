using UnityEngine;

/// <summary>
/// Matematicas de la cadena de seguimiento: calculo del objetivo, SmoothDamp,
/// flip y animacion. No sabe de reclutamiento, sacrificio ni muerte.
/// </summary>
public class FilaDinos : MonoBehaviour
{
    [Header("Ajustes de Seguimiento Simple")]
    [Tooltip("Distancia entre cada dinosaurio de la fila.")]
    public float distanciaDetras = 0.75f;
    [Tooltip("Rapidez con la que sigue al de adelante (mayor = más rápido).")]
    public float suavizadoHorizontal = 12f;
    [Tooltip("Ajuste de altura opcional respecto al jugador.")]
    public float offsetVertical = 0f;


    // Estado compartido con DinoSeguidor (se asigna desde ahi)
    [HideInInspector] public int indiceFila = 0;
    [HideInInspector] public bool cayendo = false;
    [HideInInspector] public Movimiento lider;
    [HideInInspector] public SpriteRenderer srLider;

    private DinoSeguidor dinoSeguidor;
    private SpriteRenderer sr;
    private Animator animator;
    private Rigidbody2D rb;

    private float velocidadX = 0f;
    private float velocidadY = 0f;

    private static readonly int HashVelocidad = Animator.StringToHash("Velocidad");
    private static readonly int HashEnSuelo = Animator.StringToHash("EnSuelo");

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        dinoSeguidor = GetComponent<DinoSeguidor>();

    }

    public bool Reclutado => dinoSeguidor != null && dinoSeguidor.reclutado;

    public void ResetearVelocidades()
    {
        velocidadX = 0f;
        velocidadY = 0f;
    }

    void LateUpdate()
    {
        if (cayendo)
        {
            if (transform.position.y < -15f)
                Destroy(gameObject);
            return;
        }

        if (!Reclutado || lider == null) return;

        // 1. Seguir en cadena: el primero sigue al jugador; los demás siguen al dino de adelante
        Transform objetivo = lider.transform;
        if (indiceFila > 0 && indiceFila - 1 < lider.seguidores.Count)
        {
            DinoSeguidor anterior = lider.seguidores[indiceFila - 1];
            if (anterior != null && anterior != dinoSeguidor && anterior.reclutado)
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
}