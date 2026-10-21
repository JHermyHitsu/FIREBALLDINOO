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

    [Header("Audio")]
    public AudioClip musicaPerder;
    public AudioClip musicaVictoria;

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
        ResolverPanelPerder();
        ResolverPanelGameOver();
        AsegurarHoverBotones(panelGameOver);
        AsegurarHoverBotones(panelPerder);
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        // Si se vuelve a una escena sin haber terminado, ningún panel debe quedar visible.
        if (!terminado)
        {
            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
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
        if (mov != null)
        {
            mov.enabled = false;
            mov.ResetearBoost();
        }

        // Apagar HUD y pausar reloj (componentes hermanos o en escena).
        var hud = GetComponent<HUD>() ?? FindFirstObjectByType<HUD>();
        if (hud != null) hud.Apagar();
        var reloj = GetComponent<Reloj>() ?? FindFirstObjectByType<Reloj>();
        if (reloj != null) { reloj.Apagar(); reloj.Pausar(); }

        var camGO = FindFirstObjectByType<Camara>();
        if (camGO != null) camGO.ActivarTemblor(false, 0f);

        DetenerMusicaAmbiente();

        var panelP = ResolverPanelPerder();
        var panelGO = ResolverPanelGameOver();

        if (todosCaidos && panelP != null)
        {
            if (panelGO != null) panelGO.SetActive(false);
            ActivarAncestros(panelP);
            AsegurarHoverBotones(panelP);
            panelP.SetActive(true);
            ReproducirMusica(musicaPerder);
        }
        else if (panelGO != null)
        {
            if (panelP != null) panelP.SetActive(false);
            ActivarAncestros(panelGO);
            AsegurarHoverBotones(panelGO);
            panelGO.SetActive(true);
            ReproducirMusica(musicaPerder);
        }
    }

    public void MostrarGameOverConRetardo(bool todosCaidos, float retardo)
    {
        StartCoroutine(MostrarGameOverRutina(todosCaidos, retardo));
    }

    System.Collections.IEnumerator MostrarGameOverRutina(bool todosCaidos, float retardo)
    {
        yield return new WaitForSeconds(retardo);
        MostrarGameOver(todosCaidos);
    }

    GameObject ResolverPanelPerder()
    {
        if (panelPerder != null) return panelPerder;

        var encontrado = BuscarHijoRecursivo(gameObject, "PanelPerder");
        if (encontrado != null) { panelPerder = encontrado; return panelPerder; }

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            encontrado = BuscarHijoRecursivo(canvas.gameObject, "PanelPerder");
            if (encontrado != null) { panelPerder = encontrado; return panelPerder; }
        }

        return null;
    }

    GameObject ResolverPanelGameOver()
    {
        if (panelGameOver != null) return panelGameOver;

        var encontrado = BuscarHijoRecursivo(gameObject, NombrePanelGameOver);
        if (encontrado == null) encontrado = BuscarHijoRecursivo(gameObject, "PanelGamerOver");
        if (encontrado != null) { panelGameOver = encontrado; return panelGameOver; }

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            encontrado = BuscarHijoRecursivo(canvas.gameObject, NombrePanelGameOver);
            if (encontrado == null) encontrado = BuscarHijoRecursivo(canvas.gameObject, "PanelGamerOver");
            if (encontrado != null) { panelGameOver = encontrado; return panelGameOver; }
        }

        return null;
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

        DetenerMusicaAmbiente();
        ReproducirMusica(musicaVictoria);

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

    void DetenerMusicaAmbiente()
    {
        AudioSource[] fuentes = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (var a in fuentes)
        {
            if (a != null && a != audioSource && a.isPlaying)
            {
                a.Stop();
            }
        }

        // Desactivar items de comida que queden en escena para que no se superpongan al panel.
        foreach (var item in FindObjectsByType<ItemComida>(FindObjectsSortMode.None))
        {
            if (item != null) item.gameObject.SetActive(false);
        }
    }

    void ReproducirMusica(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.clip = clip;
            audioSource.loop = false;
            audioSource.Play();
        }
    }

    // Botones de los paneles delegan al Temporizador (Reintentar/Salir).
    void OnReintentar()
    {
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        var t = FindFirstObjectByType<Temporizador>();
        if (t != null) t.Reintentar();
    }

    void OnSalir()
    {
        var t = FindFirstObjectByType<Temporizador>();
        if (t != null) t.Salir();
    }
}