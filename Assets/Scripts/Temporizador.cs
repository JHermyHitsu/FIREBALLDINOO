using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class Temporizador : MonoBehaviour
{
    public static Temporizador Instance;
    public float tiempoInicial = 180f;
    public float tiempoRestante;
    public TextMeshProUGUI textoTiempo;
    public GameObject panelGameOver; // arrastra PanelGameOver aquí
    bool terminado = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        tiempoRestante = tiempoInicial;
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
        }
        if (tiempoRestante <= 0)
        {
            tiempoRestante = 0;
            if (textoTiempo != null) textoTiempo.text = "00:00";
            MostrarGameOver();
        }
    }
    void MostrarGameOver()
    {
        terminado = true;
        var mov = FindFirstObjectByType<Movimiento>();
        if (mov != null) mov.enabled = false; // frena al dino, ya no es Morir()
        if (textoTiempo != null) textoTiempo.gameObject.SetActive(false);
        if (panelGameOver != null) panelGameOver.SetActive(true);
    }
    public void AgregarTiempo(float seg) { if(!terminado) tiempoRestante += seg; }

    public void Reintentar() // opción A: todo desde Bosque
    {
        terminado = false;
        tiempoRestante = tiempoInicial;
        if (textoTiempo != null)
        {
            textoTiempo.gameObject.SetActive(true);
            int m = Mathf.FloorToInt(tiempoRestante / 60f);
            int s = Mathf.FloorToInt(tiempoRestante % 60f);
            textoTiempo.text = $"{m:00}:{s:00}";
        }
        if (panelGameOver != null) panelGameOver.SetActive(false);
        SceneManager.LoadScene("ESCENA_BIOMA_BOSQUE");
    }
}