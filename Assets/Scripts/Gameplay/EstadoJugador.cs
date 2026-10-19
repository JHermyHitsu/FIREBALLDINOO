using UnityEngine;

/// <summary>
/// Estado del jugador: boost (multiplicador + temporizador + inmunidad) y
/// animacion (Animator). No sabe de input, fisica, salto ni muerte real.
/// </summary>
public class EstadoJugador : MonoBehaviour
{
    [Header("Componentes")]
    public Animator animator;

    // Estado del boost
    float boostMult = 1f;
    float boostTiempo = 0f;
    float boostDuracion = 1f;
    float inmuneTimer = 0f;

    // Hashes del Animator
    static readonly int HashVelocidad = Animator.StringToHash("Velocidad");
    static readonly int HashEnSuelo = Animator.StringToHash("EnSuelo");
    static readonly int HashMuerto = Animator.StringToHash("Muerto");
    static readonly int HashMorir = Animator.StringToHash("Morir");

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    // Decrementa temporizadores. Llamar desde Movimiento.Update.
    public void Tick(float deltaTime)
    {
        if (inmuneTimer > 0f)
            inmuneTimer -= deltaTime;

        if (boostTiempo > 0f)
        {
            boostTiempo -= deltaTime;
            if (boostTiempo <= 0f) boostMult = 1f;
        }
    }

    // --- Boost ---

    public bool TieneBoost() => boostTiempo > 0f;
    public bool BoostActivo => boostTiempo > 0f;
    public float BoostTiempoRestante => boostTiempo;
    public float BoostDuracion => boostDuracion;
    public float BoostMult => boostMult;
    public bool Inmune => inmuneTimer > 0f;

    public void ActivarBoost(float multiplicador, float duracion)
    {
        boostMult = multiplicador;
        boostTiempo = duracion;
        boostDuracion = duracion;
    }

    public void ResetearBoost()
    {
        boostMult = 1f;
        boostTiempo = 0f;
        boostDuracion = 1f;
    }

    public void SetInmune(float tiempo)
    {
        inmuneTimer = tiempo;
    }

    // --- Animacion ---

    public void ActualizarAnimacion(float velocidad, bool enSuelo)
    {
        if (animator == null) return;
        animator.SetFloat(HashVelocidad, Mathf.Abs(velocidad) * boostMult);
        animator.SetBool(HashEnSuelo, enSuelo);
    }

    public void AnimarMuerte()
    {
        if (animator == null) return;
        animator.SetBool(HashMuerto, true);
        animator.SetTrigger(HashMorir);
    }

    public void ResetearAnimacion()
    {
        if (animator == null) return;
        animator.SetBool(HashMuerto, false);
        animator.SetFloat(HashVelocidad, 0f);
        animator.SetBool(HashEnSuelo, true);
    }
}