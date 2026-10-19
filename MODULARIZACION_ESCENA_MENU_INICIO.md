# Modularización — ESCENA_MENU_INICIO

> **Objetivo:** modularizar los scripts de la escena, quitar código innecesario, añadir comentarios justos, y que siga funcionando exactamente igual.
>
> **Principio:** cero over-engineering. Solo lo necesario, claro y comentado.

---

## 1. Estado actual — qué hay hoy

La escena tiene **3 scripts** (≈150 líneas totales) + un GameObject de Audio sin scripts:

| Script | Líneas | Qué hace | Estado real |
|--------|--------|----------|-------------|
| `HoverBoton.cs` | 42 | Escala + color de botón al hacer hover | ✅ Funciona, pero corre `Update()` todo el tiempo |
| `MenuInicial.cs` | 12 | Carga `ESCENA_BIOMA_BOSQUE` | ❌ **Código muerto** — nadie llama `IniciarJuego()` |
| `ComicIntro.cs` | 98 | Slideshow de 4 imágenes → carga escena | ✅ Funciona, pero tiene skip/next que NO están cableados |

### GameObjects clave en la escena

```
ESCENA_MENU_INICIO
├── Canvas
│   ├── Fondo (Image)
│   ├── Boton_Inicial → [Button] onClick = ComicIntro.IniciarComic()
│   └── ImagenIntro (Image, inactiva al inicio)
├── Gestion_Menu
│   ├── MenuInicial (MUERTO — sin uso)
│   └── ComicIntro (el que realmente trabaja)
├── Audio Source (orphan, nadie lo controla)
└── EventSystem
```

---

## 2. Problemas detectados (y qué hacer)

### 2.1 `MenuInicial` — código muerto ❌

**Problema:** está adjunto a `Gestion_Menu` pero el botón llama a `ComicIntro.IniciarComic()`. `MenuInicial.IniciarJuego()` nunca se ejecuta.

**Decisión:** **BORRARLO.** Si en el futuro lo necesitás, se recupera del git en 2 segundos. No tiene sentido mantener código que no hace nada.

---

### 2.2 `ComicIntro` — complejidad especulativa ⚠️

**Problema:** tiene `Saltar()` y `Siguiente()` pero:
- `botonSkip` está asignado a `null` en la escena
- No hay botones de navegación cableados
- Solo se usa `IniciarComic()`

**Decisión:** **Simplificar a lo que se usa:**
- Mantener `IniciarComic()`
- Mantener auto-avanzar con timer
- Mantener carga de escena al final
- **Sacar** `Saltar()` y `Siguiente()` (código muerto)
- **Sacar** el campo `botonSkip` (no se usa)

Si querés el skip después, lo cableás en la escena y lo volvés a añadir.

---

### 2.3 `HoverBoton` — gasta CPU al pedo ⚠️

**Problema:** el `Update()` corre TODOS los frames, siempre. Incluso cuando el botón no está en hover y ya llegó a su estado de reposo. En un menú con 1 botón no se nota, pero es un desperdicio.

**Decisión:** **Añadir early-out** — si ya estás en el estado objetivo (escala y color correctos), salirse del `Update()`. Solo volver a entrar cuando cambia `hover`.

---

### 2.4 Nombres de escena hardcodeados 🔴

**Problema:** el string `"ESCENA_BIOMA_BOSQUE"` está escripado en:
- `ComicIntro.escenaJuego`
- `MenuInicial.escenaJuego` (se va a borrar)
- `Pausa.Salir()`
- `Temporizador.Salir()`

Si algún día cambiás el nombre de la escena, hay que buscar en 4 lados.

**Decisión:** **Crear una clase estática `Escenas`** con las constantes:

```csharp
public static class Escenas
{
    public const string MenuInicio = "ESCENA_MENU_INICIO";
    public const string BiomaBosque = "ESCENA_BIOMA_BOSQUE";
}
```

Después reemplazar los strings hardcodeados por `Escenas.BiomaBosque` en `ComicIntro`, `Pausa` y `Temporizador`.

---

### 2.5 `ComicIntro` usa `SceneManager` directo ✅ (decisión correcta, sin cambios)

**Lo que parecía un problema no lo es.** A primera vista uno diría "el proyecto tiene `TransicionManager` y este script lo ignora". Pero al leer el código real:

```csharp
public void IrA(string escenaDestino, string spawnDestino)
{
    // ...
    if (jugadorPersistente == null)
    {
        Debug.LogError("[Transicion] no hay Player en escena.");
        yield break;   // aborta
    }
}
```

`TransicionManager` está diseñado para **transiciones dentro del juego**, donde ya existe un Player con `Movimiento` y una fila de seguidores que hay que reubicar. `ESCENA_MENU_INICIO` **no tiene Player**, así que usar `TransicionManager` abortaría la carga con `no hay Player en escena`.

**Decisión:** se mantiene `SceneManager.LoadScene(Escenas.BiomaBosque)`. Solo se centraliza el nombre de escena, nada más.

---

### 2.6 `Audio Source` orphan ⚠️

**Problema:** hay un `Audio Source` con loop que suena en `Awake()`, pero ningún script lo controla. Si en el futuro necesitás pausar o cambiar la música del menú, no hay forma.

**Decisión:** por ahora **dejarlo así** — cumple su función y no rompe nada. Si más adelante querés control de audio en el menú, se puede añadir un `AudioManager` o pasarlo por `TransicionManager`. No es prioridad para esta modularización.

---

### 2.7 `Gestion_Menu` mezcla responsabilidades ⚠️

**Problema:** `Gestion_Menu` tiene tanto `MenuInicial` (muerto) como `ComicIntro`. Son cosas distintas en el mismo GameObject.

**Decisión:**
- `MenuInicial` se borra → queda solo `ComicIntro` en `Gestion_Menu`
- Renombrar el GameObject de `Gestion_Menu` a `ComicIntro` (refleja lo que realmente es)
- O si preferís, directamente mueve `ComicIntro` a un GameObject propio

---

## 3. Plan paso a paso (lo que vamos a hacer)

### Paso 1 — Crear clase `Escenas` (nuevo archivo)

**Archivo nuevo:** `Assets/Scripts/Escenas.cs`

Contenido:
```csharp
/// <summary>
/// Nombres de escena centralizados.
/// Asi no hay strings escripados por todos lados.
/// </summary>
public static class Escenas
{
    public const string MenuInicio = "ESCENA_MENU_INICIO";
    public const string BiomaBosque = "ESCENA_BIOMA_BOSQUE";
}
```

---

### Paso 2 — Simplificar `ComicIntro` (reescribir)

**Qué cambia:**
- Sacar `Saltar()`, `Siguiente()`, `botonSkip`
- Usar `Escenas.BiomaBosque` en vez del string hardcodeado
- Mantener `SceneManager.LoadScene(...)` — el menú no tiene Player, `TransicionManager` aborta (ver §2.5)
- Añadir comentarios breves y claros en cada sección

**Qué queda igual:**
- `IniciarComic()` — idéntico
- El slideshow automático con timer — idéntico
- La referencia a `imagenIntro`, `imagenes`, `tiempoPorImagen` — idéntico

---

### Paso 3 — Optimizar `HoverBoton` (reescribir)

**Qué cambia:**
- Early-out en `Update()`: si ya estás en el estado objetivo, no hacés nada
- Añadir un flag `bool enReposo` para saber si el `Update` puede salir

**Qué queda igual:**
- La lógica de escala + color lerp — idéntica
- Los campos públicos (`escalaHover`, `velocidad`, `colorHover`) — idénticos
- El comentario de clase — mismo espíritu

---

### Paso 4 — Borrar `MenuInicial` (eliminar)

- Borrar el archivo `MenuInicial.cs`
- Borrar el componente `MenuInicial` del GameObject `Gestion_Menu` en la escena
- Borrar el `.meta` correspondiente (Unity lo hace solo al borrar el archivo, pero verificar)

---

### Paso 5 — Reemplazar strings hardcodeados en `Pausa` y `Temporizador`

**En `Pausa.cs`:**
```csharp
// ANTES:
SceneManager.LoadScene("ESCENA_MENU_INICIO");

// DESPUÉS:
SceneManager.LoadScene(Escenas.MenuInicio);
```

**En `Temporizador.cs`:**
```csharp
// ANTES:
SceneManager.LoadScene("ESCENA_MENU_INICIO");

// DESPUÉS:
SceneManager.LoadScene(Escenas.MenuInicio);
```

---

### Paso 6 — Limpiar la escena en Unity

- Borrar componente `MenuInicial` de `Gestion_Menu`
- Renombrar `Gestion_Menu` a `ComicIntro` (opcional pero recomendado)
- Verificar que `Boton_Inicial` sigue llamando a `ComicIntro.IniciarComic()` — no debería cambiar nada

---

## 4. Resumen de archivos

| Archivo | Acción | Motivo |
|---------|--------|--------|
| `Escenas.cs` | **CREAR** | Centralizar nombres de escena |
| `ComicIntro.cs` | **REESCRIBIR** | Sacar código muerto, usar `Escenas`, comentar |
| `HoverBoton.cs` | **REESCRIBIR** | Early-out en Update, comentar |
| `MenuInicial.cs` | **BORRAR** | Código muerto, nadie lo llama |
| `Pausa.cs` | **EDITAR** | Reemplazar string hardcodeado por `Escenas.MenuInicio` |
| `Temporizador.cs` | **EDITAR** | Reemplazar string hardcodeado por `Escenas.MenuInicio` |

---

## 5. Fuera de alcance (NO hacemos ahora)

- **Audio Manager** para controlar la música del menú
- **Reestructurar `TransicionManager`** (ya está bien)
- **Añadir skip/next al comic** (si lo querés, lo cableás en Unity y lo reimplementás)
- **Internacionalización de textos** (no hay textos en la escena)
- **Tests unitarios** (el proyecto no tiene framework de tests)

---

## 6. Verificación post-modularización

Después de cada paso, verificar en Unity:

1. ✅ La escena `ESCENA_MENU_INICIO` abre sin errores en consola
2. ✅ El botón JUGAR inicia el comic (4 imágenes, 3 seg cada una)
3. ✅ Al terminar el comic, carga `ESCENA_BIOMA_BOSQUE`
4. ✅ El hover del botón escala + cambia color correctamente
5. ✅ La música del menón sigue sonando
6. ✅ No hay errores de compilación ni referencias rotas

---

## 7. Estado final — ✅ COMPLETADO (2026-10-18)

| Paso | Estado |
|---|---|
| 1 — Crear `Escenas.cs` | ✅ Completado |
| 2 — Simplificar `ComicIntro.cs` | ✅ Completado |
| 3 — Optimizar `HoverBoton.cs` | ✅ Completado |
| 4 — Borrar `MenuInicial.cs` | ✅ Completado |
| 5 — `Pausa.cs` / `Temporizador.cs` | ✅ Completado |
| 6 — Limpiar escena en Unity | ✅ Completado |
| 7 — Verificar en Play | ✅ Botón JUGAR, comic y carga de escena funcionando |

**Nota de arquitectura:** `ComicIntro` mantiene `SceneManager.LoadScene` a propósito. `TransicionManager.IrA()` aborta si no encuentra un Player, y el menú no tiene uno (ver §2.5).
