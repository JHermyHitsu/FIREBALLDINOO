using UnityEngine;

/// <summary>
/// Salud y muerte del jugador: sacrificio de seguidor (vida extra) y muerte real.
/// No sabe de input, fisica ni boost; delega animacion a EstadoJugador.
/// </summary>
public class SistemaSaludyMuerte : MonoBehaviour
{
    public Movimiento movimiento;
    public EstadoJugador estadoJugador;
    public AudioClip sonidoMuerte;

    private Rigidbody2D rb;
    private Collider2D col;
    private AudioSource audioSource;

    bool vivo = true;
    public bool Vivo => vivo;

    void Awake()
    {
        if (movimiento == null) movimiento = GetComponent<Movimiento>();
        if (estadoJugador == null) estadoJugador = GetComponent<EstadoJugador>();

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (sonidoMuerte == null && movimiento != null)
            sonidoMuerte = movimiento.sonidoMuerte;
    }

    public void Morir()
    {
        if (!vivo) return;
        if (estadoJugador != null && estadoJugador.Inmune) return;

        // Si hay un dino de vida extra, se sacrifica y da super impulso
        if (movimiento != null && movimiento.seguidores != null && movimiento.seguidores.Count > 0)
        {
            int ultimo = movimiento.seguidores.Count - 1;
            DinoSeguidor sacrificado = movimiento.seguidores[ultimo];
            movimiento.seguidores.RemoveAt(ultimo);

            if (sacrificado != null) sacrificado.Sacrificar();

            for (int i = 0; i < movimiento.seguidores.Count; i++)
                if (movimiento.seguidores[i] != null)
                    movimiento.seguidores[i].AsignarIndice(i);

            if (audioSource != null && movimiento.sonidoSalto != null)
                audioSource.PlayOneShot(movimiento.sonidoSalto);

            // Ventana de inmunidad mientras el jugador sale impulsado de la zona de daño
            if (estadoJugador != null) estadoJugador.SetInmune(0.8f);
            if (rb != null) rb.linearVelocity = new Vector2(rb.linearVelocity.x, (movimiento != null ? movimiento.fuerzaSalto : 14f) * 1.4f);
            if (movimiento != null) movimiento.SetEnSuelo(false);
            return;
        }

        // Muerte real
        vivo = false;

        AudioClip clip = sonidoMuerte != null ? sonidoMuerte : (movimiento != null ? movimiento.sonidoMuerte : null);
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);

        if (estadoJugador != null) estadoJugador.AnimarMuerte();

        if (rb != null) rb.linearVelocity = new Vector2(0f, 10f);
        if (col != null) col.enabled = false;

        if (movimiento != null) movimiento.ResetearBoost();

        Invoke(nameof(MostrarBotonMuerte), 1.5f);
    }

    void MostrarBotonMuerte()
    {
        PanelFinPartida.EnsureExists().MostrarGameOver(todosCaidos: true);
    }
}