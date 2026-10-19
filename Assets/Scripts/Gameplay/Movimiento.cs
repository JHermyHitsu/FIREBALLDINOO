using UnityEngine;
using System.Collections.Generic;

public class Movimiento : MonoBehaviour
{
    [Header("Configuración Básica")]
    public float velocidad = 5f;
    public float fuerzaSalto = 14f;

    [Header("Componentes Visuales y Sonido")]
    public AudioClip sonidoSalto;
    public AudioClip sonidoMuerte;

    [Header("Sistema de Vidas Extra")]
    public int maxSeguidores = 4;
    public List<DinoSeguidor> seguidores = new List<DinoSeguidor>();

    // Estado del jugador (boost + animacion) vive en EstadoJugador.
    public EstadoJugador estadoJugador;

    private Rigidbody2D rb;
    private AudioSource sfx;
    private SpriteRenderer sr;
    private Collider2D col;

    private float entradaX;
    private bool estaEnSuelo = false;
    private bool vivo = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sfx = GetComponent<AudioSource>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        if (estadoJugador == null) estadoJugador = GetComponent<EstadoJugador>();
        if (estadoJugador == null) estadoJugador = gameObject.AddComponent<EstadoJugador>();
    }

    void Update()
    {
        if (!vivo) return;

        if (estadoJugador != null) estadoJugador.Tick(Time.deltaTime);

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

        // Animacion (delegada a EstadoJugador)
        if (estadoJugador != null)
            estadoJugador.ActualizarAnimacion(entradaX, estaEnSuelo);

        // Red de seguridad
        if (transform.position.y < -12f) Morir();
    }

    void FixedUpdate()
    {
        if (!vivo) return;
        float mult = (estadoJugador != null) ? estadoJugador.BoostMult : 1f;
        rb.linearVelocity = new Vector2(entradaX * velocidad * mult, rb.linearVelocity.y);
    }

    // Detección de suelo
    private void OnCollisionEnter2D(Collision2D c) { if (c.gameObject.CompareTag("SUELO")) estaEnSuelo = true; }
    private void OnCollisionStay2D(Collision2D c) { if (c.gameObject.CompareTag("SUELO")) estaEnSuelo = true; }
    private void OnCollisionExit2D(Collision2D c) { if (c.gameObject.CompareTag("SUELO")) estaEnSuelo = false; }

    public bool EstaEnSuelo() => estaEnSuelo;

    // Resetea el boost (usado al morir / reintentar) para que no quede activo.
    public void ResetearBoost()
    {
        if (estadoJugador != null) estadoJugador.ResetearBoost();
    }

    public void ActivarBoost(float multiplicador, float duracion)
    {
        if (estadoJugador != null) estadoJugador.ActivarBoost(multiplicador, duracion);
    }

    public bool TieneBoost() => estadoJugador != null && estadoJugador.TieneBoost();
    public bool BoostActivo => estadoJugador != null && estadoJugador.BoostActivo;
    public float BoostTiempoRestante => estadoJugador != null ? estadoJugador.BoostTiempoRestante : 0f;
    public float BoostDuracion => estadoJugador != null ? estadoJugador.BoostDuracion : 1f;

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
        if (!vivo || (estadoJugador != null && estadoJugador.Inmune)) return;

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

            // Ventana de inmunidad mientras el jugador sale impulsado de la zona de daño
            if (estadoJugador != null) estadoJugador.SetInmune(0.8f);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto * 1.4f);
            estaEnSuelo = false;
            return;
        }

        // Muerte real
        vivo = false;

        if (sfx != null && sonidoMuerte != null) sfx.PlayOneShot(sonidoMuerte);

        if (estadoJugador != null) estadoJugador.AnimarMuerte();

        rb.linearVelocity = new Vector2(0f, 10f);
        if (col != null) col.enabled = false;

        Invoke(nameof(MostrarBotonMuerte), 1.5f);
    }

    void MostrarBotonMuerte()
    {
        // El Temporizador es persistente; EnsureExists cubre el Play directo
        // en Desierto/Cueva. Reintentar vuelve siempre a Bosque.
        PanelFinPartida.EnsureExists().MostrarGameOver(todosCaidos: true);
    }
}