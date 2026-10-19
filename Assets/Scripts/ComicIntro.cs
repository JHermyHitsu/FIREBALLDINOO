using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Muestra el comic de introduccion y carga la escena de juego al terminar.
/// Se adjunta al GameObject "ComicIntro" (antes "Gestion_Menu").
/// El boton JUGAR llama a IniciarComic() via onClick.
/// </summary>
public class ComicIntro : MonoBehaviour
{
    [Header("Imagenes del comic")]
    public Image imagenIntro;
    public Sprite[] imagenes;

    [Header("Configuracion")]
    public float tiempoPorImagen = 3f;

    [Header("Boton JUGAR (se oculta al iniciar el comic)")]
    public GameObject botonJugar;

    int indiceActual = 0;
    bool comicIniciado = false;

    void Start()
    {
        // La imagen del comic arranca oculta; se muestra al presionar JUGAR.
        if (imagenIntro != null)
            imagenIntro.gameObject.SetActive(false);
    }

    /// <summary>
    /// Inicia el slideshow. Se llama desde el boton JUGAR.
    /// Si no hay imagenes, carga el juego directamente.
    /// </summary>
    public void IniciarComic()
    {
        if (comicIniciado) return;
        comicIniciado = true;

        if (imagenes == null || imagenes.Length == 0)
        {
            CargarJuego();
            return;
        }

        imagenIntro.gameObject.SetActive(true);
        imagenIntro.sprite = imagenes[0];

        if (botonJugar != null)
            botonJugar.SetActive(false);

        StartCoroutine(AvanzarComic());
    }

    /// <summary>
    /// Avanza las imagenes automaticamente segun tiempoPorImagen.
    /// Al terminar, carga la escena de juego.
    /// </summary>
    IEnumerator AvanzarComic()
    {
        while (indiceActual < imagenes.Length - 1)
        {
            yield return new WaitForSeconds(tiempoPorImagen);
            indiceActual++;
            imagenIntro.sprite = imagenes[indiceActual];
        }

        yield return new WaitForSeconds(tiempoPorImagen);
        CargarJuego();
    }

    void CargarJuego()
    {
        // El menu no tiene Player, asi que va directo por SceneManager.
        // TransicionManager es solo para cambios de escena dentro del juego.
        SceneManager.LoadScene(Escenas.BiomaBosque);
    }
}
