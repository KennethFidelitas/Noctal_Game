# Movimiento y controles de Tlamani

**Proyecto:** Noctal. **Motor:** Unity 2022.3.62f3. **Fecha:** 31 de octubre de 2026.

## Objetivo y alcance

Configurar a Tlamani en el nivel para caminar, saltar y desplazarse con dash, respetando el suelo, las paredes, el techo y las plataformas. La escena principal es `Entrada_Pasillo`; también se configura el personaje en `Intro`. Se conservan los sprites y el escenario existentes del equipo.

## Controles

| Entrada | Resultado |
| --- | --- |
| A/D o flechas izquierda/derecha | Movimiento horizontal y cambio de orientación. |
| Espacio | Salto desde el suelo. Soltar antes reduce la altura. |
| Shift izquierdo o derecho | Dash horizontal hacia la última dirección elegida. |
| R | Regreso al punto inicial y limpieza de la velocidad. |

Las teclas se leen en `Update`, que se ejecuta por cuadro de imagen. Las pulsaciones se almacenan hasta `FixedUpdate`, para no perder un salto o dash entre pasos de física. Presionar izquierda y derecha a la vez detiene la intención de movimiento. Al perder el foco de la ventana se limpia la entrada pendiente.

## Movimiento y salto

`TlamaniMotor` controla la velocidad del `Rigidbody2D`. La velocidad horizontal se aproxima al objetivo mediante una aceleración limitada, lo que permite arrancar, frenar y cambiar de dirección. El desplazamiento normal se realiza mediante físicas, sin trasladar directamente el `Transform`.

El salto asigna una velocidad vertical inicial. Después, la gravedad produce el ascenso, el punto más alto y la caída. Soltar Espacio durante el ascenso reduce a la mitad la velocidad vertical restante. No hay doble salto.

Se incluyen dos tolerancias pequeñas para mejorar la respuesta: 0,10 s para saltar justo después de abandonar un borde (coyote time) y 0,12 s para recordar una pulsación de salto justo antes de aterrizar (jump buffer). Las paredes y el techo no cuentan como suelo: la detección exige una normal de superficie con componente vertical de al menos 0,65.

## Dash

Al presionar Shift, el personaje conserva una dirección horizontal durante 0,16 s. Se suspende la gravedad durante ese desplazamiento y se restaura al finalizar o reiniciar. La recarga es de 0,55 s desde el inicio del dash; en el aire solo se dispone de una carga hasta aterrizar. Mantener Shift no genera dashes automáticos.

Antes de cada paso de dash, `Rigidbody2D.Cast` barre la forma del personaje hacia el movimiento previsto. Si encuentra una pared, limita la distancia hasta el obstáculo menos un margen de 0,015 unidades y termina el dash. La detección continua del Rigidbody2D proporciona protección adicional frente a atravesar superficies delgadas.

## Parámetros configurados

| Parámetro | Valor |
| --- | --- |
| Velocidad de caminata | 6 unidades/s |
| Aceleración y frenado | 60 unidades/s² |
| Velocidad inicial de salto | 12 unidades/s |
| Escala de gravedad | 3 |
| Límite de velocidad de caída antes de integrar gravedad | 22 unidades/s |
| Velocidad de dash | 18 unidades/s |
| Duración del dash | 0,16 s |
| Desplazamiento nominal durante dash libre | 2,88 unidades |
| Recarga desde el inicio del dash | 0,55 s |
| Tolerancia después del borde / antes de aterrizar | 0,10 / 0,12 s |
| Paso fijo de física | 0,02 s (50 Hz) |

Con gravedad global de -9,81 unidades/s² y escala 3, la aceleración vertical es aproximadamente -29,43 unidades/s². Sin techo y manteniendo Espacio, la estimación ideal de la altura del salto es `v² / (2g) ≈ 2,45` unidades. El resultado discreto de Unity puede variar ligeramente por el paso de simulación y los contactos.

## Colisiones e integración

Tlamani tiene un `Rigidbody2D` dinámico, interpolación activada, rotación Z bloqueada y detección continua. Un `CapsuleCollider2D` de 0,68 × 1,10 unidades aproxima su cuerpo. El material físico tiene fricción y rebote cero para evitar que se adhiera a las paredes o rebote al caer.

El suelo y las paredes del tilemap tienen Rigidbody2D estático, TilemapCollider2D y CompositeCollider2D. Se usan celdas sólidas de tipo Grid y geometría compuesta de polígonos para evitar pequeñas discontinuidades entre tiles. Los tilemaps de fondo y decoración permanecen sin colisiones.

La zona de muerte es un trigger: detecta a Tlamani y llama al reinicio del motor, que limpia dash, salto pendiente y velocidad antes de ubicarlo en `Punto_Inicio`. Se corrigió el nombre de la clase `Zona_Muerte` para que coincida con el archivo y se asignó el punto de inicio. También existe un límite inferior de seguridad en el motor.

La cámara sigue a Tlamani suavemente. Las secuencias de reposo, caminata, salto y carrera usan los sprites originales; el dash utiliza la secuencia de carrera y un tinte cian. La interfaz muestra controles y disponibilidad del dash.

## Verificación

Las pruebas PlayMode ejecutan la simulación real de Unity con superficies controladas. Comprueban caminar en ambos sentidos y frenar, salto y aterrizaje, rechazo de doble salto, salto corto, distancia y recarga del dash, paredes delgadas a ambos lados, techo, plataforma delgada, carga aérea, reinicio durante dash, tolerancia del borde y salto anticipado.

**Resultado: 15 de 15 pruebas aprobadas, 0 fallos**, ejecutadas el 31 de octubre de 2026 en Unity 2022.3.62f3. El informe original de Unity está en [test-results.xml](test-results.xml). Las dos pruebas de integración cargan Entrada_Pasillo e Intro; en el pasillo se verifican caminata, salto, dash, aterrizaje y el trigger real de la zona de muerte.

Las siguientes capturas se renderizaron con la cámara de Unity durante las pruebas PlayMode. Muestran el resultado real de la escena; no incluyen la interfaz del editor ni el HUD superpuesto.

![Tlamani al inicio del pasillo](capturas/01-inicio.png)

*Inicio: Tlamani apoyado sobre el suelo del nivel.*

![Tlamani saltando](capturas/03-salto.png)

*Salto: ascenso con gravedad y colisiones activas.*

![Tlamani ejecutando dash](capturas/04-dash.png)

*Dash: desplazamiento horizontal y tinte cian durante la carga aérea.*

![Tlamani después de aterrizar](capturas/05-aterrizaje.png)

*Aterrizaje: contacto con el suelo después de combinar salto y dash.*

Versión lista para entregar: [Tlamani_Documentacion.pdf](Tlamani_Documentacion.pdf).

## Reproducir la prueba manual

1. Abrir `Entrada_Pasillo` y entrar en Play. Dar foco a Game.
2. Caminar en ambos sentidos; comprobar que la cámara acompaña y el sprite se orienta.
3. Saltar y aterrizar sobre el suelo y las plataformas del pasillo. Comparar un toque breve de Espacio con mantenerlo.
4. Usar Shift en suelo y durante un salto. Comprobar la recarga y que una segunda carga aérea exige aterrizar.
5. Hacer dash contra una pared y saltar bajo el techo: el personaje debe detenerse sin atravesarlos.
6. Caer en un hueco y presionar R durante otro dash: ambos casos deben regresar al punto inicial sin conservar velocidad.

## Fuentes técnicas

- [Unity 2022.3: Rigidbody2D.Cast](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Rigidbody2D.Cast.html).
- [Unity 2022.3: CollisionDetectionMode2D](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/CollisionDetectionMode2D.html).
- [Unity Test Framework 1.1: ejecución por línea de comandos](https://docs.unity3d.com/Packages/com.unity.test-framework@1.1/manual/reference-command-line.html).
