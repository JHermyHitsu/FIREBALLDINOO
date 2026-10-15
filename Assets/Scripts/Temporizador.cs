using UnityEngine;
using TMPro;

public class Temporizador : MonoBehaviour
{
    public static Temporizador Instance;
    public float tiempoInicial = 180f;
    public float tiempoRestante;
    public TextMeshProUGUI textoTiempo;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject); // igual que TransicionManager.cs:38
        tiempoRestante = tiempoInicial;
    }
    void Update()
    {
        if (tiempoRestante <= 0) return;
        tiempoRestante -= Time.deltaTime;
        if (textoTiempo != null)
        {
            int m = Mathf.FloorToInt(tiempoRestante / 60f);
            int s = Mathf.FloorToInt(tiempoRestante % 60f);
            textoTiempo.text = $"{m:00}:{s:00}";
        }
        if (tiempoRestante <= 0)
        {
            tiempoRestante = 0;
            FindFirstObjectByType<Movimiento>()?.Morir(); // reutiliza Movimiento.cs:102
        }
    }
    public void AgregarTiempo(float seg) { tiempoRestante += seg; }
}