# Respaldo de contexto para continuar Mundo Aprendo

Fecha del respaldo: 19 de septiembre de 2026.

Este documento es el punto de reanudación para continuar el trabajo después de reiniciar el equipo o abrir un chat nuevo. Debe leerse completo antes de modificar el proyecto.

## Mensaje corto para pegar en un chat nuevo

> Lee completamente el archivo `RESPALDO_CONTEXTO_CODEX_MUNDO_APRENDO.md` ubicado en la raíz del proyecto y úsalo como contexto de continuidad. Inspecciona primero el estado real del proyecto y de Git. No reviertas, reemplaces ni borres cambios preexistentes. Continúa desde la sección “Trabajo pendiente después del reinicio”. Trabaja en español, de forma autónoma, conservadora y honesta sobre las pruebas realmente ejecutadas.

## Proyecto

- Proyecto Unity: `Mundo Aprendo`.
- Raíz Unity: `F:\PUN\Tesis-Mundo-Aprendo\Mundo aprendo tesis`.
- Raíz Git: `F:\PUN\Tesis-Mundo-Aprendo`.
- Versión detectada: Unity `6000.4.0f1`.
- Rama detectada durante la intervención: `main`.
- El proyecto tenía numerosos cambios locales antes de integrar el audio. No usar `git reset --hard`, `git checkout --`, restauraciones masivas ni limpiezas generales.

## Forma de colaborar solicitada por el usuario

- Hablar y reportar en español.
- Ser cercano, claro, directo y colaborativo.
- Empezar por el resultado o la conclusión importante.
- Dar actualizaciones breves mientras se trabaja.
- Trabajar de forma autónoma sin pedir confirmación por cada archivo.
- Inspeccionar primero y realizar la menor cantidad posible de modificaciones.
- No afirmar que una prueba pasó si no se ejecutó realmente.
- Explicar bloqueos y limitaciones con evidencia concreta.
- Conservar los cambios del usuario aunque el árbol de Git esté sucio.
- No realizar acciones destructivas ni ampliar el alcance sin autorización.
- Cuando se modifiquen archivos, verificar compilación y funcionamiento en proporción al riesgo.

## Regla principal del trabajo de audio

La integración debe ser exclusivamente de audio y aditiva.

No modificar:

- gameplay ni lógica de minijuegos;
- dificultad, condiciones de victoria o fallo;
- estrellas, progreso, desbloqueos o guardado;
- navegación, nombres de escenas o Build Settings;
- Vosk, reconocimiento de voz, micrófono, vocabulario o umbrales;
- Input System;
- UI visual, posiciones, anchors, escalas, textos, sprites o animaciones;
- secuencias ni notas del Mundo Musical;
- animales, hábitats o aleatoriedad del Mundo Tamaños;
- opciones, respuestas o Nuna del Mundo Emociones.

No crear un segundo administrador de audio. Reutilizar el sistema existente. No editar escenas salvo que sea estrictamente inevitable y esté justificado.

## Fuente de los audios

El usuario indicó que todos los audios ya estaban dentro del proyecto. No realizar nuevas descargas ni sustituirlos.

Carpeta local:

`Assets/Mundo Aprendo/Audios/`

Archivos canónicos integrados:

- `BGM_SeleccionMundos_ChildsPlay.mp3`
- `BGM_MundoMusical_Ukulele.mp3`
- `BGM_MundoCuentos_LittleWonders.mp3`
- `BGM_MundoTamanos_FunOnTheFarm.mp3`
- `BGM_MundoEmociones_ChildhoodEmotion.mp3`
- `SFX_UI_PopClick.mp3`
- `SFX_Result_CartoonPositive.wav`
- `SFX_Reward_MagicMarimba.wav`
- `SFX_Result_LevelComplete.mp3`

Los nueve archivos poseen `.meta` propio. Las músicas están configuradas como Streaming/Vorbis y los SFX cortos como Decompress On Load/PCM, en 2D.

La carpeta proporcionada también contiene copias duplicadas con nombres originales y `AVMCHECKER.exe`. No se ejecutaron ni eliminaron porque eran archivos preexistentes del usuario. Ninguno de ellos fue referenciado por la integración.

## Arquitectura encontrada

- Ya existía `Bolin.AudioManager` en `Assets/Mundo Aprendo/Scripts/AudioManager.cs`.
- Ya existía el prefab `Assets/Mundo Aprendo/Prefabs/Systems/AudioManager.prefab`.
- Cada una de las seis escenas del Build contiene exactamente una instancia de ese prefab.
- El administrador es local a cada escena; no utiliza `DontDestroyOnLoad`.
- Cada escena tiene exactamente un `AudioListener`.
- El prefab dispone de un `AudioSource` para música y otro para SFX.
- El menú ya tenía música y se conservó.
- El panel de resultados compartido es `Assets/Mundo Aprendo/Prefabs/UI/PanelResultado.prefab` y se utiliza en los cuatro mundos.

Escenas reales del Build, en el orden inspeccionado:

1. `Menu`
2. `SeleccionMundos`
3. `MundoMusical`
4. `MundoCuentos_VozTest`
5. `MundoTamanos`
6. `MundoEmociones`

## Integración realizada

### Música

| Escena | Archivo | Volumen base | Loop |
|---|---|---:|---|
| `Menu` | Música anterior del proyecto | Sin cambios | Sí |
| `SeleccionMundos` | `BGM_SeleccionMundos_ChildsPlay.mp3` | 0.27 | Sí |
| `MundoMusical` | `BGM_MundoMusical_Ukulele.mp3` | 0.27 | Sí |
| `MundoCuentos_VozTest` | `BGM_MundoCuentos_LittleWonders.mp3` | 0.12 | Sí |
| `MundoTamanos` | `BGM_MundoTamanos_FunOnTheFarm.mp3` | 0.27 | Sí |
| `MundoEmociones` | `BGM_MundoEmociones_ChildhoodEmotion.mp3` | 0.22 | Sí |

El volumen guardado en `PlayerPrefs` funciona como multiplicador del volumen base de cada escena. La música de Cuentos permanece baja y no se implementó ducking porque habría ampliado innecesariamente la integración con reconocimiento de voz.

### Clic de interfaz

- `SFX_UI_PopClick.mp3` es el clic central, a volumen 0.45.
- `AudioManager.TryPlayUiClick` utiliza una guardia por frame para evitar que los componentes antiguos y el feedback visual produzcan clic duplicado.
- `UIButtonFeedback` reproduce en clic confirmado o submit, no en pointer down.
- Los botones deshabilitados no producen audio.
- Los ocho botones de `SeleccionMundos` están cubiertos por `WorldCardView` o `UIButtonScaleFeedback`.
- Todos los botones UI de las demás escenas usan `UIButtonFeedback`.
- Las siete teclas `Key_1` a `Key_7` del Mundo Musical están excluidas intencionalmente para que conserven únicamente sus notas.

### Resultado y estrellas

- `SFX_Result_CartoonPositive.wav` se reproduce una vez cuando `WorldResultPanel.Show` presenta un nivel completado, a volumen 0.55.
- `SFX_Reward_MagicMarimba.wav` se reproduce una sola vez cuando comienza la presentación de estrellas, a volumen 0.50.
- Se corrigió el comportamiento anterior de `UIStarDisplay`, que habría reproducido el clip una vez por cada estrella.
- `SFX_Result_LevelComplete.mp3` está importado pero sin referencia runtime. Se dejó disponible sin forzar un tercer sonido simultáneo.

## Archivos modificados por la integración

- `Assets/Mundo Aprendo/Scripts/AudioManager.cs`
- `Assets/Mundo Aprendo/Scripts/Options/AudioOptionsController.cs`
- `Assets/Mundo Aprendo/Scripts/ButtonClickSound.cs`
- `Assets/Mundo Aprendo/Scripts/UI/UIButtonFeedback.cs`
- `Assets/Mundo Aprendo/Scripts/WorldSelection/UIButtonScaleFeedback.cs`
- `Assets/Mundo Aprendo/Scripts/WorldSelection/WorldCardView.cs`
- `Assets/Mundo Aprendo/Scripts/UI/WorldResultPanel.cs`
- `Assets/Mundo Aprendo/Scripts/UI/UIStarDisplay.cs`
- `Assets/Mundo Aprendo/Prefabs/Systems/AudioManager.prefab`
- `Assets/Mundo Aprendo/Prefabs/UI/PanelResultado.prefab`
- `Assets/Tests/PlayMode/ProjectPlayModeSmokeTests.cs`

En la prueba PlayMode existente se cambió la expectativa de `SeleccionMundos`: ahora debe encontrar `BGM_SeleccionMundos_ChildsPlay` en loop, en vez de esperar que no exista música.

No se editó ninguna escena durante esta intervención.

## Archivos permanentes creados

- `Assets/Mundo Aprendo/Audios.meta`
- Un `.meta` para cada uno de los nueve audios canónicos.
- Este documento de respaldo, creado por solicitud explícita del usuario.

No se creó un segundo AudioManager ni un nuevo sistema de gameplay.

## Validaciones realmente ejecutadas

- Los nueve archivos se decodificaron correctamente como MP3 o WAV.
- Todos son estéreo a 44,1 kHz.
- Sus duraciones coinciden con las esperadas y ninguno contiene silencio completo.
- Sus tamaños son razonables y los hashes de las copias integradas coinciden con los archivos verificados.
- Se comprobó que cada GUID nuevo es único.
- Se comprobó que cada clip utilizado tiene exactamente una referencia de prefab.
- `SFX_Result_LevelComplete` tiene cero referencias runtime, deliberadamente.
- Se confirmó estáticamente una instancia de `AudioManager` y un `AudioListener` por escena.
- Se verificó la cobertura de botones. Las únicas teclas sin Pop Click son las siete notas del Mundo Musical.
- `dotnet build Assembly-CSharp.csproj`: compilación correcta, 0 errores y 0 advertencias.
- `dotnet build MundoAprendo.PlayModeTests.csproj`: compilación correcta, 0 errores y 0 advertencias.
- `git diff --check` sobre los cambios de audio: correcto.

## Bloqueo encontrado

Unity no pudo completar la importación ni iniciar Play Mode porque este equipo no tenía una licencia válida del Editor activa.

Mensaje observado:

`No valid Unity Editor license found. Please activate your license.`

Código de salida informado por Unity: `198`.

Por esa razón todavía NO se ejecutaron:

- Play Mode real;
- Test Runner de Unity;
- recorrido manual completo por todas las escenas;
- reproducción audible dentro del juego;
- prueba física del micrófono y reconocimiento Vosk;
- revisión de la consola durante gameplay;
- build Windows.

No afirmar que estas pruebas pasaron hasta ejecutarlas después de activar Unity.

## Trabajo pendiente después del reinicio

1. Abrir Unity Hub y activar o confirmar la licencia de Unity Personal/Editor.
2. Antes de editar, ejecutar `git status` y conservar todos los cambios locales existentes.
3. Abrir `F:\PUN\Tesis-Mundo-Aprendo\Mundo aprendo tesis` con Unity `6000.4.0f1`.
4. Esperar a que termine la importación de los nueve audios y la recompilación.
5. Revisar la consola y separar errores preexistentes de errores introducidos por audio.
6. Confirmar en el Inspector que los cinco BGM, Pop Click, Cartoon Positive y Magic Marimba mantienen sus referencias.
7. Ejecutar los PlayMode tests del proyecto.
8. Probar el flujo completo:
   - `Menu` → `SeleccionMundos`;
   - `MundoMusical` → `SeleccionMundos`;
   - `MundoCuentos_VozTest` → `SeleccionMundos`;
   - `MundoTamanos` → `SeleccionMundos`;
   - `MundoEmociones`.
9. Confirmar en cada escena la pista correcta, loop, volumen y ausencia de músicas duplicadas.
10. Pulsar repetidamente botones normales y confirmar un solo Pop Click por pulsación.
11. Confirmar que los botones deshabilitados no suenan.
12. Completar al menos un nivel y confirmar Cartoon Positive una vez y Magic Marimba una vez con las estrellas.
13. Verificar que las siete teclas musicales reproducen solamente sus notas.
14. En Cuentos, probar físicamente inicialización de Vosk, micrófono, cuenta atrás, reconocimiento, texto reconocido, selección y retorno automático. Si la música interfiere, bajar solamente el volumen base; no modificar Vosk.
15. Confirmar que estrellas, progreso, desbloqueos, guardado y navegación permanecen iguales.
16. Ejecutar un build Windows de prueba si la licencia y los módulos instalados lo permiten.
17. Revisar el diff final y eliminar únicamente temporales creados para las pruebas.
18. Entregar un informe que distinga claramente pruebas ejecutadas, resultados y limitaciones.

## Estado de limpieza

Ya se eliminaron:

- el entorno Python temporal usado para validar audio;
- la copia temporal del proyecto usada para intentar abrir Unity;
- `Temp/bin` y `Temp/obj` generados por la compilación externa.

No borrar los clips, sus `.meta`, los prefabs, los scripts runtime ni este documento mientras siga siendo necesario como respaldo.

## Riesgos y cambios preexistentes que deben preservarse

Antes de la integración el repositorio ya estaba muy modificado. Entre otros elementos se observaron:

- cambios locales en `Menu.unity` y `MundoCuentos_VozTest.unity`;
- cambios locales en `MundoMusicalSequenceGame.cs` y en la suite PlayMode;
- numerosos `.meta` de carpetas marcados como eliminados;
- eliminación previa del modelo grande `vosk-model-es-0.42.zip`;
- el modelo activo pequeño `vosk-model-small-es-0.42.zip` sí existía;
- archivos académicos y builds marcados como eliminados o sin seguimiento;
- una carpeta `Juego Completo/` sin seguimiento.

No atribuir esos cambios al trabajo de audio y no revertirlos automáticamente.

## Informe final esperado cuando las pruebas puedan completarse

El informe debe contener:

- archivos creados;
- archivos modificados;
- música por escena;
- SFX integrados;
- pruebas realmente realizadas;
- estado específico de Mundo Cuentos/Vosk;
- errores de consola antes y después;
- resultado del build;
- limpieza realizada;
- limitaciones reales;
- estado `LISTO` o `LISTO CON LIMITACIONES`.

## Metodología recomendada para retomar

Usar un flujo conservador de Unity:

1. inspección y recuperación de contexto;
2. comprobación de conectividad del Editor;
3. validación antes de editar;
4. hipótesis pequeñas y verificables;
5. cambios exclusivamente de audio;
6. compilación;
7. Play Mode y pruebas funcionales;
8. build;
9. revisión del diff y limpieza final.

Las guías de trabajo que influyeron en esta intervención fueron `unity-project-onboarding`, `unity-mcp-workflow`, `unity-feature-implementation` y `unity-build-validation`.

