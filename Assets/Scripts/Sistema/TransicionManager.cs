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

    // Asegura que siempre haya un EventSystem activo en la escena actual
    // (p. ej. Desierto que no trae EventSystem propio en su jerarquía).
    // No usamos DontDestroyOnLoad para evitar colisiones ("There can be only one active Event System")
    // con escenas que ya traen su propio EventSystem (Bosque, Cuevas, Menú).
    public void AsegurarEventSystem()
    {
        var sistemas = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (sistemas == null || sistemas.Length == 0)
        {
            CrearEventSystem();
            return;
        }

        // Si por alguna razón hubiera más de uno activo en la misma escena, eliminar duplicados
        for (int i = 1; i < sistemas.Length; i++)
        {
            if (sistemas[i] != null)
            {
                sistemas[i].gameObject.SetActive(false);
                Destroy(sistemas[i].gameObject);
            }
        }
    }

    EventSystem CrearEventSystem()
    {
        var go = new GameObject(NombreEventSystem);
        var es = go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
        return es;
    }

    // Fader mínimo por código; solo se usa si el manager se creó sin escena (sin Bosque).
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
