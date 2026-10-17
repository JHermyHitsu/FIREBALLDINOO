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

**Estado actual:** No existe. `Movimiento.cs` tiene `seguidores.Count` pero no hay UI que lo muestre.

**Cambios a hacer:**
- [ ] Crear un GameObject con Image (ícono de dino) + TextMeshProUGUI (número) anclado arriba a la izquierda.
- [ ] Actualizar el número en tiempo real desde `Movimiento.seguidores.Count`.
- [ ] Al reclutar un amigo (`AgregarSeguidor`), disparar un pop: escala 1 → 1.3 → 1 en ~0.2s.
- [ ] Si `seguidores.Count == 0`, mostrar "0" en gris/desaturado (no ocultar el ícono).
- [ ] Sin texto descriptivo ("Amigos:", etc.) — solo ícono + número.

**Referencia en código:** `Movimiento.cs` → `AgregarSeguidor()` / `RemoverSeguidor()`.

---

## 3. Indicador de boost (aura en el dino + barra finita)

**Estado actual:** `ItemComida.cs` activa el boost en `Movimiento` y destruye el item. No hay feedback visual de duración.

**Cambios a hacer:**
- [ ] Aura/borde alrededor del dino jugador mientras `boostTiempo > 0` (SpriteRenderer color tint o partículas simples).
- [ ] Barra horizontal finita (Image con fillAmount) debajo del dino o en el HUD, que se vacíe durante `duracion` segundos.
- [ ] Al terminar el boost, quitar aura y ocultar la barra (no dejar UI colgando).
- [ ] (Opcional) Sonido corto al activar y al terminar.

**Referencia en código:** `Movimiento.cs` → `ActivarBoost()` y `Update()` (temporizador de boost).

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