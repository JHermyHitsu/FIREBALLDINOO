using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(Animator))]
public class DinoSeguidor : MonoBehaviour
{
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
    private Rigidbody2D rb;

    // Matematicas de la cadena viven en FilaDinos.
    public FilaDinos filaDinos;

    private int indiceFila = 0;
    private bool cayendo = false;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (filaDinos == null) filaDinos = GetComponent<FilaDinos>();
        if (filaDinos == null) filaDinos = gameObject.AddComponent<FilaDinos>();

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
        var anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("EnSuelo", true);
            anim.SetFloat("Velocidad", 0f);
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

        if (filaDinos != null)
        {
            filaDinos.lider = lider;
            filaDinos.srLider = srLider;
            filaDinos.ResetearVelocidades();
        }

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
        if (filaDinos != null) filaDinos.indiceFila = indiceFila;
        if (sr != null && srLider != null)
            sr.sortingOrder = Mathf.Max(1, srLider.sortingOrder - 1 - indiceFila);
    }

    public void Sacrificar()
    {
        if (cayendo) return;
        cayendo = true;
        reclutado = false;
        if (filaDinos != null) filaDinos.cayendo = true;

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

        var anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("EnSuelo", false);
            anim.SetFloat("Velocidad", 0f);
            anim.SetBool("Muerto", true);
            anim.SetTrigger("Morir");
        }

        Destroy(gameObject, 1.5f);
    }

}