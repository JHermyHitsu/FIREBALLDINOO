# MD 2 — Estructura de Carpetas

> **Fecha:** 2026-10-18
> **Alcance:** reorganización física de `Assets/Scripts/`
> **Estado:** propuesta — requiere tu aprobación antes de ejecutar
> **Prerequisito:** MD 1 (inventario)

---

## 1. Por qué reorganizar

Hoy `Assets/Scripts/` es una carpeta plana con 15 archivos mezclados. No está *mal*, pero **no ayuda a encontrar nada**. Un script de UI vive al lado de un manager de escenas, al lado de un controlador de jugador, sin ninguna señal de a qué pertenece.

Esto es lo que produce el efecto contrario de lo que busca un proyecto organizado: hay que abrir cada archivo para saber qué es.

---

## 2. Estructura propuesta

```
Assets/Scripts/
├── Escenas.cs                       ← constantes (raíz, sin subcarpeta)
│
├── UI/
│   ├── ComicIntro.cs                ← menú de inicio, slideshow
│   ├── HoverBoton.cs                ← efecto hover de botones
│   └── Pausa.cs                     ← menú de pausa
│
├── Gameplay/
│   ├── Movimiento.cs                ← controlador del jugador
│   ├── DinoSeguidor.cs              ← dinosaurios seguidores
│   ├── ItemComida.cs                ← item de comida
│   ├── ZonaMuerte.cs                ← trigger de precipicio
│   ├── MetaFinal.cs                 ← trigger de meta
│   ├── SalidaNivel.cs               ← trigger de cambio de bioma
│   └── Camara.cs                    ← cámara que sigue al jugador
│
├── Sistema/
│   ├── Temporizador.cs              ← reloj + HUD + paneles de fin
│   └── TransicionManager.cs         ← cambios de escena con fade
│
└── Utilidades/
    ├── AutoDesactivar.cs            ← temporizador de vida de objetos
    └── BotonGameOver.cs             ← adaptador de botones de derrota
```

---

## 3. Tabla de asignación

| Archivo actual | Carpeta destino | Razón |
|---|---|---|
| `Escenas.cs` | *(raíz)* | Constantes transversales, no pertenece a un dominio |
| `ComicIntro.cs` | `UI/` | Controla pantalla de introducción |
| `HoverBoton.cs` | `UI/` | Efecto visual de botones |
| `Pausa.cs` | `UI/` | Menú de pausa |
| `Movimiento.cs` | `Gameplay/` | Lógica del jugador |
| `DinoSeguidor.cs` | `Gameplay/` | Entidades que siguen al jugador |
| `ItemComida.cs` | `Gameplay/` | Item del mundo |
| `ZonaMuerte.cs` | `Gameplay/` | Trigger de gameplay |
| `MetaFinal.cs` | `Gameplay/` | Trigger de gameplay |
| `SalidaNivel.cs` | `Gameplay/` | Trigger de gameplay |
| `Camara.cs` | `Gameplay/` | Sigue al jugador |
| `Temporizador.cs` | `Sistema/` | Singleton de flujo de juego |
| `TransicionManager.cs` | `Sistema/` | Singleton de flujo de juego |
| `AutoDesactivar.cs` | `Utilidades/` | Genérica, sin dependencia del juego |
| `BotonGameOver.cs` | `Utilidades/` | Adaptador delgado, puente entre UI y sistema |

---

## 4. Criterio de clasificación

> **La pregunta no es "qué hace", sino "qué cambia si lo rompo".**

Si romper el script **rompe el juego** → `Gameplay/`
Si romper el script **rompe la pantalla** → `UI/`
Si romper el script **rompe el flujo entre escenas** → `Sistema/`
Si romper el script **rompe una escena en particular** → `Utilidades/`

Esto evita el problema clásico de las carpetas por feature: un script que toca varias áreas no sabe dónde ir.

---

## 5. Reglas para scripts nuevos

| Si el script… | Va en |
|---|---|
| Lee input, mueve al jugador, o reacciona al mundo | `Gameplay/` |
| Toca Canvas, botones, paneles o texto en pantalla | `UI/` |
| Carga/guarda escenas, o es singleton persistente | `Sistema/` |
| Es genérico y no depende de nada del juego | `Utilidades/` |

Si un script **encaja en dos**, va donde esté su **efecto visible**, no su implementación.

---

## 6. Decisiones que NO tomamos

### 6.1 No usamos carpetas por feature
❌ `Scripts/Comic/`, `Scripts/Movimiento/`

**Por qué:** un script que toca dos áreas no tiene lugar. `Temporizador` maneja reloj, HUD y escenas — ¿iría en `Temporizador/` o en `HUD/`? La estructura por feature se rompe con el primer archivo compartido.

### 6.2 No creamos `Managers/`
❌ `Scripts/Managers/`

**Por qué:** cajón de sastre. Todo "un poco administrador" cae ahí y vuelve a estar todo junto. `Sistema/` es específico: flujo entre escenas y estado global.

### 6.3 No creamos `Player/` ni `Enemies/`
❌ `Scripts/Player/`, `Scripts/Dinos/`

**Por qué:** son sub-divisiones de `Gameplay/` que hoy no aportan. Si `Gameplay/` crece a 20+ archivos, se puede subdividir **después**, con datos reales en vez de intuición.

### 6.4 No movemos nada a `Assets/Scripts/` desde otras carpetas
El inventario detectó `Assets/Editor/AutoSave.cs` (73 líneas, editor-only). **No se toca.** Ya está en su carpeta correcta (`Editor/`).

---

## 7. Impacto de la reorganización

| Métrica | Antes | Después |
|---|---|---|
| Carpetas | 1 | 5 |
| Profundidad máxima | 1 | 2 |
| Scripts en carpeta raíz | 15 | 1 |
| Archivos `.cs` modificados | — | **0** |
| Comportamiento del juego | — | **sin cambios** |

**Ningún `.cs` se abre, edita ni recompila distinto.** Solo cambia la ruta en disco.

---

## 8. Riesgos de la reorganización

### 8.1 El `.meta` DEBE moverse junto al `.cs`

Este es **el único riesgo real**. Si movés el `.cs` sin su `.meta`:

- Unity genera un `.meta` nuevo con GUID distinto
- Las escenas pierden la referencia al script → **"missing script"**
- **Y no hay error de compilación** — compila perfecto y falla en runtime

Por eso el MD 3 insiste en `git mv` (mueve ambos).

### 8.2 Unity abierto durante el movimiento

Si Unity está abierto mientras movés archivos, puede:
- Cachear el path viejo
- Regenerar `.meta` duplicados
- No detectar el cambio hasta que recompila

**Solución: cerrar Unity antes de mover.** Más lento, cero sorpresas.

### 8.3 Orden incorrecto

Si movés un archivo que todavía no existe en destino, `git mv` falla. Se migra **uno por uno**, nunca la carpeta entera de golpe.

---

*Fin del MD 2. Este documento no ejecuta nada — solo propone. La ejecución está en MD 3.*