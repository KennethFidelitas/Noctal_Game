# Mecánicas de juego de Noctal

Implementación en Unity 2022.3.62f3 basada en la sección «Mecánicas de Juego» del documento G2_SC-620_GDD.pdf (páginas 3 y 4). Los patrones de enemigos toman como referencia las páginas 6 a 8. La transformación en puma sigue la indicación explícita de la sección de mecánicas.

## Alcance

La escena `Assets/Scenes/Mecanicas_Noctal.unity` contiene un recorrido de prueba con cinco salas conectadas: Entrada del Templo, Galería de la Luz, Sala de Enemigos, Santuario Corrompido y Cámara de Tenebris. Permite comprobar las siete mecánicas solicitadas. Las escenas anteriores del equipo se conservan.

Se reutilizan los sprites originales de Tlamani, incluida su animación de ataque, y los tiles del templo. El puma y los enemigos utilizan figuras provisionales construidas con mallas de colores, reemplazables por el arte final del equipo. Este entregable se centra en las mecánicas, no en completar las cinemáticas, música o menús descritos en otras secciones del GDD.

## Controles

A/D o flechas: movimiento lateral.

Espacio: salto. Soltar antes reduce la altura.

J o clic izquierdo: ataque cuerpo a cuerpo, tanto en el suelo como durante el salto.

K o clic derecho, mantenidos: rayo de luz en la dirección del personaje.

Q: alternar entre Tlamani y el puma después de recoger el ánima en el santuario.

R: reintentar desde el inicio de la sala alcanzada. Esc: pausa.

También se incluyen entradas genéricas de mando mediante el Input Manager de Unity: eje Horizontal, botón 0 para salto, 2 para ataque, 4 para luz, 3 para puma y 7 para pausa. Los números pueden variar según el mando; requieren comprobarse con el dispositivo físico.

## Movimiento lateral y salto

Tlamani camina a una velocidad máxima de 6 unidades por segundo. La aceleración y el frenado son de 60 unidades por segundo al cuadrado. El salto parte de 12 unidades por segundo de velocidad vertical y utiliza una escala de gravedad de 3. Se conserva el salto corto al soltar Espacio, la tolerancia de 0,10 segundos al salir de un borde y la memoria de pulsación de 0,12 segundos antes de aterrizar.

El personaje utiliza Rigidbody2D dinámico, interpolación, rotación bloqueada y detección continua. El suelo y las plataformas utilizan colisiones físicas. Los pivotes de los sprites mantienen los pies alineados y evitan el hundimiento al cambiar de animación.

El dash anterior permanece en las escenas de movimiento como trabajo previo, pero no forma parte de los controles del recorrido GDD porque no aparece en las mecánicas solicitadas.

## Ataque básico

El arma realiza un golpe corto hacia la orientación de Tlamani. Una zona de impacto detecta a los enemigos durante la parte activa de la animación. Cada enemigo recibe como máximo un impacto por ataque, incluso si tiene varios colliders. Las paredes bloquean el golpe y los objetivos lejanos o situados detrás quedan fuera de alcance.

El ataque también funciona en el aire. Esto permite saltar para alcanzar a Pilpintu y golpear objetivos elevados. En forma humana causa 1 punto de daño; en forma de puma las garras causan 2. La animación dura 0,36 segundos y la recuperación entre ataques es de 0,45 segundos.

## Sistema de vida

Tlamani dispone de 6 puntos de vida. Los impactos reducen la salud y activan 0,90 segundos de invulnerabilidad para impedir que un contacto reste toda la vida en varios pasos físicos consecutivos. El personaje parpadea y la cámara recibe una sacudida breve para indicar el daño.

Al llegar a cero, se detienen el movimiento y las habilidades y aparece el mensaje de derrota. R restaura la vida y la energía, vuelve a la sala alcanzada y reinicia los enemigos, mecanismos, recursos y proyectiles del recorrido. El ánima ya descubierta se conserva durante ese reintento. Las ofrendas de vida permiten recuperar salud durante la exploración.

## Habilidad especial: ánima de puma

La transformación comienza bloqueada. Se desbloquea al recoger el ánima en el altar del Santuario Corrompido. Después, Q permite alternar entre las dos formas.

El puma aumenta la velocidad un 45 % y la velocidad inicial del salto un 15 %, tiene un collider más bajo y utiliza ataques de garras. El paso bajo del santuario requiere esa forma. Antes de volver a humano se comprueba que exista espacio suficiente para el cuerpo; si hay un techo bajo, la transformación se rechaza para evitar quedar atrapado dentro de la geometría.

## Herramienta de luz y energía

El artefacto emite un rayo horizontal de 5,5 unidades de alcance. Su reserva máxima es de 100 puntos y consume 18 puntos de energía por segundo. Se detiene al agotarse la reserva. Las paredes y los objetivos alcanzados limitan el recorrido del rayo.

La luz debilita a las criaturas durante 3 segundos: reduce su movimiento, elimina temporalmente sus defensas y prolonga la preparación y recuperación de sus ataques. También disipa la barrera de sombra y activa el sello solar que abre la puerta del santuario. Estos objetos requieren una exposición continua de 0,55 segundos.

La energía se recupera explorando y combatiendo. Los cristales del recorrido aportan energía, un golpe efectivo recupera 4 puntos y derrotar a un enemigo recupera 20. Las fuentes de la arena vuelven a estar disponibles cada 8 segundos, de modo que quedarse sin energía no impide terminar el combate contra Llanthu. No existe regeneración automática por permanecer quieto lejos de los recursos.

## Enemigos y jefe

Apasanka patrulla el suelo. Al detectar a Tlamani, anuncia una embestida y avanza rápidamente. Sus contactos causan daño.

Pilpintu vuela y busca alinearse con el jugador. Anuncia su ataque, se lanza en picada y queda un momento a baja altura antes de volver al vuelo. El salto y el ataque aéreo permiten enfrentarse a ella.

El Guerrero Corrupto se aproxima al jugador y prepara un golpe pesado. Mientras mantiene la guardia, bloquea los ataques frontales. Puede recibir daño por la espalda, durante su apertura de ataque o después de ser debilitado con luz.

Ñawi permanece inmóvil. Muestra la dirección del rayo antes de disparar y espera antes de repetir el ataque. Las paredes interrumpen el rayo.

Llanthu dispone de 18 puntos de vida y requiere la luz para quedar vulnerable. Combina avances físicos con proyectiles. Al llegar a la mitad de la vida entra en una segunda fase más rápida, añade ondas de sombra y cambia de posición dentro de la arena. Sus ataques se anuncian visualmente.

Después de derrotar a Llanthu se repone la energía para el cierre del nivel. Tlamani debe iluminar el núcleo de Tenebris durante 1 segundo para sellarlo y mostrar la victoria. El núcleo no puede activarse antes de vencer al jefe.

## Decisiones de prototipo

El GDD define las capacidades y los patrones, pero no fija cifras de salud, daño, duración, energía ni bonificaciones del puma. Los valores anteriores son decisiones ajustables de esta implementación, no especificaciones numéricas extraídas del PDF. Los componentes y prefabs permiten que el equipo los equilibre posteriormente.

## Verificación

Los resultados de PlayMode se guardan en `docs/test-results-gdd.xml`. Las pruebas ejercitan ataque terrestre y aéreo, alcance y paredes, vida e invulnerabilidad, puma y techos bajos, consumo y recarga de energía, luz y mecanismos, patrones de enemigos, defensa y fases de Llanthu, victoria y reintento. También se mantienen las pruebas del movimiento anterior.

Las capturas se obtienen con la cámara real de Unity durante PlayMode y muestran el HUD del recorrido. Las pruebas de integración usan entradas programadas; los casos de enemigo comprueban sus patrones por separado y los casos de recorrido aíslan las interacciones para obtener resultados reproducibles.
