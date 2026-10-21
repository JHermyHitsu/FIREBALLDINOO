using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Transporte entre escenas: fade a negro, carga, colocacion del jugador y la
/// fila en el spawn, re-vinculacion de seguidores y camara.
/// No sabe de persistencia del jugador ni de duplicados (eso es TransicionManager).
/// </summary>
public class TransporteEscena : MonoBehaviour
{
    public static TransporteEscena Instance;

    [Header("Fundido")]
    [Tooltip("CanvasGroup negro que tapa la pantalla. Alpha 0 = visible, 1 = tapado.")]
    public CanvasGroup fader;
    [Tooltip("Duración de cada fade en segundos.")]
    public float duracionFade = 0.35f;

    // Referencia al manager de persistencia (hermano en el mismo GameObject).
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

        if (fader == null) CrearFader();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static TransporteEscena EnsureExists()
    {
        if (Instance != null) return Instance;
        var existente = FindFirstObjectByType<TransporteEscena>();
        if (existente != null) return existente;
        var go = new GameObject("TransporteEscena");
        return go.AddComponent<TransporteEscena>();
    }

    public void IrA(string escenaDestino, string spawnDestino)
    {
        if (transicionManager == null || transicionManager.EnTransicion) return;
        if (string.IsNullOrEmpty(escenaDestino) || string.IsNullOrEmpty(spawnDestino))
        {
            Debug.LogError("[Transicion] escenaDestino o spawnDestino vacío.");
            return;
        }
        StartCoroutine(Transicion(escenaDestino, spawnDestino));
    }

    IEnumerator Transicion(string escenaDestino, string spawnDestino)
    {
        if (transicionManager == null) yield break;
        transicionManager.EnTransicion = true;

        GameObject jugadorPersistente = transicionManager.jugadorPersistente;
        if (jugadorPersistente == null)
            jugadorPersistente = GameObject.FindGameObjectWithTag("Player");
        if (jugadorPersistente == null)
        {
            Debug.LogError("[Transicion] no hay Player en escena.");
            transicionManager.EnTransicion = false;
            yield break;
        }
        transicionManager.jugadorPersistente = jugadorPersistente;

        Movimiento mov = jugadorPersistente.GetComponent<Movimiento>();
        Rigidbody2D rb = jugadorPersistente.GetComponent<Rigidbody2D>();
        SpriteRenderer sr = jugadorPersistente.GetComponent<SpriteRenderer>();

        // Los dinos reclutados viajan también respetando su orden en la fila.
        List<DinoSeguidor> filaViajera = transicionManager.filaViajera;
        filaViajera.Clear();
        if (mov != null && mov.seguidores != null)
        {
            foreach (DinoSeguidor s in mov.seguidores)
            {
                if (s != null && s.reclutado && !filaViajera.Contains(s))
                {
                    DontDestroyOnLoad(s.gameObject);
                    filaViajera.Add(s);
                }
            }
        }
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
            transicionManager.EnTransicion = false;
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
            transicionManager.EnTransicion = false;
            yield break;
        }

        // 5. Colocar jugador + fila mirando a la derecha (se sigue corriendo).
        if (jugadorPersistente == null)
        {
            Debug.LogError("[Transicion] jugador destruido durante la carga.");
            yield return StartCoroutine(Fundir(1f, 0f));
            if (mov != null) mov.enabled = true;
            transicionManager.EnTransicion = false;
            yield break;
        }
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
            s.transform.position = spawn.transform.position + new Vector3(-transicionManager.separacionFila * (i + 1), 0f, 0f);
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
        transicionManager.EnTransicion = false;
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
}