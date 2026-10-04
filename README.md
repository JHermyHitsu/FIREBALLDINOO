# 🦕🔥 FireballDino — Game Design Document (GDD)

> **Versión:** 1.0  
> **Motor:** Unity  
> **Género:** Runner / Coleccionador de NPCs  
> **Referencia de estilo:** Zombie Tsunami  

---

## 📖 Historia

Un meteoro se dirige hacia la Tierra. Un dinosaurio valiente debe recorrer tres biomas peligrosos para **rescatar a sus amigos** y llevarlos a una **zona segura** antes de que sea demasiado tarde.

Al llegar al bioma subterráneo, el meteoro ya habrá impactado: la lava sube, la tierra tiembla... y al final de todo, aparece la puerta hacia la salvación.

---

## 🎯 Objetivo

| Tipo | Descripción |
|------|-------------|
| **Victoria** | Llegar a la zona segura con **al menos 1 amigo** vivo |
| **Derrota** | Que **todos los dinosaurios** (jugador + amigos) caigan |

---

## 🎮 Mecánicas Principales

### 🦕 Personaje
- El jugador controla un **dinosaurio principal**
- Al pasar junto a **amigos dinosaurios** en el nivel, estos se **unen a la fila** (al estilo Zombie Tsunami)
- Los amigos siguen al jugador en cadena

### 💀 Sistema de vidas / amigos
- El dinosaurio que **toca o cae en un obstáculo** es eliminado directamente
- Al morir, el dinosaurio reproduce una **animación estilo Mario Bros**: salta hacia arriba y cae fuera de pantalla

### ⚡ Power-up: Filete de Carne
- Al recoger un **filete de carne** en el nivel, todos los dinosaurios de la fila **corren más rápido** temporalmente
- *(El animal del que proviene la carne se definirá más adelante)*

### ⏱️ Temporizador
- Hay un **contador visible en la parte superior** de la pantalla
- Representa el tiempo antes de que el meteoro impacte (o la lava alcance al jugador en el último bioma)
- Si el tiempo llega a 0 → derrota automática *(por confirmar comportamiento exacto)*

---

## 🗺️ Estructura de Niveles (Biomas)

```
[Bosque] ──► [Desierto] ──► [Subterráneo] ──► [Zona Segura 🚪]
```

Cada bioma tiene sus **propios dinosaurios amigos** que el jugador puede rescatar.

---

## 🌲 Bioma 1 — Bosque

| Elemento | Descripción |
|----------|-------------|
| **Ambiente** | Selva densa, vegetación, árboles |
| **Amigos** | Dinosaurios típicos del bosque |
| **Obstáculos** | Huecos grandes en el suelo |
| **Extras** | Más obstáculos por definir según avance del desarrollo |

> [!NOTE]
> Los huecos grandes son el obstáculo principal. El dinosaurio que **no logra saltar a tiempo** cae y es eliminado.

---

## 🏜️ Bioma 2 — Desierto

| Elemento | Descripción |
|----------|-------------|
| **Ambiente** | Dunas, cielo caliente, tierra seca |
| **Amigos** | Dinosaurios del desierto |
| **Obstáculos** | Cactus, arenas movedizas |

> [!NOTE]
> **Cactus:** Obstáculo de colisión directa, elimina al dinosaurio que lo toca.  
> **Arena movediza:** Si el dinosaurio no salta a tiempo antes de que la arena movediza se hunda, cae con ella y muere.

---

## 🪨 Bioma 3 — Subterráneo *(el más intenso)*

| Elemento | Descripción |
|----------|-------------|
| **Ambiente** | Caverna oscura, roca, magma |
| **Amigos** | Dinosaurios subterráneos |
| **Obstáculos** | Lava ascendente, estalactitas (techo), estalagmitas (suelo) |
| **Evento especial** | ¡Terremoto! La pantalla tiembla y caen rocas |
| **Final** | Aparece la **puerta de la zona segura** |

> [!IMPORTANT]
> En este bioma el meteoro ya impactó. La lava sube constantemente (presión desde abajo). Al pasar todos los obstáculos y el terremoto, aparece la puerta final. Al entrar → **fin del juego (victoria)**.



## ⚡ Power-ups

| Power-up | Efecto | Estado |
|----------|--------|--------|
| 🥩 Filete de carne | Aumenta velocidad de toda la fila temporalmente | Confirmado |
| *(más por definir)* | — | — |

---

## 🏆 Condiciones de Fin de Juego

```
VICTORIA ✅
└── Entrar a la zona segura con al menos 1 dinosaurio vivo

DERROTA ❌
└── Todos los dinosaurios de la fila han caído
    └── (incluyendo el jugador principal)
```

---

## 🖥️ HUD (Interfaz en pantalla)

| Elemento | Posición | Descripción |
|----------|----------|-------------|
| ⏱️ Temporizador | Parte superior (centro) | Cuenta regresiva del meteoro |
| 🦕 Contador de amigos | Por definir | Número de dinos en la fila |




*Documento generado el 30/09/2026 — Actualizar conforme avance el desarrollo.*
