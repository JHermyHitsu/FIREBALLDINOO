using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Lo que se ve en pantalla mientras se juega: contador de amigos, barra de boost
/// y segmentos. No sabe nada del reloj ni de los paneles de fin de partida.
///
/// Lee el estado de Movimiento, no lo modifica.
/// Si la escena no trae el HUD serializado, se construye a si mismo.
///
/// IMPORTANTE: los segmentos cuelgan SIEMPRE de un contenedor propio
/// ("SegmentosBoost"). Si colgaran directo del canvas, apagar el contenedor
/// apagaria el canvas entero y con el todo el HUD.
/// </summary>
public class HUD : MonoBehaviour
{
    [Header("Contador de amigos")]
    public TextMeshProUGUI textoContadorAmigos;

    [Header("Boost")]
    public Image barraBoost;
    public Image[] segmentosBoost;

    // Estado interno de las animaciones
    int ultimoCountAmigos = 0;
    float progresoPopAmigos = 1f;

    void Awake()
    {
        AsegurarUI();
    }

    void Update()
    {
        ActualizarContadorAmigos();
        ActualizarBarraBoost();
        ActualizarSegmentosBoost();
    }

    // --- Construccion de respaldo ------------------------------------------

    // Si la escena no trae los objetos, se construyen aqui.
    void AsegurarUI()
    {
        Transform canvasT = ResolverCanvas();

        if (textoContadorAmigos == null)
            ConstruirContadorAmigos(canvasT);

        if (segmentosBoost == null || segmentosBoost.Length == 0)
            ConstruirSegmentosBoost(canvasT);

        ApagarSegmentos();
    }

    // Prioridad: canvas de una referencia ya asignada -> canvas existente bajo
    // este GameObject (el del Temporizador) -> canvas nuevo propio.
    Transform ResolverCanvas()
    {
        if (textoContadorAmigos != null)
            return textoContadorAmigos.transform.parent;

        if (segmentosBoost != null && segmentosBoost.Length > 0 && segmentosBoost[0] != null)
            return segmentosBoost[0].transform.parent;

        var existente = GetComponentInChildren<Canvas>(true);
        if (existente != null) return existente.transform;

        var canvasGO = new GameObject("CanvasHUD");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<GraphicRaycaster>();
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        return canvasGO.transform;
    }

    void ConstruirContadorAmigos(Transform canvasT)
    {
        if (canvasT == null) return;

        // Si la escena ya tiene un "ContadorAmigos", se reusa en vez de duplicarlo.
        var previo = canvasT.Find("ContadorAmigos");
        if (previo != null)
        {
            var img = previo.GetComponent<TextMeshProUGUI>();
            if (img != null) { textoContadorAmigos = img; return; }
        }

        var contadorGO = new GameObject("ContadorAmigos");
        contadorGO.transform.SetParent(canvasT, false);
        var rt = contadorGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -40f);
        rt.sizeDelta = new Vector2(120f, 50f);

        textoContadorAmigos = contadorGO.AddComponent<TextMeshProUGUI>();
        textoContadorAmigos.alignment = TextAlignmentOptions.Left;
        textoContadorAmigos.fontSize = 36;
        textoContadorAmigos.color = Color.white;
        textoContadorAmigos.text = "0";
    }

    void ConstruirSegmentosBoost(Transform canvasT)
    {
        if (canvasT == null) return;

        // Contenedor propio: los segmentos NUNCA cuelgan directo del canvas.
        var contGO = canvasT.Find("SegmentosBoost");
        if (contGO == null)
        {
            var nuevo = new GameObject("SegmentosBoost");
            nuevo.transform.SetParent(canvasT, false);
            var rtCont = nuevo.AddComponent<RectTransform>();
            rtCont.anchorMin = new Vector2(0f, 0f);
            rtCont.anchorMax = new Vector2(0f, 0f);
            rtCont.anchoredPosition = new Vector2(20f, 10f);
            rtCont.sizeDelta = new Vector2(100f, 20f);
            contGO = nuevo.transform;
        }

        segmentosBoost = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            // Reusar el segmento que ya exista en la escena.
            var previo = contGO.Find("BoostSeg" + (i + 1));
            var imgPrevio = previo != null ? previo.GetComponent<Image>() : null;
            if (imgPrevio != null) { segmentosBoost[i] = imgPrevio; continue; }

            var segGO = new GameObject("BoostSeg" + (i + 1));
            segGO.transform.SetParent(contGO, false);
            var rt = segGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(i * 30f, 0f);
            rt.sizeDelta = new Vector2(10f, 20f);
            rt.pivot = new Vector2(0f, 0f);

            var img = segGO.AddComponent<Image>();
            img.color = Color.white;
            img.type = Image.Type.Simple;
            segmentosBoost[i] = img;
        }
    }

    // Apaga los segmentos. Guarda contra el caso en que esten colgados del canvas:
    // apagar el padre en ese caso apagaria el HUD entero (bug B1).
    void ApagarSegmentos()
    {
        if (segmentosBoost == null) return;

        foreach (var s in segmentosBoost)
            if (s != null) s.gameObject.SetActive(false);

        if (segmentosBoost.Length == 0 || segmentosBoost[0] == null) return;
        Transform padre = segmentosBoost[0].transform.parent;
        if (padre == null) return;
        if (padre.GetComponent<Canvas>() != null) return; // el padre ES el canvas: no lo apagues
        padre.gameObject.SetActive(false);
    }

    // --- Actualizacion por frame -------------------------------------------

    // Muestra cuantos dinos te siguen. El pop escala cuando el numero cambia.
    void ActualizarContadorAmigos()
    {
        if (textoContadorAmigos == null) return;

        var mov = FindFirstObjectByType<Movimiento>();
        int count = (mov != null && mov.seguidores != null) ? mov.seguidores.Count : 0;

        if (count != ultimoCountAmigos)
        {
            ultimoCountAmigos = count;
            progresoPopAmigos = 0f;
        }
        progresoPopAmigos = Mathf.Min(1f, progresoPopAmigos + Time.deltaTime / 0.2f);

        if (progresoPopAmigos < 1f)
        {
            float popScale;
            if (progresoPopAmigos < 0.5f)
                popScale = Mathf.Lerp(1f, 1.3f, progresoPopAmigos / 0.5f);
            else
                popScale = Mathf.Lerp(1.3f, 1f, (progresoPopAmigos - 0.5f) / 0.5f);
            textoContadorAmigos.rectTransform.localScale = Vector3.one * popScale;
        }
        textoContadorAmigos.text = $"{count}";
    }

    // La barra se vacia sola mientras el boost sigue activo.
    void ActualizarBarraBoost()
    {
        if (barraBoost == null) return;

        var mov = FindFirstObjectByType<Movimiento>();
        if (mov != null && mov.BoostActivo)
        {
            barraBoost.gameObject.SetActive(true);
            float dur = mov.BoostDuracion > 0f ? mov.BoostDuracion : 1f;
            barraBoost.fillAmount = Mathf.Clamp01(mov.BoostTiempoRestante / dur);
        }
        else
        {
            barraBoost.gameObject.SetActive(false);
        }
    }

    // Cuantos segmentos se encienden = cuanto boost queda, de 3 en 3.
    void ActualizarSegmentosBoost()
    {
        if (segmentosBoost == null || segmentosBoost.Length == 0) return;

        var mov = FindFirstObjectByType<Movimiento>();
        float fraccion = 0f;
        int total = segmentosBoost.Length;

        if (mov != null && mov.BoostActivo)
        {
            float dur = mov.BoostDuracion > 0f ? mov.BoostDuracion : 1f;
            fraccion = Mathf.Clamp01(mov.BoostTiempoRestante / dur);
        }

        int activos = Mathf.CeilToInt(fraccion * total);
        if (activos > total) activos = total;

        for (int i = 0; i < total; i++)
            if (segmentosBoost[i] != null)
                segmentosBoost[i].gameObject.SetActive(i < activos);

        // El contenedor se enciende solo si queda algun segmento.
        if (segmentosBoost[0] != null)
        {
            Transform padre = segmentosBoost[0].transform.parent;
            if (padre != null && padre.GetComponent<Canvas>() == null)
                padre.gameObject.SetActive(activos > 0);
        }
    }

    // --- API para el Temporizador ------------------------------------------

    // Lo llaman MostrarGameOver y MostrarVictoria para apagar el HUD.
    public void Apagar()
    {
        if (textoContadorAmigos != null) textoContadorAmigos.gameObject.SetActive(false);
        if (barraBoost != null) barraBoost.gameObject.SetActive(false);
        ApagarSegmentos();
    }

    // Lo llama Reintentar para restaurar el HUD.
    // Los segmentos quedan apagados: se encienden con el boost.
    public void Encender()
    {
        if (textoContadorAmigos != null) textoContadorAmigos.gameObject.SetActive(true);
        if (barraBoost != null) barraBoost.gameObject.SetActive(true);
    }

    // Lo llama Reintentar para dejar las animaciones en su estado inicial.
    public void Reiniciar()
    {
        ultimoCountAmigos = 0;
        progresoPopAmigos = 1f;
        ApagarSegmentos();
        Encender();
    }
}