// =============================================================
// AutoSave.cs — Guarda automáticamente como VSCode
// Coloca este archivo en: Assets/Editor/AutoSave.cs
// =============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AutoSave
{
    // ── Configuración ──────────────────────────────────────────
    private static bool SAVE_ON_CHANGE    = true;   // Guarda al detectar cambios (como VSCode)
    private static bool SAVE_ON_PLAY      = true;   // Guarda al presionar Play
    private static bool SHOW_LOG          = true;   // Muestra mensaje en Console
    // ──────────────────────────────────────────────────────────

    static AutoSave()
    {
        // Suscribirse a eventos del editor
        EditorApplication.hierarchyChanged  += OnChange;
        EditorApplication.projectChanged    += OnChange;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;

        if (SHOW_LOG)
            Debug.Log("<color=#00e5ff>[AutoSave]</color> Activado ✓ — guarda automáticamente al detectar cambios.");
    }

    // ── Se llama cada vez que algo cambia en la escena o el proyecto ──
    private static void OnChange()
    {
        if (!SAVE_ON_CHANGE)            return;
        if (EditorApplication.isPlaying) return;   // No guardar durante Play Mode
        if (BuildPipeline.isBuildingPlayer) return;

        SaveAll();
    }

    // ── Se llama al entrar / salir de Play Mode ──
    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SAVE_ON_PLAY) return;

        // Justo antes de entrar en Play Mode
        if (state == PlayModeStateChange.ExitingEditMode)
            SaveAll();
    }

    // ── Lógica de guardado ──
    private static void SaveAll()
    {
        // Guarda todas las escenas abiertas que tengan cambios sin guardar
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.isDirty && scene.IsValid() && !string.IsNullOrEmpty(scene.path))
            {
                EditorSceneManager.SaveScene(scene);

                if (SHOW_LOG)
                    Debug.Log($"<color=#00e5ff>[AutoSave]</color> Escena guardada: <b>{scene.name}</b>");
            }
        }

        // Guarda assets del proyecto (prefabs, ScriptableObjects, etc.)
        AssetDatabase.SaveAssets();
    }
}
#endif
