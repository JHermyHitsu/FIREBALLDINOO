using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuInicial : MonoBehaviour
{
    public string escenaJuego = "ESCENA_BIOMA_BOSQUE";

    public void IniciarJuego()
    {
        SceneManager.LoadScene(escenaJuego);
    }
}