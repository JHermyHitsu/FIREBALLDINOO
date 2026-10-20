using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager / Coordinador del ciclo de vida de la partida.
/// El GameObject es persistente (DontDestroyOnLoad) y orquesta:
/// - Reloj: conteo regresivo y alerta de pánico (delegado a Reloj.cs).
/// - HUD: contador de dinos y barra de boost (delegado a HUD.cs).
/// - Fin de partida: paneles GameOver/Victoria (delegado a PanelFinPartida.cs).
/// - Flujo de sesión: Reintentar (reseteo a Bosque) y Salir (menú principal).
/// </summary>
public class Temporizador : MonoBehaviour
{
    public static Temporizador Instance;
    public const string EscenaBosque = "ESCENA_BIOMA_BOSQUE";

    // Referencias a los componentes hermanos (viven en el mismo GameManager).
    // No serializados: se resuelven en runtime en Awake.
    HUD hud;
    Reloj reloj;
    PanelFinPartida panelFinPartida;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Resolver los componentes hermanos ANTES de AsegurarUI, para que
        // AsegurarUI pueda delegar en ellos.
        panelFinPartida = GetComponent<PanelFinPartida>();
        if (panelFinPartida == null) panelFinPartida = gameObject.AddComponent<PanelFinPartida>();

        hud = GetComponent<HUD>();
        if (hud == null) hud = gameObject.AddComponent<HUD>();

        reloj = GetComponent<Reloj>();
        if (reloj == null) reloj = gameObject.AddComponent<Reloj>();

        AsegurarUI();

        // PanelFinPartida se suscribe a sceneLoaded por su cuenta.
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Devuelve el Temporizador persistente; lo crea si la escena no trae uno
    // (p. ej. Play directo en Desierto/Cueva).
    public static Temporizador EnsureExists()
    {
        var existente = FindFirstObjectByType<Temporizador>();
        if (existente != null)
        {
            existente.AsegurarUI();
            return existente;
        }
        var go = new GameObject("GameManager");
        return go.AddComponent<Temporizador>(); // Awake() resuelve los componentes hermanos
    }

    public void AsegurarUI()
    {
        if (panelFinPartida != null) panelFinPartida.AsegurarUI();
    }

    void Update()
    {
        if (panelFinPartida != null && panelFinPartida.terminado) return;

        // Temblor de cámara cuando el tiempo está bajo (8s o menos)
        var cam = FindFirstObjectByType<Camara>();
        if (cam != null)
        {
            float tr = (reloj != null) ? reloj.TiempoRestante : 0f;
            if (tr <= 16f && tr > 0f)
                cam.ActivarTemblor(true, 1.2f);
            else
                cam.ActivarTemblor(false, 0f);
        }

        if (reloj != null && reloj.TiempoRestante <= 0f)
        {
            reloj.Pausar();
            reloj.Apagar();
            MostrarGameOver();
        }
    }

    public void MostrarGameOver(bool todosCaidos = false)
    {
        AsegurarUI();
        if (panelFinPartida != null) panelFinPartida.MostrarGameOver(todosCaidos);
    }

    public void MostrarVictoria()
    {
        if (panelFinPartida != null && panelFinPartida.terminado) return;
        AsegurarUI();
        if (panelFinPartida != null) panelFinPartida.MostrarVictoria();
    }

    public void Reintentar() // vuelve siempre a Bosque con el tiempo reseteado
    {
        Time.timeScale = 1f;
        if (panelFinPartida != null) panelFinPartida.terminado = false;

        // El reloj y el HUD se restauran siempre.
        if (reloj != null) reloj.Reiniciar();
        if (hud != null) hud.Reiniciar();
        if (panelFinPartida != null)
        {
            if (panelFinPartida.panelGameOver != null) panelFinPartida.panelGameOver.SetActive(false);
            if (panelFinPartida.panelVictoria != null) panelFinPartida.panelVictoria.SetActive(false);
        }
        var camGO = FindFirstObjectByType<Camara>();
        if (camGO != null) camGO.ActivarTemblor(false, 0f);
        var movReintentar = FindFirstObjectByType<Movimiento>();
        if (movReintentar != null) movReintentar.ResetearBoost();
        TransicionManager.EnsureExists(); // garantiza el intercambio del jugador al recargar
        SceneManager.LoadScene(EscenaBosque);
    }

    public void Salir() // vuelve al menú principal
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(Escenas.MenuInicio);
    }
}
