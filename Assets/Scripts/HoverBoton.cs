using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Efecto simple de hover para botones de UI: escala leve + tinte mas claro.
// Se adjunta automaticamente a los botones del PanelPerder (ver Temporizador).
public class HoverBoton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Escala del boton al pasar el mouse.")]
    public float escalaHover = 1.08f;
    [Tooltip("Velocidad de la animacion (mas alto = mas rapido).")]
    public float velocidad = 12f;
    [Tooltip("Color del boton al pasar el mouse (mas claro/brillante).")]
    public Color colorHover = new Color(1f, 0.92f, 0.6f, 1f);

    Vector3 escalaOriginal;
    Color colorOriginal;
    Image imagen;
    bool hover = false;

    void Awake()
    {
        escalaOriginal = transform.localScale;
        imagen = GetComponent<Image>();
        if (imagen != null) colorOriginal = imagen.color;
    }

    void Update()
    {
        Vector3 escalaObj = hover ? escalaOriginal * escalaHover : escalaOriginal;
        transform.localScale = Vector3.Lerp(transform.localScale, escalaObj, Time.unscaledDeltaTime * velocidad);

        if (imagen != null)
        {
            Color cObj = hover ? colorHover : colorOriginal;
            imagen.color = Color.Lerp(imagen.color, cObj, Time.unscaledDeltaTime * velocidad);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) { hover = true; }
    public void OnPointerExit(PointerEventData eventData) { hover = false; }
}
