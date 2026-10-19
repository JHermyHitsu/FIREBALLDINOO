using UnityEngine;
using TMPro;

public class ItemComida : MonoBehaviour
{
    [Header("Efecto de velocidad")]
    public float multiplicador = 1.5f;
    public float duracion = 4f;

    [Header("Sonido")]
    public AudioClip sonidoComer;

    [Header("Hint (tutorial)")]
    [Tooltip("Texto que se muestra cuando aparece la comida.")]
    public string textoHint = "¡Come para correr más rápido!";
    [Tooltip("Segundos que dura el hint en pantalla.")]
    public float duracionHint = 3f;
    [Tooltip("Cantidad máxima de veces que se muestra el hint.")]
    public int maxVecesHint = 2;
    [Tooltip("Altura sobre la comida donde aparece el texto.")]
    public float alturaHint = 1.5f;

    private bool yaFueComido = false;
    private bool hintYaMostradoParaEste = false;

    // Contador global (persiste entre escenas dentro de la sesión).
    private static int vecesHintMostrado = 0;
    private static bool hintEnPantalla = false;

    private void OnBecameVisible()
    {
        MostrarHint();
    }

    private void MostrarHint()
    {
        if (hintYaMostradoParaEste) return;
        if (vecesHintMostrado >= maxVecesHint) return;
        if (hintEnPantalla) return;

        hintYaMostradoParaEste = true;
        vecesHintMostrado++;
        hintEnPantalla = true;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null || Camera.main == null)
        {
            hintEnPantalla = false;
            return;
        }

        GameObject go = new GameObject("HintComida");
        go.transform.SetParent(canvas.transform, false);

        TextMeshProUGUI txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = textoHint;
        txt.fontSize = 24;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        txt.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 40);

        Vector3 worldTarget = transform.position + Vector3.up * alturaHint;
        StartCoroutine(SeguirYDestruir(go, rt, canvas, worldTarget, duracionHint));
    }

    private System.Collections.IEnumerator SeguirYDestruir(GameObject go, RectTransform rt, Canvas canvas, Vector3 worldTarget, float t)
    {
        float elapsed = 0f;
        while (elapsed < t && go != null)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldTarget);
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Vector2 localPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main, out localPos))
            {
                rt.anchoredPosition = localPos;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (go != null) Destroy(go);
        hintEnPantalla = false;
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        // Si ya fue recogido o no es el jugador con tag "Player", ignoramos
        if (yaFueComido || !otro.CompareTag("Player")) return;

        Movimiento dino = otro.GetComponent<Movimiento>();
        if (dino != null)
        {
            yaFueComido = true;

            // 1. Activar el aumento de velocidad temporal en el jugador
            dino.ActivarBoost(multiplicador, duracion);

            // 2. Reproducir el efecto de sonido
            if (sonidoComer != null)
            {
                Vector3 posAudio = Camera.main != null ? Camera.main.transform.position : transform.position;
                AudioSource.PlayClipAtPoint(sonidoComer, posAudio);
            }

            // 3. Eliminar este muslo de la escena
            Destroy(gameObject);
        }
    }
}
