using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ComicIntro : MonoBehaviour
{
    [Header("Imagenes del comic")]
    public Image imagenIntro;
    public Sprite[] imagenes;

    [Header("Configuracion")]
    public float tiempoPorImagen = 3f;
    public string escenaJuego = "ESCENA_BIOMA_BOSQUE";

    [Header("Boton Skip (opcional)")]
    public GameObject botonSkip;

    [Header("Boton JUGAR (se oculta al iniciar comic)")]
    public GameObject botonJugar;

    private int indiceActual = 0;
    private bool saltado = false;
    private bool comicIniciado = false;

    void Start()
    {
        // No iniciar automáticamente — esperar al botón JUGAR
        if (imagenIntro != null)
            imagenIntro.gameObject.SetActive(false);
    }

    public void IniciarComic()
    {
        if (comicIniciado) return;
        comicIniciado = true;

        if (imagenes == null || imagenes.Length == 0)
        {
            CargarJuego();
            return;
        }

        if (imagenIntro != null)
        {
            imagenIntro.gameObject.SetActive(true);
            imagenIntro.sprite = imagenes[0];
        }

        if (botonJugar != null)
            botonJugar.SetActive(false);

        StartCoroutine(AvanzarComic());
    }

    IEnumerator AvanzarComic()
    {
        while (indiceActual < imagenes.Length - 1)
        {
            yield return new WaitForSeconds(tiempoPorImagen);
            if (saltado) yield break;
            indiceActual++;
            if (imagenIntro != null)
                imagenIntro.sprite = imagenes[indiceActual];
        }

        yield return new WaitForSeconds(tiempoPorImagen);
        if (!saltado)
            CargarJuego();
    }

    public void Saltar()
    {
        saltado = true;
        StopAllCoroutines();
        CargarJuego();
    }

    public void Siguiente()
    {
        if (saltado) return;
        StopAllCoroutines();
        indiceActual++;
        if (indiceActual >= imagenes.Length)
        {
            CargarJuego();
            return;
        }
        if (imagenIntro != null)
            imagenIntro.sprite = imagenes[indiceActual];
        StartCoroutine(AvanzarComic());
    }

    void CargarJuego()
    {
        SceneManager.LoadScene(escenaJuego);
    }
}
