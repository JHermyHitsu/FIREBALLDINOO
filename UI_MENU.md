# 🎮 FireballDino — Plan de Menú Principal y Pausa

> Objetivo: añadir una pantalla de inicio con botón PLAY y un menú de pausa activado con ESC, siguiendo el mismo patrón híbrido (jerarquía + fallback por código) usado en el HUD.

---

## 1. Menú Principal (UI de entrada)

### Descripción
Escena de inicio que se muestra al abrir el juego. Tiene el título del juego, un fondo y un botón PLAY que carga el primer bioma.

### Jerarquía sugerida (escena nueva: `ESCENA_MENU_PRINCIPAL`)

```
Canvas (Screen Space - Overlay)
├── ImagenFondo          (Image — fondo del menú)
├── TextoTitulo          (TextMeshProUGUI — "FIREBALLDINO")
├── BotonPlay            (Button)
│   └── TextoBotonPlay   (TextMeshProUGUI — "PLAY")
└── TextoVersion         (TextMeshProUGUI — "v1.0", opcional)
```

### Pasos en Unity

1. **Crear la escena:**
   - `File → New Scene` → guardar como `ESCENA_MENU_PRINCIPAL` en `Assets/Scenes/`.

2. **Crear el Canvas:**
   - Click derecho en la jerarquía → **UI → Canvas**.
   - En el Canvas: Render Mode = **Screen Space - Overlay**, UI Scale Mode = **Scale With Screen Size**, Reference Resolution = **800x600**.

3. **Fondo:**
   - Click derecho en Canvas → **UI → Image** → renombrar a `ImagenFondo`.
   - RectTransform: anchor stretch/stretch, Left/Top/Right/Bottom = 0.
   - Image: Source Image = tu fondo, Color = blanco, Image Type = Simple.

4. **Título:**
   - Click derecho en Canvas → **UI → Text - TextMeshPro** → renombrar a `TextoTitulo`.
   - RectTransform: anchor top-center, Pos (0, -80), Size (600, 80).
   - TextMeshProUGUI: Text = "FIREBALLDINO", Font Size = 64, Color = blanco, Alignment = centro.

5. **Botón PLAY:**
   - Click derecho en Canvas → **UI → Button - TextMeshPro** → renombrar a `BotonPlay`.
   - RectTransform: anchor center, Pos (0, -40), Size (220, 60).
   - En el componente Button → **OnClick()**: arrastrar el GameObject que tendrá el script `MenuPrincipal` y seleccionar `MenuPrincipal.Jugar()`.
   - Expandir `BotonPlay` → seleccionar `Text (TMP)` → Text = "PLAY", Font Size = 32, Color = negro.

6. **EventSystem:**
   - Si no existe, Unity lo crea automáticamente al crear un Canvas. Verificá que haya uno en la jerarquía.

7. **Agregar la escena en Build Settings:**
   - `File → Build Settings` → **Add Open Scenes** → asegurate de que `ESCENA_MENU_PRINCIPAL` esté **primera** en la lista (índice 0).

### Script: `MenuPrincipal.cs`

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    public void Jugar()
    {
        SceneManager.LoadScene("ESCENA_BIOMA_BOSQUE");
    }

    public void Salir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
```

### Notas
- El script `MenuPrincipal` se coloca en un GameObject vacío en la escena (ej. "MenuManager").
- El botón PLAY llama a `Jugar()` que carga la escena del bosque.
- (Opcional) Agregar botón "SALIR" que llame a `Salir()`.

---

## 2. Menú de Pausa (ESC)

### Descripción
Panel que se activa al presionar ESC. Congela el juego (`Time.timeScale = 0`) y ofrece opciones: Continuar, Reiniciar, Salir.

### Jerarquía sugerida (en `ESCENA_BIOMA_BOSQUE`, bajo el Canvas)

```
Canvas
├── ... (elementos existentes: TextoTiempo, ContadorAmigos, etc.)
└── PanelPausa               (Image — fondo semitransparente, inicia inactivo)
    ├── TextoPausa           (TextMeshProUGUI — "PAUSA")
    ├── BotonContinuar       (Button)
    │   └── TextoContinuar   (TextMeshProUGUI — "CONTINUAR")
    ├── BotonReiniciar       (Button)
    │   └── TextoReiniciar   (TextMeshProUGUI — "REINICIAR")
    └── BotonSalir           (Button)
        └── TextoSalir       (TextMeshProUGUI — "SALIR")
```

### Pasos en Unity

1. **Crear el panel:**
   - En la jerarquía, expandí **GameManager → Canvas**.
   - Click derecho en Canvas → **UI → Panel** → renombralo a `PanelPausa`.
   - RectTransform: anchor stretch/stretch, Left/Top/Right/Bottom = 0.
   - Image: Color = `0, 0, 0, 0.7` (semtransparente negro).
   - **Desactivar el GameObject** (destildar la casilla de activo en el inspector) para que inicie oculto.

2. **Texto "PAUSA":**
   - Click derecho en `PanelPausa` → **UI → Text - TextMeshPro** → renombrar a `TextoPausa`.
   - RectTransform: anchor top-center, Pos (0, -60), Size (400, 60).
   - TextMeshProUGUI: Text = "PAUSA", Font Size = 48, Color = blanco, Alignment = centro.

3. **Botones (repetir 3 veces):**
   - Click derecho en `PanelPausa` → **UI → Button - TextMeshPro**.
   - Renombrar a `BotonContinuar`, `BotonReiniciar`, `BotonSalir`.
   - RectTransform: anchor center, Pos (0, 0 / -60 / -120), Size (220, 50).
   - En cada botón, expandir → seleccionar `Text (TMP)` → cambiar texto y tamaño (24).
   - En el componente Button → **OnClick()**: arrastrar el GameObject con el script `Pausa` y seleccionar el método correspondiente.

4. **Asignar el script:**
   - Crear un GameObject vacío `PausaManager` en la escena (o usar el `GameManager`).
   - Agregar el componente `Pausa`.
   - En el inspector del script, asignar el campo `panelPausa` al GameObject `PanelPausa`.

### Script: `Pausa.cs`

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class Pausa : MonoBehaviour
{
    public GameObject panelPausa;
    private bool pausado = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pausado) Continuar();
            else Pausar();
        }
    }

    public void Pausar()
    {
        pausado = true;
        if (panelPausa != null) panelPausa.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Continuar()
    {
        pausado = false;
        if (panelPausa != null) panelPausa.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Reiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Salir()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("ESCENA_MENU_PRINCIPAL");
    }
}
```

### Notas importantes
- **`Time.timeScale = 0`** congela el juego (el timer, el dino, etc. dejan de moverse). Al reanudar, poner `Time.timeScale = 1`.
- El `Temporizador` usa `Time.deltaTime` para el countdown, que se pausa automáticamente con `timeScale = 0`. ✅
- El `DinoSeguidor` usa `Time.deltaTime` y `Time.time` — también se pausan. ✅
- Al reintentar o salir, **siempre** restaurar `Time.timeScale = 1` antes de cambiar de escena.
- El panel de pausa debe ser **hijo del Canvas** para que se renderice sobre el juego.
- Si el juego tiene un `TransicionManager` persistente, el panel de pausa puede ser persistente también (DontDestroyOnLoad) o recrearse por escena.

---

## Orden de implementación sugerido

1. **Menú Principal** (escena nueva + script `MenuPrincipal` + Build Settings).
2. **Menú de Pausa** (panel en la escena + script `Pausa`).
3. **Probar** en Play Mode: ESC → pausa → Continuar/Reiniciar/Salir.

---

## Archivos a crear

| Archivo | Ubicación |
|---------|-----------|
| `MenuPrincipal.cs` | `Assets/Scripts/` |
| `Pausa.cs` | `Assets/Scripts/` |
| `ESCENA_MENU_PRINCIPAL.unity` | `Assets/Scenes/` |