using UnityEngine;

public class BotonGameOver : MonoBehaviour
{
    public void Reintentar()
    {
        Temporizador.EnsureExists().Reintentar();
    }

    public void Salir()
    {
        Temporizador.EnsureExists().Salir();
    }
}
