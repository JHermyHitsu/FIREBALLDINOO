# MD 1 — Inventario de Scripts

> **Fecha:** 2026-10-18
> **Alcance:** `Assets/Scripts/` completo
> **Estado:** informational — no modifica código

---

## 1. Resumen general

| Métrica | Valor |
|---|---|
| Archivos en `Assets/Scripts/` | **15** |
| Líneas totales | **2012** |
| MonoBehaviours | 14 |
| Clases estáticas | 1 (`Escenas`) |
| Métodos públicos | 29 |
| Bindings `onClick` en todo el proyecto | **10** (5 métodos únicos) |

### Los 5 archivos más grandes

| # | Archivo | Líneas | % del proyecto |
|---|---|---|---|
| 1 | `Temporizador.cs` | **754** | 37.4% |
| 2 | `TransicionManager.cs` | 340 | 16.9% |
| 3 | `DinoSeguidor.cs` | 214 | 10.6% |
| 4 | `Movimiento.cs` | 188 | 9.3% |
| 5 | `ItemComida.cs` | 112 | 5.6% |

> `Temporizador.cs` solo ocupa más que los otros 14 archivos juntos.

---

## 2. Inventario archivo por archivo

### `Escenas.cs`
- **9 líneas** · clase estática · GUID `8b52e30680f5ac75f9782f08675d7a05`
- **Escenas:** ninguna (correcto, no se adjunta)
- **Qué hace:** centraliza nombres de escena como constantes.
- **Depende de:** nada
- **Problemas:** incompleto (faltan `Cuevas` y `Desierto`); `EscenaBosque` duplicada en `Temporizador.cs`

---

### `BotonGameOver.cs`
- **14 líneas** · MonoBehaviour · GUID `f2ee4094ec0b4f858b297b37bb0752ab`
- **Escenas:** `ESCENA_BIOMA_CUEVAS`
- **Qué hace:** adaptador delgado. Los botones de derrota llaman a `Temporizador.Reintentar()` / `.Salir()` a través suyo.
- **Depende de:** `Temporizador`
- **Problemas:** el mismo cableado existe de dos formas — en Bosque los botones apuntan directo a `Temporizador`, en Cueva pasan por este intermediario.

---

### `ZonaMuerte.cs`
- **26 líneas** · MonoBehaviour · GUID `15c5a560a02383a8695b5e16c095dded`
- **Escenas:** Bosque, Desierto ×2, Cuevas
- **Qué hace:** trigger de precipicio. Detecta caída y llama a `Movimiento.Morir()`.
- **Depende de:** `Movimiento`
- **Problemas:** `OnTriggerStay2D` es redundante (`Morir` ya se autoprotege); sin `OnValidate` que fuerce `isTrigger`, a diferencia de sus dos hermanos.

---

### `AutoDesactivar.cs`
- **31 líneas** · MonoBehaviour · GUID `90aaec04649892f9e945a846ba88497d`
- **Escenas:** Bosque
- **Qué hace:** utility genérica. Espera N segundos y destruye o desactiva.
- **Depende de:** nada
- **Problemas:** ninguno funcional. Es genérica y no depende del juego.

---

### `SalidaNivel.cs`
- **38 líneas** · MonoBehaviour · GUID `6f630322e2d062f4fb74060cbfa957e1`
- **Escenas:** Bosque (→ Desierto), Desierto (→ Cuevas)
- **Qué hace:** trigger al borde del bioma. Pide cambio de escena con fundido vía `TransicionManager`.
- **Depende de:** `TransicionManager`
- **Problemas:** `escenaDestino` / `spawnDestino` son strings crudos, no constantes de `Escenas`. Naming de spawn inconsistente (`Spawn_From_Bosque` vs `spawn_from_desierto`).

---

### `MetaFinal.cs`
- **39 líneas** · MonoBehaviour · GUID `550be70fd15d369b2829941baede4995`
- **Escenas:** Cuevas
- **Qué hace:** trigger de meta. Al entrar, muestra panel de victoria una vez.
- **Depende de:** `Temporizador`
- **Problemas:** `OnValidate` y `OnDrawGizmos` son **copias exactas** de los de `SalidaNivel`. Su campo `mensajeVictoria` no se usa (ver `Temporizador`).

---

### `Camara.cs`
- **46 líneas** · MonoBehaviour · GUID `852069f84f39a54ca9768996130cbc93`
- **Escenas:** Bosque, Desierto, Cuevas
- **Qué hace:** cámara 2D que sigue al jugador con suavizado, altura Y fija, adelanto en X y temblor de pantalla.
- **Depende de:** nadie (la llama `Temporizador`)
- **Problemas:** `personaje` es **null** en Desierto y Cuevas (solo `TransicionManager` la reconecta). `yFija` con valores inconsistentes entre escenas (2.68 / 0 / 7.1).

---

### `Pausa.cs`
- **60 líneas** · MonoBehaviour · GUID `31825672dce72f69e9132e044a672ebd`
- **Escenas:** Bosque
- **Qué hace:** menú de pausa con ESC. `Continuar`, `Reiniciar`, `Salir`.
- **Depende de:** `Escenas`, `SceneManagement`
- **Problemas:** `Reiniciar()` está **muerto** (verificado). `Salir()` duplica `Temporizador.Salir()`.

---

### `HoverBoton.cs`
- **63 líneas** · MonoBehaviour · GUID `c9d169e35dc205d06abcc75ca54a173b`
- **Escenas:** Bosque ×3, Menú ×1. **Además lo agrega en runtime** `Temporizador.AsegurarHoverBotones()` en Desierto/Cuevas.
- **Qué hace:** escala + tinte al hover, con early-out.
- **Depende de:** nada (la instancia en runtime depende de `Temporizador`)
- **Problemas:** doble fuente de verdad (serializado en unos botones, agregado por código en otros).

---

### `ComicIntro.cs`
- **80 líneas** · MonoBehaviour · GUID `254aad7479a9da01391e73ec5872e9e2`
- **Escenas:** `ESCENA_MENU_INICIO`
- **Qué hace:** slideshow de introducción y carga el primer bioma.
- **Depende de:** `Escenas`, `SceneManagement`
- **Problemas:** no null-checkea `imagenIntro` antes de usarlo en `IniciarComic`.

---

### `ItemComida.cs`
- **112 líneas** · MonoBehaviour · GUID `7953c919f07cac34f916eb0dcc1be290`
- **Escenas:** ninguna — **prefab `Muslo_Carne.prefab`** (82 en Bosque, 70 en Cuevas, 65 en Desierto)
- **Qué hace:** item de comida. Boost de velocidad + tutorial que muestra un hint en pantalla.
- **Depende de:** `Movimiento`, `Camera.main`
- **Problemas:** 5 responsabilidades en un archivo (pickup, audio, construcción de UI, tracking de posición, throttle global). Sus contadores `static` **nunca se resetean** al reintentar.

---

### `Movimiento.cs`
- **188 líneas** · MonoBehaviour · GUID `afc466930023d3a58b5bbc796ff83418`
- **Escenas:** ninguna — **prefab `Dino_Jugador.prefab`** (ausente en Desierto)
- **Qué hace:** controlador del jugador. Input, física, salto, suelo, animación, boost, fila de seguidores, muerte/sacrificio, game over.
- **Depende de:** `DinoSeguidor`, `Temporizador`
- **Problemas:** 10 responsabilidades. `ReiniciarNivel()` **muerto**. Sin null-guards en `rb`/`sr`/`col`. Parámetro `esPrecipicio` nunca leído.

---

### `DinoSeguidor.cs`
- **214 líneas** · MonoBehaviour · GUID `0f2bb92b29fce6fdabb4cb5093e63ec6`
- **Escenas:** ninguna — **prefabs `Amigo_dino_azul`, `Dino_rojo`**
- **Qué hace:** dinosaurios seguidores. Auto-recluta, sigue en cadena detrás del líder, se sacrifica.
- **Depende de:** `Movimiento`, `TransicionManager`, `Temporizador`
- **Problemas:** `CaerYMorir()` **muerto**. `LimpiarHistorial()` es **no-op con cuerpo vacío**. `pasosSeparacion` nunca se lee. `ReVincular` es alias de `Reclutar`.

---

### `TransicionManager.cs`
- **340 líneas** · MonoBehaviour singleton · GUID `548915dbcbdb162c0b74ef00657aee2b`
- **Escenas:** Bosque (Desierto y Cuevas lo crean por código con `EnsureExists()`)
- **Qué hace:** cambios de escena con fade, reubica jugador y seguidores, persiste el Player, administra un EventSystem único.
- **Depende de:** `Movimiento`, `DinoSeguidor`, `Camara`, `EventSystems`, `UI`
- **Problemas:** 8 responsabilidades. `separacionFila` (1.4) compite con `distanciaDetras` en `DinoSeguidor` (0.75). Su propio comentario dice que es "el único que debería llamar a SceneManager" pero `ComicIntro` lo ignora.

---

### `Temporizador.cs`
- **754 líneas** · MonoBehaviour singleton · GUID `aa2a752280a2a945db33373a0ae62eda`
- **Escenas:** Bosque (Desierto y Cuevas lo crean por código)
- **Qué hace:** reloj, HUD, contador de amigos, barra de boost, temblor de cámara, paneles de Game Over / Perder / Victoria, construcción de UI persistente por código, reconexión de UI de escena.
- **Depende de:** `Movimiento`, `DinoSeguidor`, `TransicionManager`, `Camara`, `HoverBoton`, `Escenas`
- **Problemas:** **14 responsabilidades — el archivo más problemático del proyecto.** `Update()` tiene ~150 líneas. `EscenaBosque` duplica `Escenas.BiomaBosque`. `AgregarTiempo()` muerto. `sonidoPerder` asignado en escena pero **nunca reproducido**. `barraBoost` sin objeto asignado ni fallback por código → rama muerta. `MostrarVictoria(mensaje)` **ignora su parámetro**.

---

## 3. Bindings reales del Inspector (verificados)

Estos son los **únicos** métodos que Unity puede llamar sin código C#:

| Método | Quién lo expone | Escenas |
|---|---|---|
| `Salir` | `Temporizador`, `Pausa`, `BotonGameOver` | Bosque ×3, Cuevas |
| `Reintentar` | `Temporizador`, `BotonGameOver` | Bosque ×2, Cuevas |
| `Continuar` | `Pausa` | Bosque |
| `AbrirMenu` | `Pausa` | Bosque |
| `IniciarComic` | `ComicIntro` | Menú |

> **Regla de verificación:** antes de borrar un método público, siempre verificar contra esta tabla. Un método puede no aparecer en ningún `grep` de C# y aun así estar cableado en el Inspector.

---

## 4. Código muerto confirmado (verificado por mí, no por análisis automático)

| Ítem | Ubicación | Evidencia |
|---|---|---|
| `Pausa.Reiniciar()` | `Pausa.cs:43` | Sin llamada C#, sin `m_MethodName` en ninguna escena/prefab |
| `DinoSeguidor.CaerYMorir()` | `DinoSeguidor.cs:201` | Cero ocurrencias fuera de su propia definición |
| `Temporizador.AggregarTiempo()` | `Temporizador.cs:707` | Cero ocurrencias fuera de su propia definición |
| `Movimiento.ReiniciarNivel()` | `Movimiento.cs:184` | Privado, cero llamadas |
| `DinoSeguidor.LimpiarHistorial()` | `DinoSeguidor.cs:213` | Cuerpo vacío `{ }` pero llamado desde `TransicionManager:307` y `Temporizador:744` |
| `DinoSeguidor.pasosSeparacion` | `DinoSeguidor.cs:23` | `[HideInInspector]`, nunca leído ni escrito |
| `Temporizador.sonidoPerder` | `Temporizador.cs:32` | Clip asignado en escena, `AudioSource` creado, `.Stop()` existe, **pero nunca hay `.Play()`** |
| `Temporizador.barraBoost` | `Temporizador.cs:20` | `{fileID: 0}` en Bosque, sin fallback por código → toda la rama que lo usa nunca corre |
| `Temporizador.textoMensaje` | `Temporizador.cs:28` | Configurado pero `.text` nunca se asigna; `{fileID: 0}` en Bosque |
| `Temporizador.imagenTiempoAgotado` | `Temporizador.cs:29` | `{fileID: 0}` en Bosque, no existe en ninguna escena |
| `Movimiento.Morir(esPrecipicio)` | `Movimiento.cs:135` | El parámetro nunca se lee en el cuerpo |

---

## 5. Lógica duplicada

| Duplicado | Ubicaciones | Tipo |
|---|---|---|
| `OnValidate` que fuerza `isTrigger` | `SalidaNivel.cs:14-19` ≡ `MetaFinal.cs:15-20` | **Idéntico byte a byte** |
| `OnDrawGizmos` de la cajita | `SalidaNivel.cs:29-37` ≡ `MetaFinal.cs:30-38` | Idéntico salvo el color |
| `"ESCENA_BIOMA_BOSQUE"` | `Escenas.cs:8` vs `Temporizador.cs:13` | Constante duplicada |
| `Salir()` al menú | `Pausa.cs:49-53` ≈ `Temporizador.cs:749-753` | Casi idéntico |
| Guard `CompareTag("Player")` | 5 archivos | Patrón repetido |
| Setup Rigidbody2D kinematic | `DinoSeguidor.cs:56-60` ≡ `DinoSeguidor.cs:94-99` | Casi idéntico |
| Bloque de ocultar boost | `Temporizador.cs` ×4 (L55-64, 632-641, 675-684, 722-731) | Repetido 4 veces en el mismo archivo |

---

## 6. Problemas que atraviesan todo el proyecto

### 6.1 Dos sistemas de UI en paralelo
`Temporizador` construye UI persistente **por código** cuando la escena no la trae. En Bosque la UI serializada gana, y por eso `barraBoost`, `textoMensaje` e `imagenTiempoAgotado` no hacen nada: los fallbacks que los llenarían nunca corren.

### 6.2 Acoplamiento por nombre de GameObject
14 nombres de objetos se buscan por texto (`BuscarHijoRecursivo` sobre todas las roots). Renombrar cualquiera en el editor rompe el juego **sin error de compilación**.

### 6.3 Desierto no es jugable standalone
Le falta: prefab del jugador, `Temporizador`, `TransicionManager`, `EventSystem`, y `Camara.personaje` es null. Solo se llega desde Bosque.

### 6.4 Cableado de Game Over inconsistente
Bosque: botones → `Temporizador` directo. Cuevas: botones → `BotonGameOver` → `Temporizador`. Misma UX, dos arquitecturas.

### 6.5 APIs deprecadas mezcladas
`FindObjectOfType<Canvas>()` (deprecado) en `ItemComida:45` convive con `FindFirstObjectByType` en otros. `rb.linearVelocity` (Unity 6) reemplaza a `velocity`.

### 6.6 Sin namespaces
Las 15 clases están en el namespace global, incluida `Camara`, que colisiona conceptualmente con `UnityEngine.Camera`.

---

## 7. Hallazgos NO accionables en esta fase

Se documentan pero **no se tocan** en la fase de migración:

- Los 14 responsabilidades de `Temporizador` (requiere redesign, no movimiento)
- Las 8 de `TransicionManager`
- Las 10 de `Movimiento`
- El sistema de UI dual (§6.1)
- El acoplamiento por nombres (§6.2)
- La inconsistencia de spacing de la fila

> Estos son **trabajo futuro**, no migración. Se anotan para que no se pierdan de vista.

---

*Fin del MD 1. Ningún archivo de código fue modificado.*