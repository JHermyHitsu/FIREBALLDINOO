# Auditoría de Scripts y Plan de Limpieza SOLID

Este documento detalla el diagnóstico del código actual del proyecto, los elementos redundantes o sin uso detectados, las desviaciones al Principio de Responsabilidad Única (SRP) y una guía paso a paso para optimizar y simplificar la base de código.

---

## 1. Diagnóstico General

La arquitectura general del proyecto cuenta con una buena separación de subsistemas lograda en las etapas recientes:
* **Físicas y Entrada:** Concentradas en `Movimiento.cs`.
* **Estado y Animación:** Delegadas a `EstadoJugador.cs`.
* **Vidas y Muerte:** Separadas en `SistemaSaludyMuerte.cs`.
* **Fila de Dinos:** Separada entre lógica (`ControladorFila.cs`) y cinemática (`FilaDinos.cs`).
* **Transporte y Persistencia:** Distribuidas entre `TransporteEscena.cs` y `PersistenciaJugador.cs`.
* **Interfaz de Usuario:** Dividida entre `HUD.cs`, `Reloj.cs` y `PanelFinPartida.cs`.

A pesar de esta estructura modular, se identificaron fragmentos de código muerto, parámetros huérfanos y acoplamientos innecesarios.

---

## 2. Hallazgos Detallados

### 2.1 Código Muerto y Parámetros Huérfanos

| Archivo | Elemento | Diagnóstico |
|---|---|---|
| `SistemaSaludyMuerte.cs` | Parámetro `bool esPrecipicio = false` en `Morir()` | Se recibe como argumento desde `ZonaMuerte` y `Movimiento`, pero dentro del método nunca se lee ni altera el flujo. |
| `DinoSeguidor.cs` y `FilaDinos.cs` | Campo `public int pasosSeparacion = 8` | Marcado como `[HideInInspector]` por compatibilidad antigua. El algoritmo actual usa `SmoothDamp` con `distanciaDetras`; este valor no participa en ningún cálculo. |
| `FilaDinos.cs` y `DinoSeguidor.cs` | Normalización de `localScale.z` en `Awake()` | Ambos componentes adjuntos al mismo GameObject ejecutan exactamente la misma validación de escala Z. |

### 2.2 Violaciones a Responsabilidad Única (SRP)

| Archivo | Responsabilidad Principal | Responsabilidad Extra Detectada (A separar o simplificar) |
|---|---|---|
| `ItemComida.cs` | Consumible de gameplay (dar boost de velocidad al contacto y destruirse). | Construcción dinámica de interfaz en runtime: busca el `Canvas`, crea un `GameObject` con `TextMeshProUGUI`, convierte coordenadas del mundo a pantalla y ejecuta una corrutina de temporizado. |
| `Movimiento.cs` | Control del personaje (entrada y físicas de salto/carrera). | Almacena `sonidoMuerte`, a pesar de que el ciclo de vida y audio de muerte lo gestiona `SistemaSaludyMuerte.cs`. |
| `Temporizador.cs` | Orquestador de ciclo de partida (`GameManager`). | Su nombre no refleja su propósito (el conteo real lo hace `Reloj.cs`). Además, controla directamente el efecto de temblor en `Camara.cs`. |

---

## 3. Plan de Acción Paso a Paso

### Paso 1: Eliminar Código Muerto y Parámetros Huérfanos

**Objetivo:** Retirar variables que generan confusión y no afectan el comportamiento.

1. **`SistemaSaludyMuerte.cs`**:
   - Cambiar la firma a `public void Morir()`.
   - Si en el futuro se requiere lógica especial para caídas a precipicios, se implementará explícitamente; actualmente no existe diferencia.
2. **`Movimiento.cs`**:
   - Actualizar el método puente: `public void Morir() => saludYMuerte?.Morir();`.
3. **`ZonaMuerte.cs`**:
   - Cambiar la llamada a `dino.Morir();`.
4. **`DinoSeguidor.cs` y `FilaDinos.cs`**:
   - Remover el campo huérfano `public int pasosSeparacion = 8;`.
   - Mantener la normalización de escala Z únicamente en `DinoSeguidor.Awake()`, eliminándola de `FilaDinos.Awake()`.

---

### Paso 2: Simplificar `ItemComida.cs`

**Objetivo:** Reducir la complejidad del ítem a su función esencial: detectar colisión, aplicar boost al jugador, emitir sonido y destruirse.

1. Remover de `ItemComida.cs`:
   - Campos de tutorial: `textoHint`, `duracionHint`, `maxVecesHint`, `alturaHint`, `vecesHintMostrado`, `hintEnPantalla`.
   - Métodos: `OnBecameVisible()`, `MostrarHint()`, `SeguirYDestruir()`.
2. Mantener únicamente:
   - Detección en `OnTriggerEnter2D`: comprobación de tag `"Player"`.
   - Llamada a `dino.ActivarBoost(multiplicador, duracion)`.
   - Reproducción de `sonidoComer`.
   - `Destroy(gameObject)`.

*(Nota: Si se requiere tutorial en pantalla, este debe vivir en un script de UI desacoplado o como parte de los Canvas estáticos del nivel, no instanciado dinámicamente por cada ítem individual).*

---

### Paso 3: Consolidar Configuración de Audio de Muerte

**Objetivo:** Que cada componente posea los recursos correspondientes a su función.

1. Mover `public AudioClip sonidoMuerte;` de `Movimiento.cs` hacia `SistemaSaludyMuerte.cs`.
2. En `SistemaSaludyMuerte.cs`, usar directamente su propio `sonidoMuerte`.
3. En `DinoSeguidor.cs` (`Sacrificar`):
   - Leer el clip de muerte de respaldo desde `lider.saludYMuerte.sonidoMuerte` en lugar de `lider.sonidoMuerte`.

---

### Paso 4: Limpieza de Nombres y Fachadas

**Objetivo:** Claridad conceptual sin romper escenas serializadas.

1. Mantener los nombres de clase `Temporizador` y `TransicionManager` como están para evitar desvincular componentes en las escenas `.unity` y prefabs existentes.
2. Documentar claramente en la cabecera de `Temporizador.cs` que actúa como orquestador general de la partida (GameManager), delegando el tiempo a `Reloj` y la UI a `HUD` / `PanelFinPartida`.

---

## 4. Matriz de Verificación

Tras aplicar cada paso, se debe validar en Unity:

```
[ ] Compilación limpia sin errores en C#.
[ ] El jugador se mueve y salta con normalidad.
[ ] Al tocar una ZonaMuerte sin seguidores, se produce la muerte y se abre el panel de Game Over.
[ ] Al tocar una ZonaMuerte con seguidores, se sacrifica el último seguidor y el jugador es impulsado hacia arriba.
[ ] Al recoger un muslo de comida, el jugador aumenta su velocidad y la barra/segmentos de boost se consumen de forma fluida.
[ ] La transición entre Bosque -> Desierto -> Cuevas conserva al jugador y la fila sin errores en consola.
```
