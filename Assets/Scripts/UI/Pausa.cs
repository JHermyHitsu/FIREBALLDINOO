using UnityEngine;
using UnityEngine.SceneManagement;

// Menu de pausa activado con ESC o con el boton de menu del HUD.
// Congela el juego (Time.timeScale = 0) y ofrece Continuar / Salir.
[DefaultExecutionOrder(-100)]
public class Pausa : MonoBehaviour
{
    public static bool EstaPausado;

    [Tooltip("Panel de pausa (hijo del Canvas). Debe iniciar desactivado.")]
    public GameObject panelPausa;

    private bool pausado = false;

    void Update()
    {
        // ESC alterna pausa/reanudar. Usamos GetKeyDown para que no se repita.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pausado) Continuar();
            else Pausar();
        }
    }

    // Abre la pausa (llamado desde el boton de menu del HUD).
    public void AbrirMenu()
    {
        if (!pausado) Pausar();
    }

    public void Pausar()
    {
        pausado = true;
        EstaPausado = true;
        if (panelPausa != null) panelPausa.SetActive(true);
        Time.timeScale = 0f;
        Input.ResetInputAxes();
    }

    public void Continuar()
    {
        pausado = false;
        EstaPausado = false;
        if (panelPausa != null) panelPausa.SetActive(false);
        Time.timeScale = 1f;
        Input.ResetInputAxes();
    }

    public void Salir()
    {
        EstaPausado = false;
        PersistenciaJugador.SalirAlMenu();
    }

    // Asegura que al destruirse (cambio de escena) el timeScale quede en 1.
    void OnDestroy()
    {
        Time.timeScale = 1f;
        EstaPausado = false;
    }
}
