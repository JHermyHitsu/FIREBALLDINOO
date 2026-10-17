# 🎮 FireballDino — Plan de UI / HUD

> Objetivo: HUD simple, sin saturación, con feedback visual claro para que el jugador "sienta" el estado del juego sin tener que leer texto.

---

## 1. Timer (arriba al centro)

**Estado:** ✅ Implementado en `Temporizador.cs`.

**Cambios realizados:**
- [x] Tamaño de fuente ajustado a 42 (visible sin saturar).
- [x] Color rojo neón (#ff2a4b) a partir del segundo 15.
- [x] Temblor (scale + rotación + desplazamiento) a partir del segundo 8, duración 0.2s, amplitud sutil.
- [ ] (Opcional) Sombra/outline negro para legibilidad sobre cualquier fondo de bioma — pendiente.

**Referencia en código:** `Temporizador.cs` → `Update()` y `ConstruirUIPersistente()`.

---

## 2. Contador de amigos (arriba a la izquierda)

**Estado:** ✅ Implementado (híbrido: jerarquía + fallback por código).

**Cambios realizados:**
- [x] Campo `textoContadorAmigos` en `Temporizador`.
- [x] GameObject en jerarquía asignado en inspector.
- [x] Actualizado cada frame desde `Movimiento.seguidores.Count`.
- [x] Pop (scale 1→1.3→1) al reclutar un amigo.
- [x] Fallback por código si la escena no trae el objeto.
- [x] Se oculta en Game Over/Victoria y se reactiva al reintentar.

**Referencia en código:** `Temporizador.cs` → `Update()` y `AsegurarContadorAmigos()`.

---

## 3. Indicador de boost (aura en el dino + barra finita)

**Estado:** ✅ Implementado (híbrido: jerarquía + fallback por código).

**Cambios realizados:**
- [x] Property `BoostTiempoRestante`, `BoostActivo`, `BoostDuracion` en `Movimiento`.
- [x] `ActivarBoost` guarda la duración.
- [x] Aura dorada en el SpriteRenderer del dino mientras el boost está activo.
- [x] GameObject `BarraBoost` en jerarquía asignado en inspector (Filled, Horizontal).
- [x] Barra se vacía con `fillAmount` cada frame mientras el boost está activo.
- [x] Se activa/desactiva automáticamente.
- [x] Fallback por código si la escena no trae el objeto.
- [x] Se oculta en Game Over/Victoria y se reactiva al reintentar.

**Referencia en código:** `Movimiento.cs` → `ActivarBoost()` / `Update()`; `Temporizador.cs` → `Update()` / `AsegurarBarraBoost()`.

---

## 4. Game Over / Victoria (paneles full-screen)

**Estado actual:** `Temporizador.cs` crea ambos paneles por código con un solo botón ("REINTENTAR" / "JUGAR DE NUEVO"). El dino se desactiva (`mov.enabled = false`) pero queda en escena.

**Cambios a hacer:**
- [ ] Mantener un solo botón por panel (ya implementado).
- [ ] Mostrar el dino muerto/caído detrás del panel (no ocultarlo ni destruirlo inmediatamente) — aumentar el delay de destrucción para que se vea la animación de muerte.
- [ ] En Game Over, mostrar la causa: "TE CAÍSTE" / "SIN TIEMPO" / "TODOS CAÍDOS" según corresponda (ya existe el parámetro `mensaje`).
- [ ] En Victoria, mantener stats: dinos rescatados + tiempo usado (ya implementado).
- [ ] (Opcional) Fondo del panel semitransparente (ya está en 0.6 alpha) — verificar que no tape del todo al dino.

**Referencia en código:** `Temporizador.cs` → `MostrarGameOver()` / `MostrarVictoria()` / `MostrarBotonMuerte()`.

---

## Notas generales de estilo

- Paleta por bioma: verde (bosque) → amarillo (desierto) → rojo/naranja (subterráneo). UI en tonos neutros (blanco/negro con outline) para no chocar con el fondo.
- Fuente consistente en todo el HUD (pixelada o rounded).
- Sin minimapa, inventario ni barras de vida por dino.
- Tutorial/controles van en el cómic de intro (pantalla aparte con botón Skip), no en gameplay.

---

## Orden de implementación sugerido

1. Timer con color rojo (cambio mínimo, alto impacto).
2. Contador de amigos (nuevo elemento, requiere wiring con `Movimiento`).
3. Indicador de boost (nuevo elemento, requiere wiring con `Movimiento`).
4. Ajustes de Game Over/Victoria (pulido visual).