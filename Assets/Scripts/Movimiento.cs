using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class Movimiento : MonoBehaviour
{
    [Header("Configuración Básica")]
    public float velocidad = 5f;
    public float fuerzaSalto = 14f;

    [Header("Componentes Visuales y Sonido")]
    public Animator animator;
    public AudioClip sonidoSalto;
    public AudioClip sonidoMuerte;

    [Header("Sistema de Vidas Extra")]
    public int maxSeguidores = 4;
    public List<DinoSeguidor> seguidores = new List<DinoSeguidor>();

    private Rigidbody2D rb;
    private AudioSource sfx;
    private SpriteRenderer sr;
    private Collider2D col;

    private float entradaX;
    private bool estaEnSuelo = false;
    private bool vivo = true;
    private float boostMult = 1f;
    private float boostTiempo = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sfx = GetComponent<AudioSource>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!vivo) return;

        entradaX = Input.GetAxisRaw("Horizontal");

        if (entradaX > 0) sr.flipX = false;
        else if (entradaX < 0) sr.flipX = true;

        // Salto
        if (Input.GetKeyDown(KeyCode.Space) && estaEnSuelo)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
            estaEnSuelo = false;
            if (sfx != null && sonidoSalto != null) sfx.PlayOneShot(sonidoSalto);
        }

        // Temporizador de boost
        if (boostTiempo > 0f)
        {
            boostTiempo -= Time.deltaTime;
            if (boostTiempo <= 0f) boostMult = 1f;
        }

        // Animator
        if (animator != null)
        {
            animator.SetFloat("Velocidad", Mathf.Abs(entradaX));
            animator.SetBool("EnSuelo", estaEnSuelo);
        }

        // Red de seguridad
        if (transform.position.y < -12f) Morir();
    }

    void FixedUpdate()
    {
        if (!vivo) return;
        rb.linearVelocity = new Vector2(entradaX * velocidad * boostMult, rb.linearVelocity.y);
    }

    // Detección de suelo
    private void OnCollisionEnter2D(Collision2D c) { if (c.gameObject.CompareTag("SUELO")) estaEnSuelo = true; }
    private void OnCollisionStay2D(Collision2D c) { if (c.gameObject.CompareTag("SUELO")) estaEnSuelo = true; }
    private void OnCollisionExit2D(Collision2D c) { if (c.gameObject.CompareTag("SUELO")) estaEnSuelo = false; }

    public bool EstaEnSuelo() => estaEnSuelo;

    public void ActivarBoost(float multiplicador, float duracion)
    {
        boostMult = multiplicador;
        boostTiempo = duracion;
    }

    public bool AgregarSeguidor(DinoSeguidor amigo)
    {
        if (seguidores.Count >= maxSeguidores) return false;
        seguidores.Add(amigo);
        amigo.AsignarIndice(seguidores.Count - 1);
        return true;
    }

    public void RemoverSeguidor(DinoSeguidor amigo)
    {
        if (seguidores.Contains(amigo))
        {
            seguidores.Remove(amigo);
            for (int i = 0; i < seguidores.Count; i++)
            {
                if (seguidores[i] != null)
                    seguidores[i].AsignarIndice(i);
            }
        }
    }

    public void Morir(bool esPrecipicio = false)
    {
        if (!vivo) return;

        // Si hay un dino de vida extra, se sacrifica y da super impulso
        if (seguidores.Count > 0)
        {
            int ultimo = seguidores.Count - 1;
            DinoSeguidor sacrificado = seguidores[ultimo];
            seguidores.RemoveAt(ultimo);

            if (sacrificado != null) sacrificado.Sacrificar();

            for (int i = 0; i < seguidores.Count; i++)
                if (seguidores[i] != null) seguidores[i].AsignarIndice(i);

            if (sfx != null && sonidoSalto != null) sfx.PlayOneShot(sonidoSalto);

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto * 1.25f);
            estaEnSuelo = false;
            return;
        }

        // Muerte real
        vivo = false;

        if (sfx != null && sonidoMuerte != null) sfx.PlayOneShot(sonidoMuerte);

        if (animator != null)
        {
            animator.SetBool("Muerto", true);
            animator.SetTrigger("Morir");
        }

        rb.linearVelocity = new Vector2(0f, 10f);
        if (col != null) col.enabled = false;

        Invoke(nameof(MostrarBotonMuerte), 1.5f);
    }

    void MostrarBotonMuerte()
    {
        var temp = FindFirstObjectByType<Temporizador>();
        if (temp != null) temp.MostrarGameOver("TE CAISTE");
        else ReiniciarNivel();
    }

    private void ReiniciarNivel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}