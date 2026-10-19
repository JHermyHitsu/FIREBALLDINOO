# MD 4 — Partición por Responsabilidad Única

> **Fecha:** 2026-10-19 (corregido)
> **Alcance:** los 4 scripts con múltiples responsabilidades
> **Estado:** propuesta corregida — requiere aprobación antes de ejecutar
> **Prerequisitos:** migración de carpetas completa (MD 2 + MD 3)
> **Tag de referencia:** `migracion-completa`
> **Rama de trabajo:** `docs/md4-correccion`

---

## 0. Revisión 2026-10-19 — qué se corrigió y por qué

Este documento se reescribió después de auditar el proyecto contra el código real.
La estructura, el diagnóstico y el orden de etapas se mantienen: **el análisis original
era correcto en lo esencial**. Lo que estaba mal era la *premisa* sobre cómo se comporta
la UI al cambiar de escena.

| # | Hallazgo | Dónde estaba el error | Corrección aplicada |
|---|---|---|---|
| C1 | El HUD serializado de Bosque **viaja** a Desierto y Cuevas | §3.2 daba por hecho que Cuevas/Desierto usan el camino de código | §1 y §4.2: se documenta la topología real y se corrige el diagnóstico |
| C2 | «Play directo en Cuevas/Desierto» **no es un escenario de prueba** | §6 paso 10 y §7 lo tomaban como verificación obligatoria | §4.4 y §9: se documenta por qué no se puede ejecutar y cuál es el escenario real |
| C3 | E1 y E4 estaban acopladas | §6 ponía la construcción por código en E4 pero la exigía en E1 | §8: E1 queda autocontenida, con su propio fallback |
| C4 | Los pasos de E1 estaban incompletos | §6 paso 7 decía «3 campos» (son 5) y faltaba `Reintentar()` | §8: pasos y call sites completos |
| C5 | «Re-arrastrar en las 3 escenas» | §3.1 | §4.1: `Movimiento` y `DinoSeguidor` viven en **prefabs**, se editan 1 vez |
| C6 | Contagem de líneas y referencias | §1 decía 754 líneas (son 756) | §2 y §3: números actualizados contra el código |
| C7 | `Morir(bool esPrecipicio)`: el MD afirmaba que nunca lo lee, y es cierto, pero el caller **sí cree que significa algo** | §1 | §2: se documenta el riesgo de la firma engañosa |
| C8 | `DinoSeguidor` consume **5** miembros de `Movimiento`, no 4 | §1 | §2 |
| C9 | Rollback con `git reset --hard` sobre `main` | §8 | §10: rama + punto de control, sin `reset --hard` |
| C10 | **No hay red de seguridad automática** (0 tests, 0 CI, 0 asmdef) | No estaba en el documento | §11: riesgo de fondo y cómo se mitiga |

### Bugs vivos detectados durante la auditoría

No forman parte del refactor de responsabilidad única, pero **E3 y E4 los tocan de lleno**.
Por eso se agrega la etapa **F1** (§7) como prerrequisito.

| # | Bug | Evidencia | Gravedad |
|---|---|---|---|
| **B1** | En el camino de UI por código, **se apaga el canvas entero** | `Temporizador.cs:59-63` apaga el padre de los segmentos; en el camino por código ese padre **es el canvas** | 🔴 Crítico |
| **B2** | `PanelVictoria` de Cuevas no tiene hijo `TextoVictoria` → se muestra sin mensaje | `ESCENA_BIOMA_CUEVAS.unity` (`Canvas > PanelVictoria` → `ImagenVictoria`, `BotonReintentar`, `BotonSalir`) | 🔴 Alto |
| **B3** | `BuscarHijoRecursivo(root, "PanelGameOver")` es **código muerto** | Ninguna escena tiene un GO con ese nombre; en Bosque se llama `PanelGamerOver` (con «r») | 🟡 Medio |
| **B4** | El GameOver construido por código tiene el **mensaje vacío** | `textoMensaje.text` no se asigna nunca (`Temporizador.cs:336-338`) | 🟡 Medio |
| **B5** | `DinoSeguidor.LimpiarHistorial()` está **vacío** pero se llama como si limpiara | `DinoSeguidor.cs:202` | 🟢 Bajo |

---

## 1. Topología real del proyecto (la base que faltaba)

Antes de partir nada hay que entender **quién está en cada escena y qué sobrevive al
cambio de escena**. Casi todos los errores de este refactor vienen de no tener esto claro.

### 1.1 Qué hay en cada escena

| | Bosque | Desierto | Cuevas |
|---|---|---|---|
| `GameManager` + `Temporizador` | ✅ en la escena | ❌ llega por persistencia | ❌ llega por persistencia |
| `Canvas` del HUD | ✅ `GameManager > Canvas` | ❌ (viaja el de Bosque) | ❌ propio, **sin `TextoVictoria`** |
| Jugador (`Dino_Jugador.prefab`) | ✅ | ❌ (viaja) | ❌ (viaja) |
| `TransicionManager` | ✅ | ❌ (viaja) | ❌ (viaja) |
| `MetaFinal` | ❌ | ❌ | ✅ |
| `SalidaNivel` | ✅ | ✅ | ❌ |
| `PanelVictoria` | ❌ (se crea por código) | ❌ (se crea por código) | ✅ (de escena) |

**Flujo real:** `MENU → BOSQUE → DESIERTO → CUEVAS`.
El jugador **solo existe en Bosque** (es el único lugar donde está instanciado el prefab
`Dino_Jugador`). Desierto y Cuevas no lo tienen: lo reciben por `DontDestroyOnLoad`.

### 1.2 Por qué la UI de Bosque sigue viva en Desierto y Cuevas

`Temporizador.Awake()` llama `DontDestroyOnLoad(gameObject)` sobre el `GameManager`.
**`DontDestroyOnLoad` arrastra toda la jerarquía de hijos.** Y el `Canvas` del HUD es un
hijo del `GameManager`:

```
GameManager > Canvas > TextoTiempo
GameManager > Canvas > ContadorAmigos
GameManager > Canvas > SegmentosBoost > BoostSeg1..3
GameManager > Canvas > PanelGamerOver
GameManager > Canvas > PanelPerder
```

→ **El HUD serializado de Bosque viaja entero a Desierto y a Cuevas.**

El camino de UI por código (`ConstruirUIPersistente`, `AsegurarContadorAmigos`,
`AsegurarSegmentosBoost`) **solo corre cuando el `GameManager` se crea por código**, es
decir en Play directo sobre una escena sin manager. En el flujo normal no corre.

### 1.3 Consecuencia directa sobre el refactor

Partir `Temporizador` **no** requiere reconstruir la UI en las otras escenas: la jerarquía
viaja sola. Lo que hay que cuidar es **no romper el viaje**: los campos serializados de
`Temporizador` apuntan a objetos que ahora viven bajo un `Canvas` que también viaja. Si se
mueven a un componente que **no** está en el `GameManager`, las referencias se pierden al
cambiar de escena.

> **Regla para todas las etapas:** el componente nuevo que reciba campos serializados
> **debe ir en el `GameManager`**, no en un GameObject de la escena.

---

## 2. Diagnóstico — Responsabilidad Única (S de SOLID)

| Script | Ubicación actual | Líneas | Responsabilidades | Peor caso |
|---|---|---|---|---|
| `Temporizador` | `Sistema/` | 756 | **14** | ❌ Peor del proyecto |
| `Movimiento` | `Gameplay/` | 181 | **10** | ❌ |
| `TransicionManager` | `Sistema/` | 340 | **8** | ❌ |
| `DinoSeguidor` | `Gameplay/` | 202 | **6** | ❌ |

### Otros principios (contexto, no en alcance)

| Principio | Estado | Nota |
|---|---|---|
| **O** — Abierto/Cerrado | ❌ | Cada panel nuevo obliga a tocar `Temporizador` |
| **L** — Liskov | ⚠️ | Sin herencia, pero `Morir(bool esPrecipicio)` es una **firma engañosa**: el parámetro no se lee en el cuerpo (`Movimiento.cs:134-174`) y sin embargo `ZonaMuerte.cs:24` lo pasa como `esPrecipicio: true`, dando a entender que significa algo. La corrección es **borrar el parámetro** (2 call sites), no implementarlo |
| **I** — Segregación | ❌ | Cero interfaces. `DinoSeguidor` depende de `Movimiento` entero: consume **5** miembros de 3 tipos distintos — `AgregarSeguidor()` (`DinoSeguidor.cs:78`), `seguidores` (L124, L126), `TieneBoost()` (L161), `EstaEnSuelo()` (L163), `sonidoMuerte` (L178) |
| **D** — Dependencias | ❌ | Depende de clases concretas y **busca GameObjects por nombre**: 19 llamadas a `transform.Find` / `BuscarHijoRecursivo` / `GameObject.Find` en `Temporizador.cs` y `TransicionManager.cs` |

> Este MD ataca **solo S**. Los otros tres requieren decisiones de diseño que van más allá
> de partición.

---

## 3. El inventario de responsabilidades

### 3.1 `Temporizador` — 14 responsabilidades

| # | Responsabilidad | Ubicación | Riesgo al mover |
|---|---|---|---|
| 1 | Reloj (cuenta, formatea `mm:ss`) | `Update` L470-475 | 🟢 Bajo |
| 2 | Color rojo neón bajo 15s | `Update` L477-481 | 🟢 Bajo |
| 3 | Tick de pánico (escala/rotación/offset) | `Update` L484-529 | 🟡 Medio |
| 4 | Contador de amigos + animación pop | `Update` L531-555 | 🟡 Medio |
| 5 | Barra de boost (`fillAmount`) | `Update` L557-571 | 🟢 Bajo |
| 6 | Segmentos de boost (estilo Subway) | `Update` L573-599 | 🟡 Medio |
| 7 | Temblor de cámara bajo 16s | `Update` L600-608 | 🟢 Bajo |
| 8 | Panel Game Over | `MostrarGameOver` L618 | 🔴 Alto |
| 9 | Panel Perder (todos caídos) | `MostrarGameOver` L618 | 🔴 Alto |
| 10 | Panel Victoria | `MostrarVictoria` L663 | 🔴 Alto |
| 11 | Construcción de UI persistente **por código** | `ConstruirUIPersistente` L296-373 | 🔴 Muy alto |
| 12 | Reconexión de UI al cambiar de escena | `AlCargarEscena` L74-135 | 🔴 Muy alto |
| 13 | Reconexión de botones de paneles | `ReconectarBotonesPaneles` L138-146 | 🟡 Medio |
| 14 | AudioSource de derrota | `sonidoPerder` | 🟢 Bajo |

### 3.2 `Movimiento` — 10 responsabilidades

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

### 3.3 `TransicionManager` — 8 responsabilidades

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

### 3.4 `DinoSeguidor` — 6 responsabilidades

| # | Responsabilidad | Riesgo al mover |
|---|---|---|
| 1 | Auto-reclutamiento al tocar al jugador | 🟡 Medio |
| 2 | Matemáticas de seguimiento en cadena | 🔴 Alto |
| 3 | Animación (velocidad, suelo, flip) | 🟡 Medio |
| 4 | Sacrificio | 🔴 Alto |
| 5 | Muerte + destrucción | 🟡 Medio |
| 6 | Audio | 🟢 Bajo |

---

## 4. Las tres trampas de este refactor

### 4.1 Campos serializados — pérdida silenciosa de datos

`Temporizador` tiene estos campos serializados:

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
# ESCENA_BIOMA_BOSQUE.unity (líneas 888-903)
tiempoInicial: 210
textoTiempo: {fileID: 1037321432}
textoContadorAmigos: {fileID: 11367469}
barraBoost: {fileID: 0}
```

Si movés un campo a otro componente, **Unity no tira error: pierde la referencia en
silencio**. El síntoma es un panel que no aparece, y la causa real está a metros del error.

> **Regla:** cada campo que se mueve se tiene que re-arrastrar **a mano**.

**Dónde se re-arrastra — corregido:**

| Componente | Vive en | Lugares a editar |
|---|---|---|
| `Temporizador` | Escena Bosque (no es prefab) | **1** — la escena |
| `Movimiento` | `Assets/Prefab/Dino_Jugador.prefab` | **1** — el prefab (propaga a las 3 escenas) |
| `DinoSeguidor` | `Assets/Prefab/Dino_rojo.prefab`, `Amigo_dino_azul.prefab` | **2** — los prefabs |

El riesgo de pérdida silenciosa es el mismo, pero el trabajo es **la mitad** de lo que
asumía la versión anterior de este documento.

> **Regla adicional (§1.3):** el componente que recibe los campos va en el `GameManager`.
> Si va en otro GameObject de la escena, la referencia se pierde al cambiar de escena.

### 4.2 El doble camino de UI — lo que realmente está duplicado

La versión anterior de este documento trataba «HUD por inspector» vs «HUD por código» como
el doble camino. **No es así:** el HUD serializado viaja con el `GameManager` (§1.2), así
que en el flujo real solo hay **un** camino.

Lo que **sí** está duplicado es el **Panel Victoria**:

| Panel | Instancia | Dónde |
|---|---|---|
| Victoria | Por código (`ConstruirPanelVictoria` L414) | Bajo el `Canvas` persistente |
| Victoria | De escena | `ESCENA_BIOMA_CUEVAS.unity` → `Canvas > PanelVictoria` |

`MostrarVictoria()` (L691-699) prefiere siempre el de la escena. En Cuevas ese panel
**no tiene hijo `TextoVictoria`** → el mensaje no aparece (bug **B2**, §0).

### 4.3 La trampa que no estaba: el nombre del panel

`AlCargarEscena` busca el panel por nombre exacto:

```csharp
var pg = BuscarHijoRecursivo(root, "PanelGameOver");   // Temporizador.cs:100
```

- **Ninguna escena** tiene un GameObject llamado `PanelGameOver`.
- En Bosque se llama **`PanelGamerOver`** (con «r»).
- El bucle recorre `GetActiveScene().GetRootGameObjects()` → **nunca ve la jerarquía
  persistente**, que es donde vive el `PanelGameOver` construido por código.

→ **Esa búsqueda nunca encuentra nada.** El panel sobrevive al cambio de escena gracias a
la referencia serializada y al `DontDestroyOnLoad`, no gracias a esta lógica.

### 4.4 «Play directo en Cuevas/Desierto» no es un escenario de prueba

Las escenas de Desierto y Cuevas **no tienen jugador**: el prefab `Dino_Jugador` (único con
tag `Player` y con `Movimiento`) está instanciado **solo en Bosque**.

| Prefab | Bosque | Cuevas | Desierto |
|---|---|---|---|
| `Dino_Jugador` (Movimiento) | 16 refs | **0** | **0** |
| `Amigo_dino_azul` (DinoSeguidor) | 12 | 0 | 0 |
| `Dino_rojo` (DinoSeguidor) | 0 | 0 | 12 |

En Play directo sobre esas escenas:

- No hay `Movimiento` → no hay input, no hay salto, no hay muerte.
- No hay `Temporizador` → no hay reloj, no hay HUD.
- `Temporizador.EnsureExists()` solo se dispara desde `MetaFinal`,
  `Movimiento.MostrarBotonMuerte` y `BotonGameOver` → **ninguno puede dispararse**.
- `SalidaNivel` → `TransicionManager.Transicion()` → `FindGameObjectWithTag("Player")`
  devuelve `null` → `Debug.LogError("[Transicion] no hay Player en escena.")` → la
  transición aborta.

**Conclusión: Play directo en Cuevas/Desierto es una escena estática, no jugable.** No es un
escenario de prueba válido, y además el camino de UI por código tiene el bug **B1**
(§0) que apaga el canvas entero.

**El escenario de prueba válido es el flujo real:** `Bosque → Desierto → Cuevas`, con el
jugador y el HUD viajando. Está en §9.

---

## 5. El bloqueo real: el singleton

```csharp
Temporizador.EnsureExists().MostrarVictoria();   // MetaFinal.cs:22
Temporizador.EnsureExists().MostrarGameOver(...); // Movimiento.cs:180
Temporizador.EnsureExists().Reintentar();         // BotonGameOver.cs:7
Temporizador.EnsureExists().Salir();              // BotonGameOver.cs:12
```

**4 llamadas desde afuera, todas piden el monolito entero.**

Mientras esas llamadas sigan siendo así, partir la clase es **cosmético**: los otros
archivos siguen necesitando un `Temporizador` con los 14 métodos.

> **Consecuencia:** este refactor es de **dos tiempos**.
> - **Tiempo 1:** crear los componentes nuevos y que el viejo les pase los datos.
> - **Tiempo 2:** cambiar los llamadores para que usen el componente específico.
>
> Sin el tiempo 2, el trabajo del tiempo 1 queda a medias.

`TransicionManager` tiene su propio par: `EnsureExists()` desde `SalidaNivel.cs:25` y
desde `Temporizador.cs:747`.

---

## 6. Qué NO se hace aquí

- **No se tocan los principios O, L, I, D.** Requieren decisiones de diseño (interfaces,
  eventos, tags) que van más allá de partición.
- **No se arreglan los bugs B1-B5** en las etapas E1-E10. Se arreglan en **F1** (§7),
  antes, porque E3 y E4 los tocan.
- **No se crean tests.** El proyecto no tiene infraestructura de tests (0 `asmdef`, 0 CI).
  Ver §11.

---

## 7. Orden de ejecución

### 7.1 Prerrequisitos

| Etapa | Qué | Dificultad | Reversible |
|---|---|---|---|
| **F0** | Corregir este documento | 🟢 Baja | ✅ Sí |
| **F1** | Arreglar los bugs B1-B5 | 🟡 Media | ✅ Sí |

**F1 en detalle:**

| # | Bug | Arreglo | Verificación |
|---|---|---|---|
| B1 | El canvas se apaga en el camino por código | Los segmentos por código cuelgan de un contenedor `SegmentosBoost` propio, nunca del canvas | Play directo en Desierto → el HUD aparece |
| B2 | `PanelVictoria` de Cuevas sin texto | Agregar el hijo `TextoVictoria` a la escena, **o** renombrar el que se crea por código para que ambos usen el mismo nombre | Llegar a la meta en Cuevas → se ve el mensaje |
| B3 | `PanelGamerOver` vs `"PanelGameOver"` | Renombrar el GO de Bosque a `PanelGameOver` (1 objeto) o corregir la constante de búsqueda | Cargar Bosque → el panel sigue funcionando |
| B4 | GameOver por código sin mensaje | Asignar `textoMensaje.text` en `ConstruirUIPersistente` | Play directo → el panel muestra un mensaje |
| B5 | `LimpiarHistorial()` vacío | Borrarlo y quitar sus 2 llamadas (`Temporizador.cs:746`, `TransicionManager.cs:307`) | Compila y el juego no cambia |

> **B3 tiene dos salidas válidas** y la elección cambia el alcance de E3. Decidir antes
> de arrancar E3: renombrar el objeto de la escena (1 cambio, pero toca la escena) o
> centralizar el nombre en una constante (1 cambio en código, más robusto).

### 7.2 Etapas del refactor

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
| **E10** | `PersistenciaJugador` fuera de `TransicionManager` | 🔴 Alta | ⚠️ Parcial |

**Corrección respecto de la versión anterior:** E10 estaba como 🟡 Media. No lo es.
`TransicionManager.AlCargarEscena` (L135-180) contiene la lógica de duplicados de
jugador, la adopción del jugador nuevo y la destrucción de la fila vieja — es la pieza más
delicada del archivo. **E10 es de riesgo alto.**

---

## 8. Etapa E1 — `HUD` fuera de `Temporizador` (la primera)

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
- `ConstruirUIPersistente` → es de E4
- `tiempoRestante` → **queda en `Temporizador`**; `HUD` solo lo lee
- **`AsegurarContadorAmigos` y `AsegurarSegmentosBoost` → se mueven a `HUD`** (ver abajo)

> **Cambio respecto de la versión anterior de este documento:** antes decía
> «Todo el código de construcción por código → es de E4». Eso hacía E1 **imposible de
> verificar**: el paso 10 exigía que el HUD apareciera en Cuevas/Desierto, y ese HUD lo
> construía justamente el código que se postergaba a E4.
>
> **Ahora E1 se lleva su propio fallback.** Son ~60 líneas: las dos funciones que ya
> existen (`Temporizador.cs:228-263` y `L267-294`) se trasladan tal cual al `HUD`, con
> una corrección (bug B1). E4 después se ocupa del resto del UI por código.

### El nuevo componente

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Lo que se ve en pantalla mientras se juega: contador de amigos, barra de boost
/// y segmentos. No sabe nada del reloj ni de los paneles de fin de partida.
///
/// Lee el estado de Movimiento, no lo modifica.
/// Si la escena no trae el HUD serializado, se construye a si mismo.
///
/// IMPORTANTE: los segmentos cuelgan SIEMPRE de un contenedor propio
/// ("SegmentosBoost"). Si colgaran directo del canvas, apagar el contenedor
/// apagaria el canvas entero y con el todo el HUD.
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
        AsegurarUI();
    }

    void Update()
    {
        ActualizarContadorAmigos();
        ActualizarBarraBoost();
        ActualizarSegmentosBoost();
    }

    // --- Construccion de respaldo ------------------------------------------

    // Si la escena no trae los objetos, se construyen aqui.
    void AsegurarUI()
    {
        Transform canvasT = ResolverCanvas();

        if (textoContadorAmigos == null)
            ConstruirContadorAmigos(canvasT);

        if (segmentosBoost == null || segmentosBoost.Length == 0)
            ConstruirSegmentosBoost(canvasT);

        ApagarSegmentos();
    }

    // Prioridad: canvas de una referencia ya asignada -> canvas existente bajo
    // este GameObject (el del Temporizador) -> canvas nuevo propio.
    Transform ResolverCanvas()
    {
        if (textoContadorAmigos != null)
            return textoContadorAmigos.transform.parent;

        if (segmentosBoost != null && segmentosBoost.Length > 0 && segmentosBoost[0] != null)
            return segmentosBoost[0].transform.parent;

        var existente = GetComponentInChildren<Canvas>(true);
        if (existente != null) return existente.transform;

        var canvasGO = new GameObject("CanvasHUD");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<GraphicRaycaster>();
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        return canvasGO.transform;
    }

    void ConstruirContadorAmigos(Transform canvasT)
    {
        if (canvasT == null) return;

        // Si la escena ya tiene un "ContadorAmigos", se reusa en vez de duplicarlo.
        var previo = canvasT.Find("ContadorAmigos");
        if (previo != null)
        {
            var img = previo.GetComponent<TextMeshProUGUI>();
            if (img != null) { textoContadorAmigos = img; return; }
        }

        var contadorGO = new GameObject("ContadorAmigos");
        contadorGO.transform.SetParent(canvasT, false);
        var rt = contadorGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -40f);
        rt.sizeDelta = new Vector2(120f, 50f);

        textoContadorAmigos = contadorGO.AddComponent<TextMeshProUGUI>();
        textoContadorAmigos.alignment = TextAlignmentOptions.Left;
        textoContadorAmigos.fontSize = 36;
        textoContadorAmigos.color = Color.white;
        textoContadorAmigos.text = "0";
    }

    void ConstruirSegmentosBoost(Transform canvasT)
    {
        if (canvasT == null) return;

        // Contenedor propio: los segmentos NUNCA cuelgan directo del canvas.
        var contGO = canvasT.Find("SegmentosBoost");
        if (contGO == null)
        {
            var nuevo = new GameObject("SegmentosBoost");
            nuevo.transform.SetParent(canvasT, false);
            var rtCont = nuevo.AddComponent<RectTransform>();
            rtCont.anchorMin = new Vector2(0f, 0f);
            rtCont.anchorMax = new Vector2(0f, 0f);
            rtCont.anchoredPosition = new Vector2(20f, 10f);
            rtCont.sizeDelta = new Vector2(100f, 20f);
            contGO = nuevo.transform;
        }

        segmentosBoost = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            // Reusar el segmento que ya exista en la escena.
            var previo = contGO.Find("BoostSeg" + (i + 1));
            var imgPrevio = previo != null ? previo.GetComponent<Image>() : null;
            if (imgPrevio != null) { segmentosBoost[i] = imgPrevio; continue; }

            var segGO = new GameObject("BoostSeg" + (i + 1));
            segGO.transform.SetParent(contGO, false);
            var rt = segGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(i * 30f, 0f);
            rt.sizeDelta = new Vector2(10f, 20f);
            rt.pivot = new Vector2(0f, 0f);

            var img = segGO.AddComponent<Image>();
            img.color = Color.white;
            img.type = Image.Type.Simple;
            segmentosBoost[i] = img;
        }
    }

    // Apaga los segmentos. Guarda contra el caso en que esten colgados del canvas:
    // apagar el padre en ese caso apagaria el HUD entero (bug B1).
    void ApagarSegmentos()
    {
        if (segmentosBoost == null) return;

        foreach (var s in segmentosBoost)
            if (s != null) s.gameObject.SetActive(false);

        if (segmentosBoost.Length == 0 || segmentosBoost[0] == null) return;
        Transform padre = segmentosBoost[0].transform.parent;
        if (padre == null) return;
        if (padre.GetComponent<Canvas>() != null) return; // el padre ES el canvas: no lo apagues
        padre.gameObject.SetActive(false);
    }

    // --- Actualizacion por frame -------------------------------------------

    // Muestra cuantos dinos te siguen. El pop escala cuando el numero cambia.
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

    // La barra se vacia sola mientras el boost sigue activo.
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
        if (activos > total) activos = total;

        for (int i = 0; i < total; i++)
            if (segmentosBoost[i] != null)
                segmentosBoost[i].gameObject.SetActive(i < activos);

        // El contenedor se enciende solo si queda algun segmento.
        if (segmentosBoost[0] != null)
        {
            Transform padre = segmentosBoost[0].transform.parent;
            if (padre != null && padre.GetComponent<Canvas>() == null)
                padre.gameObject.SetActive(activos > 0);
        }
    }

    // --- API para el Temporizador ------------------------------------------

    // Lo llaman MostrarGameOver y MostrarVictoria para apagar el HUD.
    public void Apagar()
    {
        if (textoContadorAmigos != null) textoContadorAmigos.gameObject.SetActive(false);
        if (barraBoost != null) barraBoost.gameObject.SetActive(false);
        ApagarSegmentos();
    }

    // Lo llama Reintentar para restaurar el HUD.
    // Los segmentos quedan apagados: se encienden con el boost.
    public void Encender()
    {
        if (textoContadorAmigos != null) textoContadorAmigos.gameObject.SetActive(true);
        if (barraBoost != null) barraBoost.gameObject.SetActive(true);
    }

    // Lo llama Reintentar para dejar las animaciones en su estado inicial.
    public void Reiniciar()
    {
        ultimoCountAmigos = 0;
        progresoPopAmigos = 1f;
        ApagarSegmentos();
        Encender();
    }
}
```

### Cómo se conecta con `Temporizador`

El `HUD` va **en el `GameManager`** (§1.3), así que sus referencias viajan con la jerarquía
persistente.

```csharp
// En Temporizador: nueva referencia (NO serializada, se resuelve en runtime)
HUD hud;

// En Awake, DESPUES de AsegurarUI() para que el canvas persistente ya exista
// cuando el HUD tenga que construirse a si mismo.
hud = GetComponent<HUD>();
if (hud == null) hud = gameObject.AddComponent<HUD>();
```

Y los call sites cambian:

| Antes | Después |
|---|---|
| `textoContadorAmigos.gameObject.SetActive(false)` | `hud.Apagar()` |
| `barraBoost.gameObject.SetActive(false)` | `hud.Apagar()` |
| `segmentosBoost[i].gameObject.SetActive(false)` | `hud.Apagar()` |
| `if (textoContadorAmigos != null) textoContadorAmigos.gameObject.SetActive(true)` | `hud.Encender()` |
| `if (barraBoost != null) barraBoost.gameObject.SetActive(true)` | `hud.Encender()` |
| `ultimoCountAmigos = 0; progresoPopAmigos = 1f;` (en `Reintentar`) | `hud.Reiniciar()` |

**Los bloques de `Update` del contador y del boost se eliminan de `Temporizador`** — ahora
viven en `HUD.Update()`.

**Y se elimina de `Temporizador`:**

- El bloque de `Awake` L54-64 (apagado inicial de los segmentos) → pasa a `HUD.Awake()`.
- `AsegurarContadorAmigos()` (L228-263) → pasa a `HUD`.
- `AsegurarSegmentosBoost()` (L267-294) → pasa a `HUD`.
- Las 2 llamadas a `AsegurarContadorAmigos()` / `AsegurarSegmentosBoost()` dentro de
  `AsegurarUI()` (L220-221).

### Los pasos

| # | Paso | Verificación |
|---|---|---|
| 1 | Crear la rama `refactor/hud` desde `main` y el punto de control `backup/pre-hud` | `git branch` los muestra |
| 2 | Crear `Assets/Scripts/UI/HUD.cs` | Compila |
| 3 | Agregar `HUD` al GameObject `GameManager` de **Bosque** (en el Inspector) | Sin errores |
| 4 | Arrastrar `ContadorAmigos` → `textoContadorAmigos` | El Inspector muestra la referencia |
| 5 | Arrastrar los 3 `BoostSeg` → `segmentosBoost` | Ídem |
| 6 | `barraBoost` queda en null (no existe en la escena) | Ídem |
| 7 | Borrar los **5** campos de `Temporizador.cs` (no 3) | Compila |
| 8 | Borrar los bloques de `Update` (contador, barra, segmentos) y el bloque de `Awake` L54-64 | Compila |
| 9 | Borrar `AsegurarContadorAmigos` y `AsegurarSegmentosBoost` de `Temporizador` y sus 2 llamadas | Compila |
| 10 | Reemplazar los call sites por `hud.Apagar()` / `hud.Encender()` / `hud.Reiniciar()` | Compila |
| 11 | **Probar el flujo real: Bosque → Desierto → Cuevas** | Checklist de §9 |
| 12 | Commit | — |

> **Sobre los pasos 4 y 5:** son la vía recomendada, pero **no son obligatorios**. El
> `HUD` reusa por nombre los objetos que ya están en la escena (`ContadorAmigos`,
> `SegmentosBoost > BoostSeg1..3`), así que si una referencia queda sin arrastrar, el
> componente la encuentra sola. Eso **no** reemplaza arrastrarlas: una referencia
> serializada sobrevive al cambio de escena; un `Find` por nombre solo funciona si el
> objeto existe en la jerarquía bajo el canvas correcto.

---

## 9. Verificación

### El escenario que importa: el flujo real

`Bosque → Desierto → Cuevas` con el jugador y el HUD viajando. **Este es el único escenario
que valida la etapa.** (Play directo no sirve: §4.4.)

| Momento | Qué mirar |
|---|---|
| En Bosque | Reloj contando, contador en 0, sin segmentos |
| Agarrar un dino | El contador sube a 1 y hace el pop |
| Comer un muslo | Se encienden los segmentos de boost y se van vaciando |
| Cruzar a Desierto | El reloj sigue, el contador sigue, el HUD no parpadea |
| Caminar por Desierto | El HUD sigue visible y actualizado |
| Cruzar a Cuevas | Ídem |
| Morir en Cuevas | Aparece el panel de Game Over **con mensaje** |
| Reintentar | Vuelve a Bosque con el reloj reseteado y el HUD encendido |

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
☐ Cruce a Desierto: el HUD sigue visible y no parpadea
☐ Cruce a Cuevas: el HUD sigue visible y no parpadea
```

### Para las etapas que tocan UI por código

```
☐ Play directo en ESCENA_BIOMA_CUEVAS → el HUD aparece (tras el arreglo de B1)
☐ Play directo en ESCENA_BIOMA_DESIERTO → el HUD aparece (tras el arreglo de B1)
```

> Aunque Play directo no es jugable (§4.4), **sigue siendo un test válido del camino de
> UI por código**: `Temporizador.EnsureExists()` lo dispara. Es la única forma de
> ejercitar esa rama.

### Señales de alarma

| Señal | Causa probable |
|---|---|
| El contador no aparece | Referencia no arrastrada, o falta el fallback |
| Los segmentos no se encienden | `segmentosBoost` mal asignado |
| En Cuevas/Desierto no hay HUD | Falta la construcción por código |
| El HUD parpadea | Doble `Update` — el viejo y el nuevo |
| **Desaparecen TODOS los elementos de la pantalla** | Un hijo del canvas se apagó. Revisar el padre de `segmentosBoost` (bug B1) |
| Faltan objetos en el Inspector | Campo serializado que no se re-arrastró |
| Al cruzar de escena pierde el HUD | El componente nuevo quedó fuera del `GameManager` (§1.3) |

---

## 10. Rollback

**No usar `git reset --hard`.** Descarta también lo que no está commiteado, y en `main`
reescribe historia local.

| Paso | Comando | Resultado |
|---|---|---|
| 1 | `git switch -c refactor/hud` | Rama de trabajo |
| 2 | `git branch backup/pre-hud` | Punto de control (una rama, no un tag) |
| 3 | ... trabajo ... | Cada etapa cierra con un commit |
| 4 | Volver atrás | `git switch main` + `git branch -D refactor/hud` |

Este repo ya usa la convención de ramas `backup/…` (`backup/antes-de-revertir-2026-10-19`,
`backup/pre-merge-121458`). El plan anterior proponía un tag `pre-hud` con
`git reset --hard`: se descarta.

---

## 11. El riesgo de fondo: no hay red de seguridad automática

Verificado en el repositorio:

- **0** `asmdef`, **0** carpetas de tests, **0** referencias a `UnityEngine.TestTools`.
- **No hay CI**: no existe `.github/`, `.gitlab-ci.yml` ni nada equivalente.
- `com.unity.test-framework: 1.6.0` está en `Packages/manifest.json` **y no lo usa nadie**.
- `Assembly-CSharp` no compila desde la línea de comandos: la única verificación posible
  es **abrir Unity y jugar**.

Todo el §9 es verificación manual. Eso obliga a dos cosas:

1. **Cada etapa necesita un punto de control en git** que se pueda verificar a mano en
   ≤ 2 minutos, con un checklist corto y una sola variable tocada.
2. **La línea de base del diff se compara contra `main`**, no contra el commit anterior:
   una etapa puede romper algo que venía de la anterior.

```bash
# Ver el diff acumulado de la rama contra el punto de control
git diff backup/pre-hud --stat
```

---

## 12. Resumen de etapas

| Etapa | Script | Qué se separa | Dificultad |
|---|---|---|---|
| **F1** | — | Bugs B1-B5 (previo) | 🟡 Media |
| **E1** | `Temporizador` | HUD (contador + boost) | 🟢 Baja |
| **E2** | `Temporizador` | Reloj + tick de pánico | 🟢 Baja |
| **E3** | `Temporizador` | Paneles de fin de partida | 🔴 Alta |
| **E4** | `Temporizador` | UI construida por código | 🔴 Muy alta |
| **E5** | `Movimiento` | Estado de boost + animación | 🟡 Media |
| **E6** | `Movimiento` | Salud, muerte y sacrificio | 🟡 Media |
| **E7** | `Movimiento` | Control de la fila | 🔴 Alta |
| **E8** | `DinoSeguidor` | Matemáticas de la cadena | 🔴 Alta |
| **E9** | `TransicionManager` | Transporte de la fila | 🟡 Media |
| **E10** | `TransicionManager` | Persistencia del jugador | 🔴 Alta |

**Total estimado:** 10 etapas de refactor + 1 etapa previa de bugs, cada una con
verificación completa y commit propio.

> **No hagas las 10 de una.** Hacé E1, verificá que el juego sigue exactamente igual
> (incluido el cruce a Desierto y Cuevas), y solo entonces seguí. Si E1 sale mal,
> `git switch main` y volvés al punto de partida sin haber roto nada más.
