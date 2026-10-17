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

    [Header("Configuración")]
    public float tiempoInicial = 180f;
    public float tiempoRestante;
    public TextMeshProUGUI textoTiempo;
    public TextMeshProUGUI textoContadorAmigos; // contador de amigos (arriba a la izquierda)
    public GameObject panelGameOver; // arrastra PanelGameOver aquí

    // Estado del contador de amigos
    int ultimoCountAmigos = 0;
    float progresoPopAmigos = 1f;
    public TextMeshProUGUI textoMensaje; // arrastra Texto_TiempoAgotado aquí
    [Header("Victoria (se crea solo si falta)")]
    public GameObject panelVictoria;
    public TextMeshProUGUI textoVictoria;
    bool terminado = false;
    int ultimoSegundoTick = 9;
    float progresoTick = 1f;
    Vector2 posicionBaseTimer = new Vector2(0f, -40f);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        tiempoRestante = tiempoInicial;
        AsegurarUI();
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
            if (panelVictoria != null) panelVictoria.SetActive(false);
        }
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
    public void AsegurarUI()
    {
        if (panelGameOver == null || textoMensaje == null || textoTiempo == null)
            ConstruirUIPersistente();
        AsegurarContadorAmigos();
        AsegurarPanelVictoria();
    }

    // Fallback por código: crea el contador si la escena no lo trae.
    void AsegurarContadorAmigos()
    {
        if (textoContadorAmigos != null) return;

        Transform canvasT = null;
        if (textoTiempo != null) canvasT = textoTiempo.transform.parent;
        if (canvasT == null) canvasT = transform.Find("CanvasPersistente");

        if (canvasT == null)
        {
            var canvasGO = new GameObject("CanvasPersistente");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGO.AddComponent<GraphicRaycaster>();
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 600f);
            canvasT = canvasGO.transform;
        }

        var contadorGO = new GameObject("ContadorAmigos");
        contadorGO.transform.SetParent(canvasT, false);
        var rtContador = contadorGO.AddComponent<RectTransform>();
        rtContador.anchorMin = new Vector2(0f, 1f);
        rtContador.anchorMax = new Vector2(0f, 1f);
        rtContador.anchoredPosition = new Vector2(20f, -40f);
        rtContador.sizeDelta = new Vector2(120f, 50f);

        textoContadorAmigos = contadorGO.AddComponent<TextMeshProUGUI>();
        textoContadorAmigos.alignment = TextAlignmentOptions.Left;
        textoContadorAmigos.fontSize = 36;
        textoContadorAmigos.color = Color.white;
        textoContadorAmigos.text = "0";
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

        // Reloj HUD (arriba al centro)
        var relojGO = new GameObject("TextoTiempo");
        relojGO.transform.SetParent(canvasGO.transform, false);
        textoTiempo = relojGO.AddComponent<TextMeshProUGUI>();
        textoTiempo.alignment = TextAlignmentOptions.Center;
        textoTiempo.fontSize = 42;
        var rtReloj = relojGO.GetComponent<RectTransform>();
        rtReloj.anchorMin = new Vector2(0.5f, 1f);
        rtReloj.anchorMax = new Vector2(0.5f, 1f);
        rtReloj.anchoredPosition = new Vector2(0f, -40f);
        rtReloj.sizeDelta = new Vector2(200f, 50f);

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
        textoMensaje.text = "TIEMPO AGOTADO";
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
        Transform canvasT = ObtenerCanvas();
        if (canvasT == null) return; // sin canvas no hay dónde construir
        if (panelVictoria != null) Destroy(panelVictoria);
        ConstruirPanelVictoria(canvasT);
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

        var msgGO = new GameObject("Texto_Victoria");
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
        if (tiempoRestante <= 0) return;
        tiempoRestante -= Time.deltaTime;
        if (textoTiempo != null)
        {
            int m = Mathf.FloorToInt(tiempoRestante / 60f);
            int s = Mathf.FloorToInt(tiempoRestante % 60f);
            textoTiempo.text = $"{m:00}:{s:00}";

            // Color rojo neón a partir del segundo 15
            if (tiempoRestante <= 15f)
                textoTiempo.color = new Color(1f, 0.165f, 0.294f); // #ff2a4b
            else
                textoTiempo.color = Color.white;

            // Tick de pánico cada segundo a partir del segundo 8 (scale + rotación + desplazamiento)
            if (tiempoRestante <= 8f && tiempoRestante > 0f)
            {
                int segundoActual = Mathf.FloorToInt(tiempoRestante);
                if (segundoActual != ultimoSegundoTick)
                {
                    ultimoSegundoTick = segundoActual;
                    progresoTick = 0f;
                    // Capturar la posición base real del timer en la escena
                    posicionBaseTimer = textoTiempo.rectTransform.anchoredPosition;
                }
                progresoTick = Mathf.Min(1f, progresoTick + Time.deltaTime / 0.2f);

                float t = progresoTick;
                float scale, rot, x;
                if (t < 0.3f)
                {
                    float p = t / 0.3f;
                    scale = Mathf.Lerp(1f, 1.12f, p);
                    rot = Mathf.Lerp(0f, -1.5f, p);
                    x = Mathf.Lerp(0f, -3f, p);
                }
                else if (t < 0.6f)
                {
                    float p = (t - 0.3f) / 0.3f;
                    scale = Mathf.Lerp(1.12f, 1.05f, p);
                    rot = Mathf.Lerp(-1.5f, 1f, p);
                    x = Mathf.Lerp(-3f, 2f, p);
                }
                else
                {
                    float p = (t - 0.6f) / 0.4f;
                    scale = Mathf.Lerp(1.05f, 1f, p);
                    rot = Mathf.Lerp(1f, 0f, p);
                    x = Mathf.Lerp(2f, 0f, p);
                }

                textoTiempo.rectTransform.localScale = Vector3.one * scale;
                textoTiempo.rectTransform.localEulerAngles = new Vector3(0f, 0f, rot);
                textoTiempo.rectTransform.anchoredPosition = new Vector2(posicionBaseTimer.x + x, posicionBaseTimer.y);
            }
            else
            {
                textoTiempo.rectTransform.localScale = Vector3.one;
                textoTiempo.rectTransform.localEulerAngles = Vector3.zero;
                // No forzar anchoredPosition: respeta la posición definida en la escena
            }
        }
        // Contador de amigos: se sincroniza con Movimiento.seguidores
        if (textoContadorAmigos != null)
        {
            int count = 0;
            var mov = FindFirstObjectByType<Movimiento>();
            if (mov != null) count = mov.seguidores.Count;

            if (count != ultimoCountAmigos)
            {
                ultimoCountAmigos = count;
                progresoPopAmigos = 0f;
            }
            progresoPopAmigos = Mathf.Min(1f, progresoPopAmigos + Time.deltaTime / 0.2f);

            float popScale = 1f;
            if (progresoPopAmigos < 1f)
            {
                if (progresoPopAmigos < 0.5f)
                    popScale = Mathf.Lerp(1f, 1.3f, progresoPopAmigos / 0.5f);
                else
                    popScale = Mathf.Lerp(1.3f, 1f, (progresoPopAmigos - 0.5f) / 0.5f);
            }
            textoContadorAmigos.rectTransform.localScale = Vector3.one * popScale;
            textoContadorAmigos.text = $"{count}";
        }

        if (tiempoRestante <= 0)
        {
            tiempoRestante = 0;
            if (textoTiempo != null) textoTiempo.text = "00:00";
            MostrarGameOver();
        }
    }

    public void MostrarGameOver(string mensaje = "TIEMPO AGOTADO")
    {
        AsegurarUI();
        terminado = true;
        var mov = FindFirstObjectByType<Movimiento>();
        if (mov != null) mov.enabled = false; // frena al dino, ya no es Morir()
        if (textoTiempo != null) textoTiempo.gameObject.SetActive(false);
        if (textoContadorAmigos != null) textoContadorAmigos.gameObject.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);
        if (textoMensaje != null) textoMensaje.text = mensaje;
        if (panelGameOver != null) panelGameOver.SetActive(true);
    }

    // Victoria al llegar a la MetaFinal: congela reloj y dino, muestra stats.
    public void MostrarVictoria(string mensaje = "¡REFUGIO ALCANZADO!")
    {
        if (terminado) return; // si ya murió o se acabó el tiempo, no hay victoria
        AsegurarUI();
        terminado = true;

        int rescatados = 0;
        var mov = FindFirstObjectByType<Movimiento>();
        if (mov != null)
        {
            mov.enabled = false;
            foreach (var s in mov.seguidores)
                if (s != null) rescatados++;
        }

        float usado = Mathf.Max(0f, tiempoInicial - tiempoRestante);
        int mu = Mathf.FloorToInt(usado / 60f);
        int su = Mathf.FloorToInt(usado % 60f);

        if (textoTiempo != null) textoTiempo.gameObject.SetActive(false);
        if (textoContadorAmigos != null) textoContadorAmigos.gameObject.SetActive(false);
        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (textoVictoria != null)
            textoVictoria.text = $"{mensaje}\nDinos rescatados: {rescatados}\nTiempo: {mu:00}:{su:00}";
        if (panelVictoria != null) panelVictoria.SetActive(true);
    }

    public void AgregarTiempo(float seg) { if(!terminado) tiempoRestante += seg; }

    public void Reintentar() // vuelve siempre a Bosque con el tiempo reseteado
    {
        terminado = false;
        tiempoRestante = tiempoInicial;
        ultimoSegundoTick = 9;
        progresoTick = 1f;
        ultimoCountAmigos = 0;
        progresoPopAmigos = 1f;
        if (textoTiempo != null)
        {
            textoTiempo.gameObject.SetActive(true);
            if (textoContadorAmigos != null) textoContadorAmigos.gameObject.SetActive(true);
            int m = Mathf.FloorToInt(tiempoRestante / 60f);
            int s = Mathf.FloorToInt(tiempoRestante % 60f);
            textoTiempo.text = $"{m:00}:{s:00}";
        }
        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);
        DinoSeguidor.LimpiarHistorial(); // la fila no debe arrastrar posiciones del bioma anterior
        TransicionManager.EnsureExists(); // garantiza el intercambio del jugador al recargar
        SceneManager.LoadScene(EscenaBosque);
    }
}
