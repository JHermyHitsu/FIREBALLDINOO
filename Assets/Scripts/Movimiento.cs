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

    [Header("Sistema de Vidas Extra (Compañeros)")]
    public int maxSeguidores = 4;
    public List<DinoSeguidor> seguidores = new List<DinoSeguidor>();

    // Componentes del GameObject
    private Rigidbody2D rb;
    private AudioSource sfx;
    private SpriteRenderer sr;

    // Variables de estado
    private float entradaX;
    private bool estaEnSuelo = false;
    private bool vivo = true;

    // Power-up de velocidad (Muslo de carne)
    private float boostMult = 1f;
    private float boostTiempo = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sfx = GetComponent<AudioSource>();
        sr = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!vivo) return;

        // 1. Leer teclado (-1 = Izquierda, 1 = Derecha)
        entradaX = Input.GetAxisRaw("Horizontal");

        // Girar sprite según dirección
        if (entradaX > 0) sr.flipX = false;
        else if (entradaX < 0) sr.flipX = true;

        // 2. Salto básico
        if (Input.GetKeyDown(KeyCode.Space) && estaEnSuelo)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
            estaEnSuelo = false;

            if (sfx != null && sonidoSalto != null)
                sfx.PlayOneShot(sonidoSalto);
        }

        // 3. Temporizador de boost de carne
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

        // 5. Red de seguridad si cae por debajo del nivel
        if (transform.position.y < -12f)
        {
            Morir();
        }
    }

    void FixedUpdate()
    {
        if (!vivo) return;

        // Movimiento horizontal
        rb.linearVelocity = new Vector2(entradaX * velocidad * boostMult, rb.linearVelocity.y);
    }

    // --- DETECCIÓN DE SUELO BÁSICA ---
    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("SUELO")) estaEnSuelo = true;
    }

    private void OnCollisionStay2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("SUELO")) estaEnSuelo = true;
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("SUELO")) estaEnSuelo = false;
    }

    public bool EstaEnSuelo() => estaEnSuelo;

    // --- BOOST DE CARNE ---
    public void ActivarBoost(float multiplicador, float duracion)
    {
        boostMult = multiplicador;
        boostTiempo = duracion;
    }

    // --- SISTEMA DE SEGUIDORES (VIDAS EXTRA) ---
    public bool AgregarSeguidor(DinoSeguidor amigo)
    {
        if (seguidores.Count >= maxSeguidores) return false;

        seguidores.Add(amigo);
        amigo.AsignarIndice(seguidores.Count - 1);
        return true;
    }

    // --- MUERTE Y SALTO DE RESCATE (ESTILO YOSHI) ---
    public void Morir(bool esPrecipicio = false)
    {
        if (!vivo) return;

        // Caso A: Si tienes un dino de vida extra, se sacrifica y te da un super impulso hacia arriba
        if (seguidores.Count > 0)
        {
            int ultimoIndice = seguidores.Count - 1;
            DinoSeguidor dinoSacrificado = seguidores[ultimoIndice];
            seguidores.RemoveAt(ultimoIndice);

            if (dinoSacrificado != null)
                dinoSacrificado.Sacrificar();

            // Reorganizar fila para los dinos restantes
            for (int i = 0; i < seguidores.Count; i++)
                if (seguidores[i] != null) seguidores[i].AsignarIndice(i);

            if (sfx != null && sonidoSalto != null)
                sfx.PlayOneShot(sonidoSalto);

            // ¡Super impulso hacia arriba! El jugador retoma el control en el aire
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto * 1.25f);
            estaEnSuelo = false;
            return;
        }

        // Caso B: Si tienes 0 vidas, muerte normal y reinicio de escena
        vivo = false;

        if (sfx != null && sonidoMuerte != null)
            sfx.PlayOneShot(sonidoMuerte);

        if (animator != null)
        {
            animator.SetBool("Muerto", true);
            animator.SetTrigger("Morir");
        }

        // Pequeño salto de derrota estilo Mario
        rb.linearVelocity = new Vector2(0f, 10f);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Invoke(nameof(ReiniciarNivel), 2f);
    }

    private void ReiniciarNivel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}