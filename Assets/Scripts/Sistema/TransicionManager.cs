using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Manager único que controla los cambios de escena caminando.
// Vive entre escenas (DontDestroyOnLoad) y es el ÚNICO que debería llamar a SceneManager.
// El jugador y sus dinos reclutados viajan con él para no perder la fila.
public class TransicionManager : MonoBehaviour
{
    public static TransicionManager Instance { get; private set; }

    [Header("Fila")]
    [Tooltip("Cuántos metros detrás del jugador aparecen los seguidores al llegar.")]
    public float separacionFila = 0.7f;

    // Persistencia del jugador vive en PersistenciaJugador (hermano).
    public PersistenciaJugador persistenciaJugador;

    // Delegados a PersistenciaJugador para compatibilidad con TransporteEscena.
    public GameObject jugadorPersistente
    {
        get => persistenciaJugador != null ? persistenciaJugador.jugadorPersistente : null;
        set { if (persistenciaJugador != null) persistenciaJugador.jugadorPersistente = value; }
    }
    public List<DinoSeguidor> filaViajera => persistenciaJugador != null ? persistenciaJugador.filaViajera : null;
    public bool EnTransicion
    {
        get => persistenciaJugador != null && persistenciaJugador.EnTransicion;
        set { if (persistenciaJugador != null) persistenciaJugador.EnTransicion = value; }
    }

    const string NombreEventSystem = "EventSystem_Persistente";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (persistenciaJugador == null) persistenciaJugador = GetComponent<PersistenciaJugador>();
        if (persistenciaJugador == null) persistenciaJugador = gameObject.AddComponent<PersistenciaJugador>();

        AsegurarEventSystem();
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
    public void AsegurarEventSystem()
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
            {
                s.gameObject.SetActive(false);
                Destroy(s.gameObject);
            }
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
        if (Instance == this) Instance = null;
    }
}
