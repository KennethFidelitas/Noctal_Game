# Noctal - Movimiento de Tlamani

Proyecto para **Unity 2022.3.62f3**. Rama de trabajo: `feature/tlamani-movimiento`.

## Ejecutar

1. Abrir esta carpeta desde Unity Hub con Unity 2022.3.62f3.
2. Abrir `Assets/Scenes/Entrada_Pasillo.unity` y presionar **Play**.
3. Hacer clic en la pestaña **Game** para que reciba el teclado.

| Acción | Tecla |
| --- | --- |
| Caminar | A / D o flechas izquierda / derecha |
| Saltar | Espacio (soltar pronto produce un salto más corto) |
| Dash horizontal | Shift izquierdo o derecho |
| Volver al inicio | R |

El dash sigue la última dirección del personaje, dura 0,16 s y tiene una recarga de 0,55 s desde su inicio. Se permite una carga aérea hasta aterrizar. La esquina superior derecha muestra su disponibilidad.

`Intro.unity` también tiene configurado a Tlamani. `Entrada_Pasillo` es la escena inicial de la compilación. Se utilizan los sprites originales del repositorio.

## Archivos principales

- `Assets/Scripts/Tlamani/TlamaniMotor.cs`: caminar, salto, dash y físicas.
- `Assets/Scripts/Tlamani/TlamaniKeyboard.cs`: lectura del teclado.
- `Assets/Scripts/Tlamani/TlamaniVisual.cs`: animaciones por cuadros y orientación.
- `Assets/Scripts/Tlamani/TlamaniCamera.cs`: cámara que sigue al personaje.
- `Assets/Prefabs/Tlamani.prefab`: personaje reutilizable; asignar el punto de inicio en cada escena.
- `Assets/Zona_Muerte.cs`: regreso seguro al punto de inicio al caer.
- `Assets/Tests/PlayMode`: pruebas de movimiento y colisiones con Physics2D real.
- [Documentación técnica](docs/movimiento-tlamani.md).

## Probar

En Unity: **Window > General > Test Runner > PlayMode > Run All**.
Las pruebas no se incluyen en una compilación del juego.

## Integración con el equipo

La rama parte del commit `abf5646` de `escenario-entrada-pasillo-persona2`, que ya contenía el escenario de entrada, pasillo y zona de muerte. El trabajo de movimiento conserva ese escenario e integra al personaje. No se modifica la rama `main`.

Las escenas ya están guardadas y listas para jugar. La herramienta opcional **Noctal > Configurar Tlamani en las escenas** reproduce la configuración inicial; no ejecutarla sobre escenas con cambios sin guardar, pues vuelve a abrir y guardar ambas escenas.
