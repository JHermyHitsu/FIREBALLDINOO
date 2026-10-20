using UnityEngine;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// Responsabilidad unica: mostrar la imagen final (con su musica) durante
/// unos segundos y avisar cuando termina. No sabe del GameOver ni del reloj.
/// Se adjunta al GameManager.
/// </summary>
public class ImagenFinalController : MonoBehaviour
{
    // Se resuelve por nombre en la escena ("ImagenFinal").
    private GameObject imagenFinal;
    [Header("Musica")]
    public AudioClip musicaImagenFinal;
    [Header("Duracion")]
    public float duracion = 3f;
    [Header("Temblor")]
    public float intensidadTemblor = 40f;
    [Header("Cobertura (evita ver bordes al temblar)")]
    public float escalaCobertura = 1.2f;

    private AudioSource audioSource;
    private bool mostrando = false;

    public bool EstaMostrando => mostrando;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        ResolverImagenFinal();
    }

    public void Iniciar(Action onTerminado)
    {
        if (mostrando) return;
        mostrando = true;

        var mov = FindFirstObjectByType<Movimiento>();
        if (mov != null) mov.enabled = false;

        var img = ResolverImagenFinal();
        if (img != null)
        {
            ActivarAncestros(img);
            var rt0 = img.GetComponent<RectTransform>();
            if (rt0 != null) rt0.localScale = new Vector3(escalaCobertura, escalaCobertura, 1f);
            img.SetActive(true);
        }

        if (audioSource != null && musicaImagenFinal != null)
        {
            audioSource.clip = musicaImagenFinal;
            audioSource.loop = false;
            audioSource.Play();
        }

        StartCoroutine(Rutina(img, onTerminado));
    }

    System.Collections.IEnumerator Rutina(GameObject img, Action onTerminado)
    {
        RectTransform rt = img != null ? img.GetComponent<RectTransform>() : null;
        Vector2 posOriginal = rt != null ? rt.anchoredPosition : Vector2.zero;

        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(
                    UnityEngine.Random.Range(-intensidadTemblor, intensidadTemblor),
                    UnityEngine.Random.Range(-intensidadTemblor, intensidadTemblor));
            }
            yield return null;
        }

        if (rt != null)
        {
            rt.anchoredPosition = posOriginal;
            rt.localScale = Vector3.one;
        }
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        if (img != null) img.SetActive(false);
        mostrando = false;
        onTerminado?.Invoke();
    }

    public void Detener()
    {
        mostrando = false;
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        var img = ResolverImagenFinal();
        if (img != null)
        {
            var rt = img.GetComponent<RectTransform>();
            if (rt != null) { rt.anchoredPosition = Vector2.zero; rt.localScale = Vector3.one; }
            img.SetActive(false);
        }
    }

    GameObject ResolverImagenFinal()
    {
        if (imagenFinal != null && imagenFinal.scene == SceneManager.GetActiveScene()) return imagenFinal;
        imagenFinal = null;

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var encontrado = BuscarHijoRecursivo(canvas.gameObject, "ImagenFinal");
            if (encontrado != null) { imagenFinal = encontrado; return imagenFinal; }
        }
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var encontrado = BuscarHijoRecursivo(root, "ImagenFinal");
            if (encontrado != null) { imagenFinal = encontrado; return imagenFinal; }
        }
        return null;
    }

    GameObject BuscarHijoRecursivo(GameObject obj, string nombre)
    {
        if (obj == null) return null;
        if (obj.name == nombre) return obj;
        foreach (Transform child in obj.transform)
        {
            var found = BuscarHijoRecursivo(child.gameObject, nombre);
            if (found != null) return found;
        }
        return null;
    }

    void ActivarAncestros(GameObject go)
    {
        if (go == null) return;
        Transform t = go.transform.parent;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            t = t.parent;
        }
    }
}