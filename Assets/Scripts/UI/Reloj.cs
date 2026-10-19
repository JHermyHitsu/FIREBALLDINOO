using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reloj de la partida: cuenta regresiva en mm:ss, color rojo neón bajo 15s
/// y tick de pánico (escala/rotación/desplazamiento) bajo 8s.
/// No sabe nada de paneles de fin de partida ni del HUD de boost.
/// </summary>
public class Reloj : MonoBehaviour
{
    [Header("Configuración")]
    public float tiempoInicial = 180f;
    public TextMeshProUGUI textoTiempo;

    // Estado interno
    public float tiempoRestante;
    int ultimoSegundoTick = 9;
    float progresoTick = 1f;
    Vector2 posicionBaseTimer = new Vector2(0f, -40f);

    // Cuando es false, el reloj no cuenta (fin de partida).
    public bool activo = true;

    void Awake()
    {
        tiempoRestante = tiempoInicial;
        AsegurarUI();
    }

    void Update()
    {
        if (!activo) return;
        if (tiempoRestante <= 0f)
        {
            tiempoRestante = 0f;
            if (textoTiempo != null) textoTiempo.text = "00:00";
            return;
        }

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
    }

    // --- Construccion de respaldo ------------------------------------------

    void AsegurarUI()
    {
        if (textoTiempo != null) return;

        Transform canvasT = null;
        var existente = GetComponentInChildren<Canvas>(true);
        if (existente != null) canvasT = existente.transform;

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

        var relojGO = new GameObject("TextoTiempo");
        relojGO.transform.SetParent(canvasT, false);
        textoTiempo = relojGO.AddComponent<TextMeshProUGUI>();
        textoTiempo.alignment = TextAlignmentOptions.Center;
        textoTiempo.fontSize = 42;
        var rt = relojGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -40f);
        rt.sizeDelta = new Vector2(200f, 50f);
    }

    // --- API para el Temporizador ------------------------------------------

    public float TiempoRestante => tiempoRestante;

    public void Apagar()
    {
        if (textoTiempo != null) textoTiempo.gameObject.SetActive(false);
    }

    public void Encender()
    {
        if (textoTiempo != null) textoTiempo.gameObject.SetActive(true);
    }

    public void Pausar()
    {
        activo = false;
    }

    public void Reanudar()
    {
        activo = true;
    }

    public void Reiniciar()
    {
        tiempoRestante = tiempoInicial;
        ultimoSegundoTick = 9;
        progresoTick = 1f;
        activo = true;
        Encender();
        if (textoTiempo != null)
        {
            int m = Mathf.FloorToInt(tiempoRestante / 60f);
            int sec = Mathf.FloorToInt(tiempoRestante % 60f);
            textoTiempo.text = $"{m:00}:{sec:00}";
            textoTiempo.rectTransform.localScale = Vector3.one;
            textoTiempo.rectTransform.localEulerAngles = Vector3.zero;
        }
    }
}