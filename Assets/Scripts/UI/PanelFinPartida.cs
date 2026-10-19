using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Paneles de fin de partida (GameOver, Perder, Victoria) y su reconexion al
/// cambiar de escena. No sabe del reloj ni del HUD; los apaga/pausa al terminar.
/// </summary>
public class PanelFinPartida : MonoBehaviour
{
    public static PanelFinPartida Instance;

    [Header("Paneles")]
    public GameObject panelGameOver;
    public GameObject panelPerder;

    // PanelVictoria vive en la escena de Cuevas: se busca en runtime, no se serializa.
    [System.NonSerialized] public GameObject panelVictoria;

    private AudioSource audioSource;

    // Estado de la partida
    public bool terminado = false;

    public const string NombrePanelGameOver = "PanelGameOver";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        AsegurarUI();

        SceneManager.sceneLoaded += AlCargarEscena;

        // Inicializar con la escena actual (Awake ocurre despues de que la
        // escena ya esta cargada, asi que sceneLoaded no dispara solo).
        AlCargarEscena(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= AlCargarEscena;
    }

    public static PanelFinPartida EnsureExists()
    {
        if (Instance != null) return Instance;
        var existente = FindFirstObjectByType<PanelFinPartida>();
        if (existente != null) return existente;
        var go = new GameObject("PanelFinPartida");
        return go.AddComponent<PanelFinPartida>();
    }

    public void AsegurarUI()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
        AsegurarHoverBotones(panelGameOver);
        AsegurarHoverBotones(panelPerder);
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        // Si se vuelve a una escena sin haber terminado, ningún panel debe quedar visible.
        if (!terminado)
        {
            if (panelGameOver != null) panelGameOver.SetActive(false);
            if (panelPerder != null) panelPerder.SetActive(false);
            if (panelVictoria != null) panelVictoria.SetActive(false);
        }

        // Reasignar PanelVictoria en la escena nueva (prioridad sobre el persistente)
        GameObject pvEscena = BuscarPanelVictoriaEnEscena();
        if (pvEscena != null && pvEscena != panelVictoria)
        {
            if (panelVictoria != null) panelVictoria.SetActive(false);
            panelVictoria = pvEscena;
        }

        // Reasignar PanelGameOver y PanelPerder de la escena nueva.
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var pg = BuscarHijoRecursivo(root, NombrePanelGameOver);
            if (pg == null) pg = BuscarHijoRecursivo(root, "PanelGamerOver");
            if (pg != null && pg != panelGameOver)
            {
                if (panelGameOver != null) panelGameOver.SetActive(false);
                panelGameOver = pg;
            }
            var pp = BuscarHijoRecursivo(root, "PanelPerder");
            if (pp != null && pp != panelPerder)
            {
                if (panelPerder != null) panelPerder.SetActive(false);
                panelPerder = pp;
            }
        }

        ReconectarBotonesPaneles();
    }

    void ReconectarBotonesPaneles()
    {
        ReconectarBotonEnPanel(panelGameOver, "BotonReintentar", OnReintentar);
        ReconectarBotonEnPanel(panelGameOver, "BotonSalir", OnSalir);
        ReconectarBotonEnPanel(panelPerder, "BotonReintentar", OnReintentar);
        ReconectarBotonEnPanel(panelPerder, "BotonSalir", OnSalir);
        AsegurarHoverBotones(panelGameOver);
        AsegurarHoverBotones(panelPerder);
    }

    void ReconectarBotonEnPanel(GameObject panel, string nombreBoton, UnityEngine.Events.UnityAction accion)
    {
        if (panel == null) return;
        var btnGO = BuscarHijoRecursivo(panel, nombreBoton);
        if (btnGO == null) return;
        var btn = btnGO.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(accion);
    }

    GameObject BuscarHijoRecursivo(GameObject obj, string nombre)
    {
        if (obj == null) return null;
        if (obj.name == nombre) return obj;
        foreach (Transform child in obj.transform)
        {
            var found = BuscarHijoRecursivo(child.gameObject, nombre);
            if (found != null) return found;
        }
        return null;
    }

    GameObject BuscarPanelVictoriaEnEscena()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var pv = BuscarHijoRecursivo(root, "PanelVictoria");
            if (pv != null) return pv;
        }
        return null;
    }

    void ActivarAncestros(GameObject go)
    {
        if (go == null) return;
        Transform t = go.transform.parent;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            t = t.parent;
        }
    }

    void AsegurarHoverBotones(GameObject panel)
    {
        if (panel == null) return;
        foreach (var btn in panel.GetComponentsInChildren<Button>(true))
        {
            if (btn.GetComponent<HoverBoton>() == null)
                btn.gameObject.AddComponent<HoverBoton>();
        }
    }

    // --- API ---

    public void MostrarGameOver(bool todosCaidos = false)
    {
        AsegurarUI();
        terminado = true;

        var mov = FindFirstObjectByType<Movimiento>();
        if (mov != null) mov.enabled = false;

        // Apagar HUD y pausar reloj (componentes hermanos del mismo GameManager).
        var hud = GetComponent<HUD>();
        if (hud != null) hud.Apagar();
        var reloj = GetComponent<Reloj>();
        if (reloj != null) { reloj.Apagar(); reloj.Pausar(); }

        var camGO = FindFirstObjectByType<Camara>();
        if (camGO != null) camGO.ActivarTemblor(false, 0f);

        // Apagar boost (segmentos) — ya cubierto por hud.Apagar().

        if (todosCaidos && panelPerder != null)
        {
            if (panelGameOver != null) panelGameOver.SetActive(false);
            AsegurarHoverBotones(panelPerder);
            panelPerder.SetActive(true);
        }
        else if (panelGameOver != null)
        {
            if (panelPerder != null) panelPerder.SetActive(false);
            AsegurarHoverBotones(panelGameOver);
            panelGameOver.SetActive(true);
        }
    }

    public void MostrarVictoria()
    {
        if (terminado) return;
        AsegurarUI();
        terminado = true;

        var mov = FindFirstObjectByType<Movimiento>();
        if (mov != null) mov.enabled = false;

        var hud = GetComponent<HUD>();
        if (hud != null) hud.Apagar();
        var reloj = GetComponent<Reloj>();
        if (reloj != null) { reloj.Apagar(); reloj.Pausar(); }

        if (mov != null) mov.ResetearBoost();

        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (panelPerder != null) panelPerder.SetActive(false);

        // Preferir el PanelVictoria de la escena (jerarquia) sobre el persistente.
        var pvEscena = BuscarPanelVictoriaEnEscena();
        if (pvEscena != null)
        {
            if (panelVictoria != null && panelVictoria != pvEscena)
                panelVictoria.SetActive(false);
            panelVictoria = pvEscena;
        }
        if (panelVictoria != null)
        {
            ActivarAncestros(panelVictoria);
            panelVictoria.SetActive(true);
        }
    }

    // Botones de los paneles delegan al Temporizador (Reintentar/Salir).
    void OnReintentar()
    {
        var t = FindFirstObjectByType<Temporizador>();
        if (t != null) t.Reintentar();
    }

    void OnSalir()
    {
        var t = FindFirstObjectByType<Temporizador>();
        if (t != null) t.Salir();
    }
}