# Noctal — Mecánicas de juego

Prototipo para **Unity 2022.3.62f3**, basado en «Mecánicas de Juego» del GDD del equipo (páginas 3–4), con los patrones de enemigos de las páginas 6–8.

## Ejecutar

1. Abrir esta carpeta desde Unity Hub con Unity 2022.3.62f3.
2. Abrir `Assets/Scenes/Mecanicas_Noctal.unity` y presionar **Play**.
3. Hacer clic en **Game** para que reciba el teclado.

| Acción | Control |
| --- | --- |
| Caminar | A / D o flechas |
| Saltar | Espacio; soltar antes reduce la altura |
| Ataque corto, también en el aire | J o clic izquierdo |
| Rayo de luz | Mantener K o clic derecho |
| Transformación en puma | Q, después de recoger el ánima |
| Reintentar desde la sala alcanzada | R |
| Pausar / continuar | Esc |

El recorrido conecta Entrada del Templo, Galería de la Luz, Sala de Enemigos, Santuario Corrompido y Cámara de Tenebris. En el santuario se recoge el ánima, se cruza el paso bajo como puma y se usa la luz sobre la sombra y el sello que abre la puerta. Llanthu requiere luz para recibir daño; al derrotarlo hay que iluminar Tenebris para completar el nivel. Las fuentes de energía de la arena se recargan cada ocho segundos.

La vida y la energía son limitadas. Los cristales y los golpes efectivos recuperan energía; las ofrendas recuperan vida. La luz abre defensas, ralentiza el movimiento y prolonga la preparación y recuperación de los enemigos. El puma camina más rápido, salta más alto y golpea con garras.

## Alcance y recursos

Se reutilizan los sprites de Tlamani y los tiles del proyecto. El puma, los cuatro tipos de enemigo y Llanthu tienen **representaciones gráficas provisionales**, hechas con mallas de colores, que el equipo puede sustituir por su arte final. Las cifras de daño, vida y energía son valores de prototipo, ya que el GDD no los especifica. Las otras secciones del GDD, como cinemáticas, música y menús completos, quedan fuera de este entregable de mecánicas.

`Entrada_Pasillo` e `Intro` conservan el trabajo anterior de movimiento y dash. El recorrido nuevo usa las mecánicas del GDD y es la primera escena de compilación.

## Archivos y documentación

- `Assets/Scripts/Tlamani/TlamaniMotor.cs`: movimiento, salto y físicas.
- `TlamaniAbilities.cs`: ataque, transformación y luz; `TlamaniVitals.cs`: salud.
- `NoctalEnemy.cs` y `NoctalProjectile.cs`: patrones y ataques de enemigos.
- `NoctalLightTarget.cs` y `NoctalPickup.cs`: mecanismos y recursos.
- `NoctalLevel.cs` y `NoctalHud.cs`: avance, reintento, pausa, victoria e interfaz.
- `Assets/Prefabs/Tlamani_Mecanicas.prefab`: personaje del recorrido GDD.
- [Documentación de mecánicas](docs/mecanicas-gdd.md).
- [Documento Word con texto y capturas](docs/Tlamani_Documentacion.docx).
- [Informe de pruebas PlayMode](docs/test-results-gdd.xml).
- [Documentación anterior de movimiento y dash](docs/movimiento-tlamani.md).

## Verificación

En Unity: **Window > General > Test Runner > PlayMode > Run All**. Los tests usan Physics2D real y cubren las habilidades, los patrones enemigos, las interacciones del recorrido y las regresiones del movimiento. Los casos de integración suspenden la IA para aislar las interacciones; otros casos verifican los patrones con IA activa. Las capturas de `docs/capturas/07` a `12` proceden de la cámara de Unity durante PlayMode.

La escena está guardada y lista para jugar. **Noctal > Abrir mecanicas del GDD** la abre. La herramienta **Noctal > Construir recorrido de mecanicas GDD** regenera la escena y los assets de demostración; usarla solo si se desea reemplazar esa configuración generada.
