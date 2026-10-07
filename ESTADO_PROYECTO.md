# 🦖 FireballDino — Estado y Avance del Proyecto

Documento de seguimiento del estado actual del desarrollo del videojuego **FireballDino** (Proyecto Activo: `FIREBALLDINOO` en Unity 6.3 LTS).

---

## 📌 1. Información General del Proyecto

* **Nombre del Juego:** FireballDino
* **Género:** 2D Runner / Plataformas cooperativo estilo *Zombie Tsunami*
* **Motor:** Unity 6.3 LTS (Versión `6000.3.24f1`)
* **Repositorio Activo:** `https://github.com/JHermyHitsu/FIREBALLDINOO.git`
* **Rama de Trabajo Actual:** `feat/bioma-bosque`
* **Plataforma Objetivo:** PC / WebGL

---

## 📖 2. Premisa y Narrativa (Intro Cómic)

* **Contexto:** Un T-Rex protagonista vive pacíficamente en el valle cuando es el primero en divisar la caída inminente de un gigantesco meteorito en llamas.
* **Objetivo:** Descubre una cueva subterránea blindada (refugio seguro) y emprende una carrera contra el reloj para rescatar y liderar a sus amigos T-Rex dispersos a lo largo de 3 biomas antes del impacto final.
* **Mecánica Central de Cadena:** A medida que toca a sus amigos, se unen a su fila. Si caen por huecos o chocan con obstáculos letales, son eliminados uno por uno (estilo *Zombie Tsunami* con animación de derrota al estilo *Mario Bros*).

---

## 🗺️ 3. Estructura de Niveles y Biomas

El juego completo se conectará mediante transiciones de escenas (`SceneManager`):

```
[ESCENA_INTRO_COMIC] 
       ↓
[ESCENA_BIOMA_BOSQUE] (En desarrollo)
       ↓ (Salida / Trigger derecho)
[ESCENA_BIOMA_DESIERTO] (En desarrollo por compañero de equipo)
       ↓ (Salida / Trigger derecho)
[ESCENA_BIOMA_SUBTERRANEO] (En desarrollo por compañero de equipo)
       ↓
[CUEVA / ZONA SEGURA - VICTORIA]
```

---

## ✅ 4. Avances Implementados hasta el Momento

### A. Escenario del Bosque (`ESCENA_BIOMA_BOSQUE`)
* **Sistema de Tilemaps organizado en Grid:**
  * `tilemap_bosque_suelo`: Plataformas y suelo transitable.
  * `Tilemap_bosque_decoraciones`: Elementos visuales, arbustos y detalles sin colisión física.
  * `Tilemap_bosque_fondo`: Fondo ambiental (`Order in Layer` ajustado).
  * `Tilemap_bosque_arbol`: Capa dedicada a los troncos y vegetación alta.
* **Físicas del Suelo:**
  * `Tilemap Collider 2D` configurado.
  * `Composite Collider 2D` + `Rigidbody 2D` (Static) configurados para fusionar celdas y evitar tropiezos entre bloques individuales.
  * Tag asignado: `SUELO`.

### B. Personaje Jugable (`Dino_Jugador`)
* **Visual:**
  * Sprite Sheet cortado de la colección Dino (`DinoSprites - vita_0`).
  * `Order in Layer` ajustado para renderizarse frente al fondo y vegetación.
* **Físicas:**
  * `Rigidbody 2D` (Dynamic, `Freeze Rotation Z` activado, `Continuous Collision Detection`).
  * `Capsule Collider 2D` ajustado al cuerpo del dinosaurio.
* **Scripts de Control:**
  * `MOVIMIENTO1.cs`: Movimiento horizontal fluido (`Input.GetAxisRaw`), giro de sprite (`flipX`), detección de suelo mediante etiqueta `SUELO` y salto con la barra espaciadora (`Space`).

### C. Sistema de Cámara (`Main Camera`)
* Script `Camara.cs` implementado y vinculado al transform del `personaje` para seguimiento horizontal y vertical en `LateUpdate`.

---

## 📋 5. Tareas Pendientes y Próximos Pasos

### 🔹 Corto Plazo (Completar Bioma Bosque)
- [ ] **Animaciones del Dino:** Configurar el *Animator* con las transiciones Idle, Correr y Saltar usando los frames de `DinoSprites - vita`.
- [ ] **Zona de Muerte por Caída (Killzone):** Colocar un `Box Collider 2D` tipo *Trigger* en el fondo de los precipicios para detectar caídas.
- [ ] **Dinos Amigos Coleccionables:** Crear el Prefab del dino coleccionable que se active y sume a la fila al ser tocado.
- [ ] **Trigger de Salida de Nivel:** Objeto invisible con script al final del nivel para cambiar a `ESCENA_BIOMA_DESIERTO`.
- [ ] **Power-Up Filete de Carne:** Ítem de aumento de velocidad temporal.

### 🔹 Integración y Equipo
- [ ] Unificar las escenas de los compañeros (`ESCENA_BIOMA_DESIERTO` y `ESCENA_BIOMA_SUBTERRANEO`) mediante Pull Requests limpios en GitHub.
- [ ] Crear la pantalla / UI del Cómic de introducción con botón de *Skip*.
- [ ] Configurar el temporizador visible en pantalla (HUD / UI).
- [ ] Añadir las escenas en `Build Settings` para la compilación final.
