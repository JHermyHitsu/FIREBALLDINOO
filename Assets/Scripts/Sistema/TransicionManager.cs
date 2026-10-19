using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Manager único que controla los cambios de escena caminando.
// Vive entre escenas (DontDestroyOnLoad) y es el ÚNICO que debería llamar a SceneManager.
// El jugador y sus dinos reclutados viajan con él para no perder la fila.
public class TransicionManager : MonoBehaviour
{
    public static TransicionManager Instance { get; private set; }

    [Header("Fila")]
    [Tooltip("Cuántos metros detrás del jugador aparecen los seguidores al llegar.")]
    public float separacionFila = 0.7f;

    public bool EnTransicion { get; set; }

    const string NombreEventSystem = "EventSystem_Persistente";

    // Jugador y seguidores que viajan entre escenas.
    public GameObject jugadorPersistente;
    public readonly List<DinoSeguidor> filaViajera = new List<DinoSeguidor>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        jugadorPersistente = GameObject.FindGameObjectWithTag("Player");
        if (jugadorPersistente != null)
            DontDestroyOnLoad(jugadorPersistente);

        AsegurarEventSystem();

        SceneManager.sceneLoaded += AlCargarEscena;
    }

    // Devuelve el manager persistente; lo crea si la escena no trae uno
    // (p. ej. Play directo en Desierto/Cueva).
    public static TransicionManager EnsureExists()
    {
        if (Instance != null) return Instance;
        var existente = FindFirstObjectByType<TransicionManager>();
        if (existente != null) return existente;
        var go = new GameObject("TransicionManager");
        return go.AddComponent<TransicionManager>();
    }

    // El EventSystem de Bosque (con su InputSystemUIInputModule ya configurado)
    // viaja entre escenas; sin él los botones del Game Over no responden en
    // Desierto/Cueva, que no traen EventSystem propio.
    void AsegurarEventSystem()
    {
        var sistemas = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        EventSystem nuestro = null;
        foreach (var s in sistemas)
            if (s != null && s.gameObject.name == NombreEventSystem) { nuestro = s; break; }

        if (nuestro == null)
        {
            foreach (var s in sistemas)
            {
                if (s == null) continue;
                nuestro = s;
                nuestro.gameObject.name = NombreEventSystem;
                DontDestroyOnLoad(nuestro.gameObject);
                break;
            }
            if (nuestro == null)
                nuestro = CrearEventSystem();
        }

        // Elimina duplicados de escena (p. ej. el EventSystem fresco de Bosque al volver).
        foreach (var s in sistemas)
            if (s != null && s != nuestro)
                Destroy(s.gameObject);
    }

    EventSystem CrearEventSystem()
    {
        var go = new GameObject(NombreEventSystem);
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
        DontDestroyOnLoad(go);
        return go.GetComponent<EventSystem>();
    }

    // Fader mínimo por código; solo se usa si el manager se creó sin escena (sin Bosque).
    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= AlCargarEscena;
    }

    // Evita jugadores duplicados:
    // - En transición: gana el jugador viajero, se destruye el que traiga la escena nueva.
    // - Sin transición (muerte y reinicio): se adopta el jugador fresco de la escena
    //   y se destruye la fila vieja para empezar limpio.
    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        AsegurarEventSystem();

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
