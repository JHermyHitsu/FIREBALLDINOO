# MD 3 — Checklist de Migración

> **Fecha:** 2026-10-18
> **Alcance:** ejecución de la reorganización de MD 2
> **Estado:** listo para ejecutar
> **Prerequisitos:** MD 1 y MD 2 aprobados

---

## 1. El ciclo (repetir 15 veces)

Cada script se mueve **solo**. Nunca dos a la vez.

```
┌─ 1. Cerrar Unity                        ┐
├─ 2. git mv (archivo + .meta juntos)     ┤
├─ 3. Abrir Unity → compilar              ┤
├─ 4. Verificar 0 errores                 ┤
├─ 5. Abrir escena(s) → sin missing       ┤
├─ 6. Play → probar comportamiento        ┤
├─ 7. git commit                           ┤
└─ 8. ¿Funciona? → sí: sigue  /  no: revert ┘
```

**Si algo falla: `git revert <hash>` y volvés al estado anterior.** Ese commit es tu red.

---

## 2. Por qué un commit por archivo

| Un mega-commit | Un commit por archivo |
|---|---|
| Si falla, no sabés qué movió | Sabés exactamente cuál |
| Revert deshace 15 cambios | Revert deshace 1 |
| Diff inrevisable | Cada commit es 2 líneas |

---

## 3. Orden de migración recomendado

De menor a mayor riesgo.

| # | Archivo | Destino | Riesgo | Verificación |
|---|---|---|---|---|
| 1 | `Escenas.cs` | *(raíz)* | 🟢 Nulo | Compila |
| 2 | `AutoDesactivar.cs` | `Utilidades/` | 🟢 Bajo | 1 objeto con el componente en Bosque |
| 3 | `ComicIntro.cs` | `UI/` | 🟢 Bajo | Botón JUGAR → comic completo |
| 4 | `HoverBoton.cs` | `UI/` | 🟡 Medio | Hover en Menú y en Bosque |
| 5 | `BotonGameOver.cs` | `Utilidades/` | 🟡 Medio | Game Over en Cuevas → Reintentar / Salir |
| 6 | `Pausa.cs` | `UI/` | 🟡 Medio | ESC pausa / Continuar / Salir |
| 7 | `ZonaMuerte.cs` | `Gameplay/` | 🟡 Medio | Morir por precipicio en Bosque |
| 8 | `MetaFinal.cs` | `Gameplay/` | 🟡 Medio | Llegar a la meta en Cuevas → victoria |
| 9 | `SalidaNivel.cs` | `Gameplay/` | 🟠 Alto | Borde del bioma en Bosque → cambia a Desierto |
| 10 | `Camara.cs` | `Gameplay/` | 🟠 Alto | Seguir al jugador + temblor al agotarse el tiempo |
| 11 | `ItemComida.cs` | `Gameplay/` | 🟠 Alto | Comer un muslo → boost + hint |
| 12 | `DinoSeguidor.cs` | `Gameplay/` | 🔴 Muy alto | Reclutamiento, cadena, sacrificio, cambio de escena |
| 13 | `Movimiento.cs` | `Gameplay/` | 🔴 Muy alto | Mover, saltar, boost, followers, muerte |
| 14 | `TransicionManager.cs` | `Sistema/` | 🔴 Muy alto | Cambio de escena con fade y reubicación de la fila |
| 15 | `Temporizador.cs` | `Sistema/` | 🔴 Muy alto | Reloj, HUD, todos los paneles, reintentar, salir |

> Los últimos 4 son los que mueven la fila de dinosaurios entre escenas. Si algo falla ahí, **pará y reportá** — no sigas con el siguiente.

---

## 4. Comandos del ciclo

### Paso 1 — Cerrar Unity

Manual. Antes de tocar nada.

### Paso 2 — Mover (script + meta)

```bash
cd /home/dayvis/ProyectosUnity/FIREBALLDINOO

# Crear la carpeta destino (solo la primera vez de cada carpeta)
mkdir -p Assets/Scripts/UI
mkdir -p Assets/Scripts/Gameplay
mkdir -p Assets/Scripts/Sistema
mkdir -p Assets/Scripts/Utilidades

# Mover script Y .meta juntos (el comodín cubre ambos)
git mv Assets/Scripts/HoverBoton.cs* Assets/Scripts/UI/
```

> ⚠️ **El `.meta` NO se arrastra solo.** `git mv archivo.cs destino/` mueve únicamente el `.cs`
> y deja el `.meta` atrás. Por eso el comando usa el comodín `archivo.cs*`.
>
> Esto ya pasó una vez: `AutoDesactivar.cs` quedó sin su `.meta` en la carpeta vieja y
> Unity habría generado un GUID nuevo, rompiendo la referencia de la escena.

**Verificar que se movieron los dos:**

```bash
find Assets -name "HoverBoton*" -not -path "*/Library/*"
```

Debe devolver **exactamente 2 líneas**, ambas en la carpeta destino:
```
Assets/Scripts/UI/HoverBoton.cs
Assets/Scripts/UI/HoverBoton.cs.meta
```

Si aparece un `HoverBoton.cs.meta` **en la carpeta vieja**, el `.meta` se quedó atrás.
**No abras Unity todavía** — corregilo primero:

```bash
git mv Assets/Scripts/HoverBoton.cs.meta Assets/Scripts/UI/
```

**Verificar que el GUID no cambió:**

```bash
grep guid Assets/Scripts/UI/HoverBoton.cs.meta
```

### Paso 3-6 — En Unity

1. Abrir Unity
2. Esperar a que compile
3. Chequear la consola: **0 errores**
4. Abrir la escena donde vive el script
5. Confirmar que el GameObject **no** muestra `(Missing)`
6. Play → probar según la tabla del punto 3

### Paso 7 — Commit

```bash
git add -A
git commit -m "refactor: move HoverBoton to UI/ folder"
```

### Si falla

```bash
git log --oneline -1        # anotá el hash
git revert <hash>           # vuelve atrás
```

---

## 5. Checklist por tipo de script

### Scripts en escenas (comprobación visual)
```
☐ Consola: 0 errores
☐ GameObject seleccionado: sin (Missing)
☐ Script visible en el Inspector
☐ Play funciona
```

### Scripts en prefabs (comprobación adicional)
```
☐ Abrir el prefab (Doble click en el asset)
☐ Componentes intactos
☐ Instancias en escena: sin (Missing)
☐ Play funciona
```

### Scripts singletons (`Temporizador`, `TransicionManager`)
```
☐ Play en la escena donde está serializado
☐ Sin errores de "ya existe" o doble creación
☐ Funcionalidad principal intacta
☐ Play desde una escena donde NO está (Debierto/Cuevas)
   → verificar que EnsureExists() lo crea bien
```

### Scripts con UI construida por código (`ItemComida`)
```
☐ Consumir un item → boost activo
☐ El hint aparece
☐ La UI no desaparece raro
☐ Reiniciar nivel → el hint vuelve a funcionar
```

---

## 6. Señales de alarma (pará y reportá)

Si ves cualquiera de estas, **revertí y avisame**. No sigas con el siguiente archivo.

| Señal | Qué significa |
|---|---|
| `(Missing Script)` en un GameObject | El `.meta` no se movió |
| `NullReferenceException` en consola | Referencia perdida |
| `The referenced script on this Behaviour is missing` | GUID roto |
| La UI no aparece | `Temporizador` no encontró sus objetos por nombre |
| El juego se pausa solo | `Time.timeScale` quedó en 0 |
| El dinosaurio no sigue al jugador | `Movimiento` / `DinoSeguidor` desincronizados |
| El cambio de escena no hace fade | `TransicionManager` no arrancó |

### Chequeo rápido ANTES de abrir Unity

Este `find` detecta el `.meta` huérfano sin necesidad de abrir el editor:

```bash
find Assets -name "NombreScript*" -not -path "*/Library/*"
```

Si devuelve más de 2 líneas, o si alguna está fuera de la carpeta destino,
**el `.meta` se quedó atrás**. Corregilo antes de abrir Unity.

---

## 7. Al terminar

```bash
git log --oneline -15        # los 15 commits
git status                  # debería estar limpio
```

Luego, **en otro commit**, se puede considerar la limpieza de código muerto del MD 1 — pero **nunca en el mismo commit** que un movimiento. Si mezclás mover y borrar, cuando algo falle no sabés cuál de las dos cosas lo causó.

---

## 8. Lo que NO se hace en esta fase

- ❌ Borrar código muerto (MD 1 §4) → fase aparte
- ❌ Dividir `Temporizador` en 5 clases → requiere diseño
- ❌ Agregar namespaces
- ❌ Arreglar APIs deprecadas
- ❌ Tocar el sistema de UI dual

> Solo se mueven archivos. Nada de código se abre ni se edita.

---

*Fin del MD 3. Ejecutar un archivo por vez, con verificación completa antes de seguir.*