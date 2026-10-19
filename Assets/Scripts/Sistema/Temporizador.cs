using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Reloj global + pantalla de Game Over.
// El GameObject es persistente (DontDestroyOnLoad) junto con su Canvas hijo,
// por eso el panel funciona en Bosque, Desierto y Cueva.
// "Reintentar" vuelve siempre a ESCENA_BIOMA_BOSQUE con el tiempo reseteado.
public class Temporizador : MonoBehaviour
{
    public static Temporizador Instance;
    public const string EscenaBosque = "ESCENA_BIOMA_BOSQUE";
    // Nombre canónico del panel de Game Over. En Bosque la escena lo llama
    // "PanelGamerOver" (con r); se acepta como alias para no romper.
    public const string NombrePanelGameOver = "PanelGameOver";

    [Header("Configuración")]
    public GameObject panelGameOver; // arrastra PanelGameOver aquí
    public GameObject panelPerder;   // arrastra PanelPerder aquí (todos caídos)

    // Referencias a los componentes hermanos (viven en el mismo GameManager).
    // No serializados: se resuelven en runtime en Awake.
    HUD hud;
    Reloj reloj;

    public TextMeshProUGUI textoMensaje; // arrastra Texto_TiempoAgotado aquí
    public GameObject imagenTiempoAgotado; // solo se muestra al agotarse el tiempo
    [Header("Sonido de Perder")]
    [Tooltip("Sonido o música de Game Over (al caerse o acabarse el tiempo).")]
    public AudioClip sonidoPerder;
    private AudioSource audioSource;

    [Header("Victoria (se crea solo si falta)")]
    public GameObject panelVictoria;
    public TextMeshProUGUI textoVictoria;
    bool terminado = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        AsegurarUI();

        // El HUD y el Reloj viven en el mismo GameManager: se resuelven (o se
        // agregan) ahora, despues de AsegurarUI() para que el canvas persistente
        // ya exista.
        hud = GetComponent<HUD>();
        if (hud == null) hud = gameObject.AddComponent<HUD>();

        reloj = GetComponent<Reloj>();
        if (reloj == null) reloj = gameObject.AddComponent<Reloj>();

        SceneManager.sceneLoaded += AlCargarEscena;
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= AlCargarEscena;
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        // Si se vuelve a una escena sin haber terminado, ningún panel debe quedar visible.
        if (!terminado)
        {
            if (panelGameOver != null) panelGameOver.SetActive(false);
            if (panelPerder != null) panelPerder.SetActive(false);
            if (panelVictoria != null) panelVictoria.SetActive(false);
            if (imagenTiempoAgotado != null) imagenTiempoAgotado.SetActive(false);
        }

        // Reasignar PanelVictoria en la escena nueva (prioridad sobre el persistente)
        GameObject pvEscena = BuscarPanelVictoriaEnEscena();
        if (pvEscena != null && pvEscena != panelVictoria)
        {
            if (panelVictoria != null) panelVictoria.SetActive(false);
            panelVictoria = pvEscena;
            Transform t = panelVictoria.transform.Find("TextoVictoria");
            if (t == null) t = panelVictoria.transform.Find("Texto_Victoria");
            if (t != null) textoVictoria = t.GetComponent<TMPro.TextMeshProUGUI>();
        }

        // Reasignar PanelGameOver y PanelPerder de la escena nueva.
        // El Temporizador es persistente (DontDestroyOnLoad): los paneles de la
        // escena anterior se destruyeron, así que hay que tomar los de la nueva.
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

        // Reconectar los botones Reintentar/Salir de los paneles de la escena nueva
        // al Temporizador persistente (el de la escena se destruyó).
        ReconectarBotonesPaneles();

    }

    // Reconecta los botones de los paneles de Game Over / Perder al Temporizador persistente.
    void ReconectarBotonesPaneles()
    {
        ReconectarBotonEnPanel(panelGameOver, "BotonReintentar", Reintentar);
        ReconectarBotonEnPanel(panelGameOver, "BotonSalir", Salir);
        ReconectarBotonEnPanel(panelPerder, "BotonReintentar", Reintentar);
        ReconectarBotonEnPanel(panelPerder, "BotonSalir", Salir);
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

    // Devuelve el Temporizador persistente; lo crea (con UI mínima por código)
    // si la escena actual no trae uno (p. ej. Play directo en Desierto/Cueva).
    public static Temporizador EnsureExists()
    {
        var existente = FindFirstObjectByType<Temporizador>();
        if (existente != null)
        {
            existente.AsegurarUI();
            return existente;
        }
        var go = new GameObject("GameManager");
        return go.AddComponent<Temporizador>(); // Awake() construye la UI de respaldo
    }

    // Si las referencias del inspector se perdieron, construye una UI persistente por código.
    // Busca un hijo por nombre recursivamente en la jerarquia
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

    // Busca el PanelVictoria de la escena activa, incluyendo GameObjects inactivos.
    // GameObject.Find() NO encuentra objetos inactivos, por eso se recorre la jerarquia.
    GameObject BuscarPanelVictoriaEnEscena()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var pv = BuscarHijoRecursivo(root, "PanelVictoria");
            if (pv != null) return pv;
        }
        return null;
    }

    // Activa todos los ancestros (Canvas, etc.) para que un panel pueda renderizarse.
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

    public void AsegurarUI()
    {
        if (panelGameOver == null)
            ConstruirUIPersistente();
        if (panelVictoria == null)
        {
            var pv = BuscarPanelVictoriaEnEscena();
            if (pv != null) panelVictoria = pv;
        }
        AsegurarPanelVictoria();
        AsegurarHoverBotones(panelPerder);
        AsegurarHoverBotones(panelGameOver);
    }

    void ConstruirUIPersistente()
    {
        Transform viejo = transform.Find("CanvasPersistente");
        if (viejo != null) Destroy(viejo.gameObject);

        var canvasGO = new GameObject("CanvasPersistente");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<GraphicRaycaster>();
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);

        // Panel de Game Over (pantalla completa, inicia oculto)
        panelGameOver = new GameObject("PanelGameOver");
        panelGameOver.transform.SetParent(canvasGO.transform, false);
        var fondo = panelGameOver.AddComponent<Image>();
        fondo.color = new Color(0f, 0f, 0f, 0.6f);
        var rtPanel = panelGameOver.GetComponent<RectTransform>();
        rtPanel.anchorMin = Vector2.zero;
        rtPanel.anchorMax = Vector2.one;
        rtPanel.offsetMin = Vector2.zero;
        rtPanel.offsetMax = Vector2.zero;

        var msgGO = new GameObject("Texto_TiempoAgotado");
        msgGO.transform.SetParent(panelGameOver.transform, false);
        textoMensaje = msgGO.AddComponent<TextMeshProUGUI>();
        textoMensaje.alignment = TextAlignmentOptions.Center;
        textoMensaje.fontSize = 48;
        var rtMsg = msgGO.GetComponent<RectTransform>();
        rtMsg.anchorMin = new Vector2(0.5f, 0.5f);
        rtMsg.anchorMax = new Vector2(0.5f, 0.5f);
        rtMsg.anchoredPosition = new Vector2(0f, 60f);
        rtMsg.sizeDelta = new Vector2(600f, 80f);

        var btnGO = new GameObject("BotonReintentar");
        btnGO.transform.SetParent(panelGameOver.transform, false);
        var img = btnGO.AddComponent<Image>();
        img.color = Color.white;
        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(Reintentar);
        var rtBtn = btnGO.GetComponent<RectTransform>();
        rtBtn.anchorMin = new Vector2(0.5f, 0.5f);
        rtBtn.anchorMax = new Vector2(0.5f, 0.5f);
        rtBtn.anchoredPosition = new Vector2(0f, -60f);
        rtBtn.sizeDelta = new Vector2(220f, 60f);

        var lblGO = new GameObject("Texto_Boton");
        lblGO.transform.SetParent(btnGO.transform, false);
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.fontSize = 28;
        lbl.color = Color.black;
        lbl.text = "REINTENTAR";
        var rtLbl = lblGO.GetComponent<RectTransform>();
        rtLbl.anchorMin = Vector2.zero;
        rtLbl.anchorMax = Vector2.one;
        rtLbl.offsetMin = Vector2.zero;
        rtLbl.offsetMax = Vector2.zero;

        panelGameOver.SetActive(false);

        ConstruirPanelVictoria(canvasGO.transform);
    }

    Transform ObtenerCanvas()
    {
        if (panelGameOver != null) return panelGameOver.transform.parent;
        Transform c = transform.Find("CanvasPersistente");
        return c;
    }

    // El panel de victoria no existe en las escenas: siempre se crea por código
    // bajo el Canvas persistente, para no depender de prefabs por bioma.
    void AsegurarPanelVictoria()
    {
        if (panelVictoria != null && textoVictoria != null) return;
        // Si el panel se encontró en la escena, no destruir ni recrear
        if (panelVictoria != null)
        {
            if (textoVictoria == null)
            {
                Transform t = panelVictoria.transform.Find("TextoVictoria");
                if (t == null) t = panelVictoria.transform.Find("Texto_Victoria");
                if (t != null) textoVictoria = t.GetComponent<TMPro.TextMeshProUGUI>();
            }
            return;
        }
        Transform canvasT = ObtenerCanvas();
        if (canvasT == null) return; // sin canvas no hay dónde construir
        if (panelVictoria != null) Destroy(panelVictoria);
        ConstruirPanelVictoria(canvasT);
    }

    // Asegura que los botones del panel tengan el efecto hover simple.
    void AsegurarHoverBotones(GameObject panel)
    {
        if (panel == null) return;
        foreach (var btn in panel.GetComponentsInChildren<Button>(true))
        {
            if (btn.GetComponent<HoverBoton>() == null)
                btn.gameObject.AddComponent<HoverBoton>();
        }
    }

    void ConstruirPanelVictoria(Transform canvasT)
    {
        panelVictoria = new GameObject("PanelVictoria");
        panelVictoria.transform.SetParent(canvasT, false);
        var fondo = panelVictoria.AddComponent<Image>();
        fondo.color = new Color(0.05f, 0.2f, 0.05f, 0.75f);
        var rtPanel = panelVictoria.GetComponent<RectTransform>();
        rtPanel.anchorMin = Vector2.zero;
        rtPanel.anchorMax = Vector2.one;
        rtPanel.offsetMin = Vector2.zero;
        rtPanel.offsetMax = Vector2.zero;

        var msgGO = new GameObject("TextoVictoria");
        msgGO.transform.SetParent(panelVictoria.transform, false);
        textoVictoria = msgGO.AddComponent<TextMeshProUGUI>();
        textoVictoria.alignment = TextAlignmentOptions.Center;
        textoVictoria.fontSize = 40;
        textoVictoria.text = "¡REFUGIO ALCANZADO!";
        var rtMsg = msgGO.GetComponent<RectTransform>();
        rtMsg.anchorMin = new Vector2(0.5f, 0.5f);
        rtMsg.anchorMax = new Vector2(0.5f, 0.5f);
        rtMsg.anchoredPosition = new Vector2(0f, 60f);
        rtMsg.sizeDelta = new Vector2(640f, 160f);

        var btnGO = new GameObject("BotonJugarDeNuevo");
        btnGO.transform.SetParent(panelVictoria.transform, false);
        var img = btnGO.AddComponent<Image>();
        img.color = Color.white;
        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(Reintentar);
        var rtBtn = btnGO.GetComponent<RectTransform>();
        rtBtn.anchorMin = new Vector2(0.5f, 0.5f);
        rtBtn.anchorMax = new Vector2(0.5f, 0.5f);
        rtBtn.anchoredPosition = new Vector2(0f, -100f);
        rtBtn.sizeDelta = new Vector2(260f, 60f);

        var lblGO = new GameObject("Texto_Boton");
        lblGO.transform.SetParent(btnGO.transform, false);
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.fontSize = 28;
        lbl.color = Color.black;
        lbl.text = "JUGAR DE NUEVO";
        var rtLbl = lblGO.GetComponent<RectTransform>();
        rtLbl.anchorMin = Vector2.zero;
        rtLbl.anchorMax = Vector2.one;
        rtLbl.offsetMin = Vector2.zero;
        rtLbl.offsetMax = Vector2.zero;

        panelVictoria.SetActive(false);
    }

    void Update()
    {
        if (terminado) return;

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
        terminado = true;
        var mov = FindFirstObjectByType<Movimiento>();
        if (mov != null) mov.enabled = false; // frena al dino, ya no es Morir()
        if (reloj != null) { reloj.Apagar(); reloj.Pausar(); }
        if (hud != null) hud.Apagar();
        if (panelVictoria != null) panelVictoria.SetActive(false);
        var camGO = FindFirstObjectByType<Camara>();
        if (camGO != null) camGO.ActivarTemblor(false, 0f);

        var movMuerte = FindFirstObjectByType<Movimiento>();
        if (movMuerte != null) movMuerte.ResetearBoost();

        // Activar el panel correspondiente según la causa
        if (todosCaidos && panelPerder != null)
        {
            if (panelGameOver != null) panelGameOver.SetActive(false);
            AsegurarHoverBotones(panelPerder);
            panelPerder.SetActive(true);
            if (imagenTiempoAgotado != null) imagenTiempoAgotado.SetActive(false);
        }
        else if (panelGameOver != null)
        {
            if (panelPerder != null) panelPerder.SetActive(false);
            AsegurarHoverBotones(panelGameOver);
            panelGameOver.SetActive(true);
            if (imagenTiempoAgotado != null) imagenTiempoAgotado.SetActive(true);
        }
    }

    // Victoria al llegar a la MetaFinal: congela reloj y dino, muestra stats.
    public void MostrarVictoria()
    {
        if (terminado) return; // si ya murió o se acabó el tiempo, no hay victoria
        AsegurarUI();
        terminado = true;

        var mov = FindFirstObjectByType<Movimiento>();
        if (mov != null) mov.enabled = false;

        if (reloj != null) { reloj.Apagar(); reloj.Pausar(); }
        if (hud != null) hud.Apagar();
        if (mov != null) mov.ResetearBoost();
        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (panelPerder != null) panelPerder.SetActive(false);

        // Preferir el PanelVictoria de la escena (jerarquia) sobre el persistente.
        // Activar tambien sus ancestros (Canvas) porque en la escena suelen estar inactivos.
        var pvEscena = BuscarPanelVictoriaEnEscena();
        if (pvEscena != null)
        {
            if (panelVictoria != null && panelVictoria != pvEscena)
                panelVictoria.SetActive(false);
            panelVictoria = pvEscena;
            Transform t = panelVictoria.transform.Find("TextoVictoria");
            if (t != null) textoVictoria = t.GetComponent<TMPro.TextMeshProUGUI>();
        }
        if (panelVictoria != null)
        {
            ActivarAncestros(panelVictoria);
            panelVictoria.SetActive(true);
        }
    }

    public void Reintentar() // vuelve siempre a Bosque con el tiempo reseteado
    {
        terminado = false;

        // El reloj y el HUD se restauran siempre.
        if (reloj != null) reloj.Reiniciar();
        if (hud != null) hud.Reiniciar();
        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);
        var camGO = FindFirstObjectByType<Camara>();
        if (camGO != null) camGO.ActivarTemblor(false, 0f);
        if (imagenTiempoAgotado != null) imagenTiempoAgotado.SetActive(false);
        if (audioSource != null) audioSource.Stop();
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
