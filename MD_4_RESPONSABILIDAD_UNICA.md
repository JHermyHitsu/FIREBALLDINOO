# MD 4 — Partición por Responsabilidad Única

> **Fecha:** 2026-10-18
> **Alcance:** los 4 scripts con múltiples responsabilidades
> **Estado:** propuesta — requiere aprobación antes de ejecutar
> **Prerequisitos:** migración de carpetas completa (MD 2 + MD 3)
> **Tag de referencia:** `migracion-completa`

---

## 1. Diagnóstico —Responsabilidad Única (S de SOLID)

| Script | Ubicación actual | Líneas | Responsabilidades | Peor caso |
|---|---|---|---|---|
| `Temporizador` | `Sistema/` | 754 | **14** | ❌ Peor del proyecto |
| `Movimiento` | `Gameplay/` | 181 | **10** | ❌ |
| `TransicionManager` | `Sistema/` | 340 | **8** | ❌ |
| `DinoSeguidor` | `Gameplay/` | 202 | **6** | ❌ |

### Otros principios (contexto, no en alcance)

| Principio | Estado | Nota |
|---|---|---|
| **O** — Abierto/Cerrado | ❌ | Cada panel nuevo obliga a tocar `Temporizador` |
| **L** — Liskov | ⚠️ | Sin herencia; `Morir(bool esPrecipicio)` recibe un parámetro que nunca lee |
| **I** — Segregación | ❌ | Cero interfaces. `DinoSeguidor` depende del `Movimiento` entero cuando necesita 4 métodos |
| **D** — Dependencias | ❌ | Depende de clases concretas; **busca GameObjects por nombre** (`BuscarHijoRecursivo`) |

> Este MD ataca **solo S**. Los otros tres requieren decisiones de diseño que van más allá de partición.

---

## 2. El inventario de responsabilidades

### 2.1 `Temporizador` — 14 responsabilidades

| # | Responsabilidad | Ubicación | Riesgo al mover |
|---|---|---|---|
| 1 | Reloj (cuenta, formatea `mm:ss`) | `Update` L468-470 | 🟢 Bajo |
| 2 | Color rojo neón bajo 15s | `Update` L481-484 | 🟢 Bajo |
| 3 | Tick de pánico (escala/rotación/offset) | `Update` L486-528 | 🟡 Medio |
| 4 | Contador de amigos + animación pop | `Update` L531-556 | 🟡 Medio |
| 5 | Barra de boost (`fillAmount`) | `Update` L558-571 | 🟢 Bajo |
| 6 | Segmentos de boost (estilo Subway) | `Update` L574-600 | 🟡 Medio |
| 7 | Temblor de cámara bajo 16s | `Update` L601-608 | 🟢 Bajo |
| 8 | Panel Game Over | `MostrarGameOver` | 🔴 Alto |
| 9 | Panel Perder (todos caídos) | `MostrarGameOver` | 🔴 Alto |
| 10 | Panel Victoria | `MostrarVictoria` | 🔴 Alto |
| 11 | Construcción de UI persistente **por código** | `ConstruirUIPersistente` L296-383 | 🔴 Muy alto |
| 12 | Reconexión de UI al cambiar de escena | `AlCargarEscena` L74-137 | 🔴 Muy alto |
| 13 | Reconexión de botones de paneles | `ReconectarBotonesPaneles` L138-174 | 🟡 Medio |
| 14 | AudioSource de derrota | `sonidoPerder` | 🟢 Bajo |

### 2.2 `Movimiento` — 10 responsabilidades

| # | Responsabilidad | Riesgo al mover |
|---|---|---|
| 1 | Lectura de input (`GetAxisRaw`) | 🟢 Bajo |
| 2 | Física horizontal (`FixedUpdate`) | 🟢 Bajo |
| 3 | Salto + detección de suelo | 🟡 Medio |
| 4 | Flip del sprite | 🟢 Bajo |
| 5 | Animación (Animator por string) | 🟡 Medio |
| 6 | Audio (salto, muerte) | 🟢 Bajo |
| 7 | Boost (estado + multiplicador) | 🟡 Medio |
| 8 | Fila de seguidores (alta/baja/reindexado) | 🔴 Alto |
| 9 | Muerte + sacrificio de seguidor | 🔴 Alto |
| 10 | Trigger de Game Over | 🔴 Alto |

### 2.3 `TransicionManager` — 8 responsabilidades

| # | Responsabilidad | Riesgo al mover |
|---|---|---|
| 1 | Corrutina de transición | 🟡 Medio |
| 2 | Fade a negro (CanvasGroup) | 🟡 Medio |
| 3 | Construcción del fader por código | 🟢 Bajo |
| 4 | Persistencia del jugador | 🔴 Alto |
| 5 | Resolución de duplicados de jugador | 🔴 Alto |
| 6 | Transporte de la fila de seguidores | 🔴 Alto |
| 7 | Re-vinculación de la cámara | 🟡 Medio |
| 8 | Ciclo de vida del EventSystem | 🟡 Medio |

### 2.4 `DinoSeguidor` — 6 responsabilidades

| # | Responsabilidad | Riesgo al mover |
|---|---|---|
| 1 | Auto-reclutamiento al tocar al jugador | 🟡 Medio |
| 2 | Matemáticas de seguimiento en cadena | 🔴 Alto |
| 3 | Animación (velocidad, suelo, flip) | 🟡 Medio |
| 4 | Sacrificio | 🔴 Alto |
| 5 | Muerte + destrucción | 🟡 Medio |
| 6 | Audio | 🟢 Bajo |

---

## 3. Las dos trampas de este refactor

### 3.1 Campos serializados — pérdida silenciosa de datos

`Temporizador` tiene estos camposserializados:

```csharp
public TextMeshProUGUI textoTiempo;
public TextMeshProUGUI textoContadorAmigos;
public Image barraBoost;
public Image[] segmentosBoost;
public GameObject panelGameOver;
public GameObject panelPerder;
public GameObject imagenTiempoAgotado;
public AudioClip sonidoPerder;
public GameObject panelVictoria;
public TextMeshProUGUI textoVictoria;
```

**En la escena están guardados como `fileID`:**

```yaml
# ESCENA_BIOMA_BOSQUE.unity (línea ~893)
tiempoInicial: 210
textoTiempo: {fileID: 1037321432}
textoContadorAmigos: {fileID: 11367469}
barraBoost: {fileID: 0}
```

Si movés un campo a otro componente, **Unity no tira error: pierde la referencia en silencio**. El síntoma es un panel que no aparece, y la causa real está a metros del error.

> **Regla:** cada campo que se mueve se tiene que re-arrastrar **a mano** en el Inspector de las 3 escenas.

### 3.2 El doble camino de UI

`Temporizador` construye UI **por código** cuando la escena no la trae:

```csharp
void ConstruirUIPersistente()   // L296-383
void AsegurarContadorAmigos()   // L228-266
void AsegurarSegmentosBoost()   // L267-295
void ConstruirPanelVictoria()   // L414-465
```

| Escena | UI serializada | Camino que corre |
|---|---|---|
| `ESCENA_BIOMA_BOSQUE` | Sí | El del Inspector |
| `ESCENA_BIOMA_CUEVAS` | No | **El de código** |
| `ESCENA_BIOMA_DESIERTO` | No | **El de código** |

**Riesgo:** si partís `Temporizador` y solo tocás el camino del Inspector, **Desierto y Cuevas se quedan sin HUD**. Y no lo vas a ver hasta que entres a esas escenas.

---

## 4. El bloqueo real: el singleton

```csharp
Temporizador.EnsureExists().MostrarVictoria();   // MetaFinal
Temporizador.EnsureExists().MostrarGameOver(...); // Movimiento
Temporizar.EnsureExists().Reintentar();          // BotonGameOver
Temporizador.EnsureExists().Salir();             // BotonGameOver
```

**4 llamadas desde afuera, todas piden el monolito entero.**

Mientras esas llamadas sigan siendo así, partir la clase es **cosmético**: los otros archivos siguen necesitando un `Temporizador` con los 14 métodos.

> **Consecuencia:** este refactor es de **dos tiempos**.
> - **Tiempo 1:** crear los componentes nuevos y que el viejo les pase los datos.
> - **Tiempo 2:** cambiar los llamadores para que usen el componente específico.
>
> Sin el tiempo 2, el trabajo del tiempo 1 queda a medias.

---

## 5. Orden de ejecución recomendado

De menor a mayor riesgo. **Un script por vez, con verificación completa.**

| Etapa | Qué | Dificultad | Reversible |
|---|---|---|---|
| **E1** | `HUD` fuera de `Temporizador` | 🟢 Baja | ✅ Sí |
| **E2** | `Reloj` fuera de `Temporizador` | 🟢 Baja | ✅ Sí |
| **E3** | `PanelFinPartida` fuera de `Temporizador` | 🔴 Alta | ⚠️ Parcial |
| **E4** | `UIPersistente` fuera de `Temporizador` | 🔴 Muy alta | ❌ No |
| **E5** | `EstadoJugador` fuera de `Movimiento` | 🟡 Media | ✅ Sí |
| **E6** | `SistemaSaludyMuerte` fuera de `Movimiento` | 🟡 Media | ✅ Sí |
| **E7** | `ControladorFila` fuera de `Movimiento` | 🔴 Alta | ⚠️ Parcial |
| **E8** | `FilaDinos` fuera de `DinoSeguidor` | 🔴 Alta | ⚠️ Parcial |
| **E9** | `TransporteEscena` fuera de `TransicionManager` | 🟡 Media | ✅ Sí |
| **E10** | `PersistenciaJugador` fuera de `TransicionManager` | 🟡 Media | ✅ Sí |

---

## 6. Etapa E1 — `HUD` fuera de `Temporizador` (la primera)

### Qué se mueve

| Campo | Tipo actual | Va a |
|---|---|---|
| `textoContadorAmigos` | `TextMeshProUGUI` | `HUD` |
| `barraBoost` | `Image` | `HUD` |
| `segmentosBoost` | `Image[]` | `HUD` |
| `ultimoCountAmigos` | `int` | `HUD` |
| `progresoPopAmigos` | `float` | `HUD` |

### Qué NO se mueve en esta etapa

- `textoTiempo` → es de E2
- `panelGameOver`, `panelPerder`, `panelVictoria` → son de E3
- Todo el código de construcción por código → es de E4
- `tiempoRestante` → **queda en `Temporizador`**; `HUD` solo lo lee

### El nuevo componente

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Solo lo que se ve en pantalla mientras se juega: contador de amigos,
/// barra de boost y segmentos. No sabe nada del reloj ni de los paneles.
/// Lee el estado de Movimiento, no lo modifica.
/// </summary>
public class HUD : MonoBehaviour
{
    [Header("Contador de amigos")]
    public TextMeshProUGUI textoContadorAmigos;

    [Header("Boost")]
    public Image barraBoost;
    public Image[] segmentosBoost;

    // Estado interno de las animaciones
    int ultimoCountAmigos = 0;
    float progresoPopAmigos = 1f;

    void Awake()
    {
        // Los segmentos arrancan apagados: solo se ven con el boost activo.
        if (segmentosBoost != null)
        {
            foreach (var s in segmentosBoost)
                if (s != null) s.gameObject.SetActive(false);
            if (segmentosBoost.Length > 0 && segmentosBoost[0] != null)
            {
                Transform padre = segmentosBoost[0].transform.parent;
                if (padre != null) padre.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        ActualizarContadorAmigos();
        ActualizarBarraBoost();
        ActualizarSegmentosBoost();
    }

    // Muestra cuántos dinos te siguen. El pop escala cuando el numero cambia.
    void ActualizarContadorAmigos()
    {
        if (textoContadorAmigos == null) return;

        var mov = FindFirstObjectByType<Movimiento>();
        int count = (mov != null && mov.seguidores != null) ? mov.seguidores.Count : 0;

        if (count != ultimoCountAmigos)
        {
            ultimoCountAmigos = count;
            progresoPopAmigos = 0f;
        }
        progresoPopAmigos = Mathf.Min(1f, progresoPopAmigos + Time.deltaTime / 0.2f);

        if (progresoPopAmigos < 1f)
        {
            float popScale;
            if (progresoPopAmigos < 0.5f)
                popScale = Mathf.Lerp(1f, 1.3f, progresoPopAmigos / 0.5f);
            else
                popScale = Mathf.Lerp(1.3f, 1f, (progresoPopAmigos - 0.5f) / 0.5f);
            textoContadorAmigos.rectTransform.localScale = Vector3.one * popScale;
        }
        textoContadorAmigos.text = $"{count}";
    }

    // La barra se vacía sola mientras el boost sigue activo.
    void ActualizarBarraBoost()
    {
        if (barraBoost == null) return;

        var mov = FindFirstObjectByType<Movimiento>();
        if (mov != null && mov.BoostActivo)
        {
            barraBoost.gameObject.SetActive(true);
            float dur = mov.BoostDuracion > 0f ? mov.BoostDuracion : 1f;
            barraBoost.fillAmount = Mathf.Clamp01(mov.BoostTiempoRestante / dur);
        }
        else
        {
            barraBoost.gameObject.SetActive(false);
        }
    }

    // Cuantos segmentos se encienden = cuanto boost queda, de 3 en 3.
    void ActualizarSegmentosBoost()
    {
        if (segmentosBoost == null || segmentosBoost.Length == 0) return;

        var mov = FindFirstObjectByType<Movimiento>();
        float fraccion = 0f;
        int total = segmentosBoost.Length;

        if (mov != null && mov.BoostActivo)
        {
            float dur = mov.BoostDuracion > 0f ? mov.BoostDuracion : 1f;
            fraccion = Mathf.Clamp01(mov.BoostTiempoRestante / dur);
        }

        int activos = Mathf.CeilToInt(fraccion * total);
        for (int i = 0; i < total; i++)
            if (segmentosBoost[i] != null)
                segmentosBoost[i].gameObject.SetActive(i < activos);

        // El contenedor padre se apaga cuando no queda ninguno.
        if (segmentosBoost[0] != null)
        {
            Transform padre = segmentosBoost[0].transform.parent;
            if (padre != null) padre.gameObject.SetActive(activos > 0);
        }
    }

    // Lo llaman MostrarGameOver y MostrarVictoria para apagar el HUD.
    public void Apagar()
    {
        if (textoContadorAmigos != null) textoContadorAmigos.gameObject.SetActive(false);
        if (barraBoost != null) barraBoost.gameObject.SetActive(false);
        if (segmentosBoost != null)
        {
            foreach (var s in segmentosBoost)
                if (s != null) s.gameObject.SetActive(false);
            if (segmentosBoost.Length > 0 && segmentosBoost[0] != null)
            {
                Transform padre = segmentosBoost[0].transform.parent;
                if (padre != null) padre.gameObject.SetActive(false);
            }
        }
    }

    // Lo llama Reintentar para restaurar el HUD.
    public void Encender()
    {
        if (textoContadorAmigos != null) textoContadorAmigos.gameObject.SetActive(true);
        if (barraBoost != null) barraBoost.gameObject.SetActive(true);
    }
}
```

### Cómo se conecta con `Temporizador`

`Temporizador` obtiene la referencia en `Awake` y la usa en vez de sus propios campos:

```csharp
// En Temporizador, nueva referencia (NO serializada, se resuelve en runtime)
HUD hud;

void Awake()
{
    // ... lo que ya existe
    hud = GetComponent<HUD>();
    if (hud == null) hud = gameObject.AddComponent<HUD>();
}
```

Y los call sites cambian:

| Antes | Después |
|---|---|
| `textoContadorAmigos.gameObject.SetActive(false)` | `hud.Apagar()` |
| `barraBoost.gameObject.SetActive(false)` | `hud.Apagar()` |
| `if (textoContadorAmigos != null) textoContadorAmigos.gameObject.SetActive(true)` | `hud.Encender()` |

**Los bloques de `Update` del contador y del boost se eliminan de `Temporizador`** — ahora viven en `HUD.Update()`.

### Los pasos

| # | Paso | Verificación |
|---|---|---|
| 1 | Cerrar Unity, crear tag `pre-hud` | — |
| 2 | Crear `Assets/Scripts/UI/HUD.cs` | Compila |
| 3 | Abrir Unity, agregar `HUD` al GameObject `GameManager` de **Bosque** | Sin errores |
| 4 | Arrastrar `TextoContadorAmigos` → `textoContadorAmigos` | Inspector muestra la referencia |
| 5 | Arrastrar los 3 segmentos → `segmentosBoost` | Ídem |
| 6 | `barraBoost` queda en null (no existe en la escena) | Ídem |
| 7 | Borrar los 3 campos de `Temporizador.cs` | Compila |
| 8 | Reemplazar los call sites por `hud.Apagar()` / `hud.Encender()` | Compila |
| 9 | **Probar en Bosque** | reloj, contador, boost, game over, reintentar |
| 10 | **Probar en Cuevas y Desierto** (Play directo) | El HUD aparece — construido por código |
| 11 | Commit | — |

### El paso 10 es el que casi todos olvidan

Si `HUD` necesita funcionar también cuando **no hay HUD serializado** (Cuevas, Desierto), hay que copiar la parte de construcción por código. **Esa es la E4.**

**Para E1 completa:** `HUD` debe construirse a sí mismo si no encuentra referencias. Es el mismo patrón que ya usa `Temporizador`, nada nuevo.

---

## 7. Verificación por etapa

### Siempre, antes de commitear

```
☐ Unity compila sin errores ni warnings nuevos
☐ GameObject GameManager: HUD sin (Missing)
☐ Las referencias del Inspector apuntan a los objetos correctos
☐ Play: el reloj cuenta
☐ Play: agarrar un dino actualiza el contador
☐ Play: comer un muslo llena los segmentos de boost
☐ Play: el boost se vacía solo
☐ Play: Game Over apaga el HUD
☐ Play: Reintentar enciende el HUD
```

### Para las etapas que tocan UI por código

```
☐ Play directo en ESCENA_BIOMA_CUEVAS → el HUD aparece
☐ Play directo en ESCENA_BIOMA_DESIERTO → el HUD aparece
```

### Señales de alarma

| Señal | Causa probable |
|---|---|
| El contador no aparece | Referencia no arrastrada, o falta el fallback |
| Los segmentos no se encienden | `segmentosBoost` mal asignado |
| En Cuevas/Desierto no hay HUD | Falta la construcción por código (E4) |
| El HUD parpadea | Doble `Update` — el viejo y el nuevo |
| Faltan objetos en el Inspector | Campo serializado que no se re-arrastró |

---

## 8. Resumen

| Etapa | Script | Qué se separa | Dificultad |
|---|---|---|---|
| **E1** | `Temporizador` | HUD (contador + boost) | 🟢 Baja |
| **E2** | `Temporizador` | Reloj + tick de pánico | 🟢 Baja |
| **E3** | `Temporizador` | Paneles de fin de partida | 🔴 Alta |
| **E4** | `Temporizador` | UI construida por código | 🔴 Muy alta |
| **E5** | `Movimiento` | Estado de boost + animación | 🟡 Media |
| **E6** | `Movimiento` | Salud, muerte y sacrificio | 🟡 Media |
| **E7** | `Movimiento` | Control de la fila | 🔴 Alta |
| **E8** | `DinoSeguidor` | Matemáticas de la cadena | 🔴 Alta |
| **E9** | `TransicionManager` | Transporte de la fila | 🟡 Media |
| **E10** | `TransicionManager` | Persistencia del jugador | 🟡 Media |

**Total estimado:** 10 etapas, cada una con verificación completa y commit propio.

> **No hagas las 10 de una.** Hacé E1, verificá que el juego sigue exactamente igual, y solo entonces seguí. Si E1 sale mal, `git reset --hard pre-hud` y volvés al punto de partida sin haber roto nada más.

---

*Fin del MD 4. Ningún archivo de código fue modificado.*
*Tag de retorno: `migracion-completa`*