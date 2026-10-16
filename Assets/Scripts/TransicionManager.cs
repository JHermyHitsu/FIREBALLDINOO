using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Manager único que controla los cambios de escena caminando.
// Vive entre escenas (DontDestroyOnLoad) y es el ÚNICO que debería llamar a SceneManager.
// El jugador y sus dinos reclutados viajan con él para no perder la fila.
public class TransicionManager : MonoBehaviour
{
    public static TransicionManager Instance { get; private set; }

    [Header("Fundido")]
    [Tooltip("CanvasGroup negro que tapa la pantalla. Alpha 0 = visible, 1 = tapado.")]
    public CanvasGroup fader;
    [Tooltip("Duración de cada fade en segundos.")]
    public float duracionFade = 0.35f;

    [Header("Fila")]
    [Tooltip("Cuántos metros detrás del jugador aparecen los seguidores al llegar.")]
    public float separacionFila = 0.7f;

    public bool EnTransicion { get; private set; }

    const string NombreEventSystem = "EventSystem_Persistente";

    // Jugador y seguidores que viajan entre escenas.
    GameObject jugadorPersistente;
    readonly List<DinoSeguidor> filaViajera = new List<DinoSeguidor>();

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

        if (fader == null)
            CrearFader();
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
    void CrearFader()
    {
        var go = new GameObject("FaderCanvas");
        go.transform.SetParent(transform, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        var grupo = go.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        var img = go.AddComponent<Image>();
        img.color = Color.black;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        fader = grupo;
    }

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

    public void IrA(string escenaDestino, string spawnDestino)
    {
        if (EnTransicion) return;
        if (string.IsNullOrEmpty(escenaDestino) || string.IsNullOrEmpty(spawnDestino))
        {
            Debug.LogError("[Transicion] escenaDestino o spawnDestino vacío.");
            return;
        }
        StartCoroutine(Transicion(escenaDestino, spawnDestino));
    }

    IEnumerator Transicion(string escenaDestino, string spawnDestino)
    {
        EnTransicion = true;

        if (jugadorPersistente == null)
            jugadorPersistente = GameObject.FindGameObjectWithTag("Player");
        if (jugadorPersistente == null)
        {
            Debug.LogError("[Transicion] no hay Player en escena.");
            EnTransicion = false;
            yield break;
        }

        Movimiento mov = jugadorPersistente.GetComponent<Movimiento>();
        Rigidbody2D rb = jugadorPersistente.GetComponent<Rigidbody2D>();
        SpriteRenderer sr = jugadorPersistente.GetComponent<SpriteRenderer>();

        // Los dinos reclutados viajan también. Los no reclutados se quedan
        // (se destruyen con su escena, que es lo correcto).
        filaViajera.Clear();
        foreach (DinoSeguidor s in FindObjectsByType<DinoSeguidor>(FindObjectsSortMode.None))
        {
            if (s != null && s.reclutado && !filaViajera.Contains(s))
            {
                DontDestroyOnLoad(s.gameObject);
                filaViajera.Add(s);
            }
        }

        // 1. Congelar al jugador (el Animator sigue, se lo ve corriendo en el fade).
        if (mov != null) mov.enabled = false;
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // 2. Fade a negro.
        yield return StartCoroutine(Fundir(0f, 1f));

        // 3. Cargar la escena en fondo sin activarla hasta que esté lista.
        AsyncOperation op = SceneManager.LoadSceneAsync(escenaDestino, LoadSceneMode.Single);
        if (op == null)
        {
            Debug.LogError("[Transicion] no se pudo cargar: " + escenaDestino + ". ¿Está en Build Profiles?");
            yield return StartCoroutine(Fundir(1f, 0f));
            if (mov != null) mov.enabled = true;
            EnTransicion = false;
            yield break;
        }
        op.allowSceneActivation = false;
        while (op.progress < 0.9f)
            yield return null;
        op.allowSceneActivation = true;
        yield return null;

        // 4. Buscar el spawn. Si no existe, abortar con fade de emergencia
        //    (nunca teleportar al 0,0,0).
        GameObject spawn = GameObject.Find(spawnDestino);
        if (spawn == null)
        {
            Debug.LogError("[Transicion] no existe el spawn '" + spawnDestino + "' en " + escenaDestino + ".");
            yield return StartCoroutine(Fundir(1f, 0f));
            if (mov != null) mov.enabled = true;
            EnTransicion = false;
            yield break;
        }

        // 5. Colocar jugador + fila mirando a la derecha (se sigue corriendo).
        jugadorPersistente.transform.position = spawn.transform.position;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (sr != null) sr.flipX = false;

        // Limpiar y re-vincular la lista de seguidores del jugador
        if (mov != null)
        {
            mov.seguidores.Clear();
        }

        for (int i = filaViajera.Count - 1; i >= 0; i--)
        {
            DinoSeguidor s = filaViajera[i];
            if (s == null)
            {
                filaViajera.RemoveAt(i);
                continue;
            }
            s.transform.position = spawn.transform.position + new Vector3(-separacionFila * (i + 1), 0f, 0f);
            Rigidbody2D rbF = s.GetComponent<Rigidbody2D>();
            if (rbF != null) rbF.linearVelocity = Vector2.zero;
        }

        // Re-vincular los seguidores al jugador para que la fila e índices sean correctos
        if (mov != null)
        {
            for (int i = 0; i < filaViajera.Count; i++)
            {
                DinoSeguidor s = filaViajera[i];
                if (s != null)
                {
                    s.ReVincular(mov);
                    s.AsignarIndice(i);
                    mov.seguidores.Add(s);
                }
            }
        }

        // Limpiar historial de posiciones para que arranquen desde el spawn
        DinoSeguidor.LimpiarHistorial();

        // 6. Re-linkear la cámara de la escena nueva y teleportarla (evita el barrido largo).
        Camara cam = FindFirstObjectByType<Camara>();
        if (cam != null)
        {
            cam.personaje = jugadorPersistente.transform;
            cam.transform.position = new Vector3(spawn.transform.position.x + cam.adelantoX, cam.yFija, -10f);
        }

        // 7. Fade de vuelta y devolver el control.
        yield return StartCoroutine(Fundir(1f, 0f));
        if (mov != null) mov.enabled = true;
        EnTransicion = false;
    }

    IEnumerator Fundir(float desde, float hasta)
    {
        if (fader == null)
        {
            Debug.LogWarning("[Transicion] no hay fader asignado, salto el fundido.");
            yield break;
        }

        float t = 0f;
        while (t < duracionFade)
        {
            t += Time.unscaledDeltaTime;
            fader.alpha = Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duracionFade)));
            yield return null;
        }
        fader.alpha = hasta;
    }
}
