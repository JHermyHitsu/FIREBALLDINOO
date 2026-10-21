using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistencia del jugador entre escenas: DontDestroyOnLoad, resolucion de
/// duplicados al cambiar de escena, adopcion del jugador nuevo y destruccion
/// de la fila vieja en un reinicio.
/// No sabe de transporte (fade) ni de EventSystem.
/// </summary>
public class PersistenciaJugador : MonoBehaviour
{
    public static PersistenciaJugador Instance;

    // Jugador y seguidores que viajan entre escenas.
    public GameObject jugadorPersistente;
    public readonly List<DinoSeguidor> filaViajera = new List<DinoSeguidor>();

    // Estado de transicion (lo setea TransporteEscena).
    public bool EnTransicion { get; set; }

    // Referencia al manager hermano (EventSystem, etc.)
    public TransicionManager transicionManager;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (transicionManager == null) transicionManager = GetComponent<TransicionManager>();
        if (transicionManager == null) transicionManager = gameObject.AddComponent<TransicionManager>();

        jugadorPersistente = GameObject.FindGameObjectWithTag("Player");
        if (jugadorPersistente != null)
            DontDestroyOnLoad(jugadorPersistente);

        SceneManager.sceneLoaded += AlCargarEscena;
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= AlCargarEscena;
        if (Instance == this) Instance = null;
    }

    public static PersistenciaJugador EnsureExists()
    {
        if (Instance != null) return Instance;
        var existente = FindFirstObjectByType<PersistenciaJugador>();
        if (existente != null) return existente;
        var go = new GameObject("PersistenciaJugador");
        return go.AddComponent<PersistenciaJugador>();
    }

    // Salida limpia al menu: detiene todos los audios y destruye el jugador persistente.
    public static void SalirAlMenu()
    {
        Time.timeScale = 1f;
        foreach (var a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            if (a != null && a.isPlaying) a.Stop();

        if (Instance != null)
        {
            Instance.EnTransicion = false;
            if (Instance.jugadorPersistente != null) Destroy(Instance.jugadorPersistente);
            Instance.jugadorPersistente = null;
            Instance.filaViajera.Clear();
        }

        SceneManager.LoadScene(Escenas.MenuInicio);
    }

    // Evita jugadores duplicados:
    // - En transicion: gana el jugador viajero, se destruye el que traiga la escena nueva.
    // - Sin transicion (muerte y reinicio): se adopta el jugador fresco de la escena
    //   y se destruye la fila vieja para empezar limpio.
    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (transicionManager != null) transicionManager.AsegurarEventSystem();

        GameObject[] jugadores = GameObject.FindGameObjectsWithTag("Player");
        if (jugadores.Length <= 1)
        {
            if (jugadores.Length == 1 && jugadorPersistente == null)
            {
                jugadorPersistente = jugadores[0];
                DontDestroyOnLoad(jugadorPersistente);
            }
            return;
        }

        if (EnTransicion && jugadorPersistente != null)
        {
            foreach (GameObject j in jugadores)
                if (j != jugadorPersistente)
                    Destroy(j);
        }
        else
        {
            foreach (DinoSeguidor s in filaViajera)
                if (s != null)
                    Destroy(s.gameObject);
            filaViajera.Clear();

            // Intercambio al jugador fresco de la escena. Se elige explícitamente
            // uno distinto al viejo: adoptar jugadores[0] a ciegas podía quedarse
            // con el cadáver recién destruido (p. ej. reintentar tras morir).
            GameObject viejo = jugadorPersistente;
            GameObject nuevo = null;
            foreach (GameObject j in jugadores)
                if (j != null && j != viejo) { nuevo = j; break; }

            if (viejo != null)
                Destroy(viejo);

            jugadorPersistente = nuevo;
            if (jugadorPersistente == null)
                jugadorPersistente = GameObject.FindGameObjectWithTag("Player");
            if (jugadorPersistente != null)
                DontDestroyOnLoad(jugadorPersistente);
        }
    }
}