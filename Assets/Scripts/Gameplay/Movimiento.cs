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

    // Fila de seguidores vive en ControladorFila.
    public ControladorFila controladorFila;
    public List<DinoSeguidor> seguidores => controladorFila != null ? controladorFila.seguidores : null;

    // Estado del jugador (boost + animacion) vive en EstadoJugador.
    public EstadoJugador estadoJugador;

    private Rigidbody2D rb;
    private AudioSource sfx;
    private SpriteRenderer sr;
    private Collider2D col;

    private float entradaX;
    private bool estaEnSuelo = false;

    // Salud y muerte viven en SistemaSaludyMuerte.
    public SistemaSaludyMuerte saludYMuerte;
    public bool Vivo => saludYMuerte != null ? saludYMuerte.Vivo : true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sfx = GetComponent<AudioSource>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        if (estadoJugador == null) estadoJugador = GetComponent<EstadoJugador>();
        if (estadoJugador == null) estadoJugador = gameObject.AddComponent<EstadoJugador>();

        if (saludYMuerte == null) saludYMuerte = GetComponent<SistemaSaludyMuerte>();
        if (saludYMuerte == null) saludYMuerte = gameObject.AddComponent<SistemaSaludyMuerte>();

        if (controladorFila == null) controladorFila = GetComponent<ControladorFila>();
        if (controladorFila == null) controladorFila = gameObject.AddComponent<ControladorFila>();
    }

    void Update()
    {
        if (!Vivo) return;
        if (Time.timeScale == 0f || Pausa.EstaPausado) return; // pausa: no procesar input

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
        if (!Vivo) return;
        float mult = (estadoJugador != null) ? estadoJugador.BoostMult : 1f;
        rb.linearVelocity = new Vector2(entradaX * velocidad * mult, rb.linearVelocity.y);
    }

    // Detección de suelo
    private void OnCollisionEnter2D(Collision2D c) { if (c.gameObject.CompareTag("SUELO")) estaEnSuelo = true; }
    private void OnCollisionStay2D(Collision2D c) { if (c.gameObject.CompareTag("SUELO")) estaEnSuelo = true; }
    private void OnCollisionExit2D(Collision2D c) { if (c.gameObject.CompareTag("SUELO")) estaEnSuelo = false; }

    public bool EstaEnSuelo() => estaEnSuelo;
    public void SetEnSuelo(bool v) => estaEnSuelo = v;

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
        if (controladorFila != null) return controladorFila.AgregarSeguidor(amigo);
        return false;
    }

    public void RemoverSeguidor(DinoSeguidor amigo)
    {
        if (controladorFila != null) controladorFila.RemoverSeguidor(amigo);
    }

    public void Morir()
    {
        if (saludYMuerte != null) saludYMuerte.Morir();
    }
}