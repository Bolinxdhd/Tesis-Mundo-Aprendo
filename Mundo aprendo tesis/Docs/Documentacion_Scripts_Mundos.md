# Documentacion de scripts de mundos - Mundo Aprendo

Fecha de documentacion: 2026-06-30
Proyecto: Mundo Aprendo
Unity objetivo: 6000.4.0f1

## Alcance

Este documento describe solo los scripts directamente asociados a los mundos o minijuegos:

- Mundo Musical.
- Mundo de Cuentos.
- Mundo de los Tamanos.
- Mundo de las Emociones.

No se documentan aqui scripts generales de menu, opciones, navegacion, accesibilidad global, UI compartida o progreso global, salvo cuando un mundo los usa como dependencia. Por ejemplo, `WorldProgressRepository`, `SceneNavigation`, `UIStarDisplay`, `UIProgressBar` y `StarRatingCalculator` se mencionan como dependencias, pero no se explican como scripts de mundo.

## Mapa Rapido

| Mundo | Escena principal | Scripts documentados |
| --- | --- | --- |
| Musical | `Assets/Scenes/MundoMusical.unity` | `MundoMusicalSequenceGame` |
| Cuentos | `Assets/Scenes/MundoCuentos_VozTest.unity` | `VoiceRecognitionTest`, `ReadingEvaluator`, `ReadingEvaluationResult`, `StoryProgressRepository`, `ISpeechToTextService`, `WindowsDictationSpeechService` |
| Tamanos | `Assets/Scenes/MundoTamanos.unity` | `SizeWorldController`, `SizeWorldAnimationController`, `SizeWorldSceneBuilder` |
| Emociones | `Assets/Scenes/Mundos/MundoEmociones.unity` | `EmotionGameManager`, `EmotionRoundView`, `EmotionAnswerButton`, `EmotionResultPanel`, `EmotionGameAnimationController` |

## Reglas de Mantenimiento

- No renombrar clases `MonoBehaviour` sin migracion de escenas/prefabs.
- No renombrar campos `[SerializeField]` sin usar `FormerlySerializedAs`.
- No eliminar metodos publicos conectados desde botones del Inspector.
- No cambiar claves de `PlayerPrefs`.
- No cambiar nombres de escenas usados por `SceneNavigation`.
- Mantener separada la logica del juego de la animacion cuando sea posible.
- Al modificar cualquier mundo, validar compilacion y pruebas de escenas.

## Mundo Musical

### `MundoMusicalSequenceGame`

Ruta: `Assets/Mundo Aprendo/Scripts/MundoMusical/Scripts/MundoMusicalSequenceGame.cs`

Namespace: `Assets.SurpriseBox.Scripts`

Tipo: `MonoBehaviour`

Responsabilidad:

- Controla el minijuego musical basado en secuencias de notas.
- Reproduce la secuencia configurada.
- Valida la tecla esperada.
- Cuenta errores.
- Calcula estrellas.
- Guarda el progreso del mundo musical.
- Actualiza textos, botones, colores de teclas y progreso visual.

Clases internas:

- `PianoKeyConfig`: datos configurables de cada tecla.
  - `id`: identificador logico de la nota.
  - `displayName`: texto visible.
  - `normalColor`: color base.
  - `highlightColor`: color al iluminar.
  - `toneFrequency`: frecuencia usada si no hay clip.
  - `audioClip`: sonido opcional.
- `PianoStep`: paso de una secuencia.
  - `keyIndex`: indice de tecla esperada.
  - `delayBefore`: pausa antes de reproducir el paso.
- `PianoSequence`: secuencia completa.
  - `sequenceName`: nombre visible.
  - `steps`: pasos de notas que el jugador debe repetir.

Campos principales del Inspector:

- Configuracion:
  - `playSequenceOnStart`: reproduce secuencia al iniciar.
  - `repeatOnMistake`: repite secuencia tras error.
  - `highlightDuration`: duracion visual del resaltado.
  - `mistakeReplayDelay`: espera antes de repetir por error.
  - `delayBeforeNextSequence`: espera antes de avanzar.
  - `autoAdvanceSequence`: avance automatico al terminar una secuencia.
- Piano:
  - `pianoKeys`: configuracion de notas.
- Secuencias:
  - `sequences`: lista de secuencias jugables.
- UI:
  - textos TMP de titulo, estado y secuencia.
  - botones de inicio, siguiente y teclas.
  - `AudioSource`.
  - paneles de inicio, piano y resultado.
  - textos legacy ocultos con `FormerlySerializedAs`.
- Estrellas:
  - `starImages`, `fullStarSprite`, `emptyStarSprite`, `starDisplay`.
- Progreso visual:
  - `sequenceStepIndicators`, `sequenceStepLabels`.

Metodos publicos:

- `StartActivity()`: inicia la actividad desde el panel inicial.
- `RestartActivity()`: reinicia el mundo.
- `CompleteActivity()`: finaliza, guarda progreso y muestra resultado.
- `RegisterMistake()`: incrementa errores y recalcula estrellas.
- `UpdateStarsUi()`: refresca estrellas.
- `ReturnToWorldSelection()`: vuelve a seleccion de mundos.
- `PlayCurrentSequence()`: reproduce la secuencia actual.
- `NextSequence()`: avanza manualmente a la siguiente secuencia.
- `SelectSequence(int index)`: selecciona una secuencia por indice.

Flujo:

1. `Awake()` resuelve referencias faltantes, configura botones y datos por defecto.
2. `Start()` prepara estado inicial y opcionalmente reproduce.
3. `StartActivity()` muestra el panel de piano y deja lista la primera secuencia.
4. `PlayCurrentSequence()` inicia `PlaySequenceRoutine()`.
5. Cada tecla llama internamente a `OnKeyPressed(int keyIndex)`.
6. Si la nota es correcta, avanza el indice esperado.
7. Si hay error, llama `RegisterMistake()` y puede repetir la secuencia.
8. Al completar secuencias, `CompleteActivity()` guarda estrellas.

Persistencia:

- Usa `WorldProgressRepository.SaveBestResult(WorldIndex, currentStars)`.
- `WorldIndex` del mundo musical: `0`.

Corrutinas importantes:

- `PlaySequenceRoutine()`: reproduccion guiada.
- `AdvanceAfterSequenceCompleteRoutine()`: espera y avanza.
- `ReplayAfterMistakeRoutine()`: repeticion tras error.
- `HighlightKeyRoutine()`: resaltado visual.
- `FlashKeyRoutine()`: feedback de tecla.

Dependencias:

- `Bolin.WorldProgressRepository`.
- `Bolin.SceneNavigation`.
- `Bolin.UIStarDisplay`.
- TextMeshPro, UI y AudioSource.

Riesgos:

- Muchos campos estan serializados en escena.
- Los textos legacy existen para conservar referencias antiguas.
- No cambiar namespace o nombre de clase sin revisar escena.
- No modificar indices de teclas sin actualizar `pianoKeys`, botones y secuencias.

## Mundo de Cuentos

### `VoiceRecognitionTest`

Ruta: `Assets/Mundo Aprendo/Scripts/MundoCuentos/Scripts/VoiceRecognitionTest.cs`

Namespace: `Bolin`

Tipo: `MonoBehaviour`

Responsabilidad:

- Controla la seleccion de cuentos.
- Abre un cuento por id.
- Muestra texto de lectura.
- Inicia reconocimiento de voz.
- Ejecuta cuenta regresiva.
- Detecta silencio y tiempo maximo.
- Valida coincidencia de lectura.
- Muestra resultado y estrellas.
- Guarda progreso por cuento.
- Desbloquea acceso a otros mundos cuando se completa la cantidad requerida.

Clases auxiliares en el mismo archivo:

- `PalabraReconocida`:
  - representa una palabra mostrada en el texto reconocido.
  - guarda id, texto original, texto normalizado, estado correcto e instante de creacion.
- `CuentoData`:
  - datos serializables de un cuento.
  - `id`, `titulo`, `descripcion`, `textoCompleto`, `icono`.
- `CuentoCardView`:
  - referencias visuales de una tarjeta de cuento.
  - `cuentoId`, `button`, `iconImage`, `titleText`, `completedText`, `starsText`.

Campos principales del Inspector:

- Paneles:
  - seleccion, lectura y resultado.
- Seleccion de cuentos:
  - lista `cuentosDisponibles`.
  - lista `cuentoCards`.
  - textos de progreso y desbloqueo.
  - botones de retorno y otros mundos.
- Lectura:
  - titulo, icono, texto del cuento, texto reconocido, placeholder, texto parcial/final, estado, resultado, countdown, errores y estrellas.
- Resultado:
  - titulo, puntaje, estrellas y mensaje final.
- Botones:
  - iniciar, detener, reintentar, validar, limpiar, regresar a seleccion.
- Desplazamiento:
  - `ScrollRect` de texto reconocido y cuerpo del cuento.
- Estrellas:
  - imagenes, sprites y `UIStarDisplay`.
- Configuracion:
  - `story`, escenas de retorno, cantidad de cuentos requeridos, retraso de retorno.
- Validacion:
  - umbrales de exito y estrellas.
  - `removeCommonWords`.
  - `completeOnlyWithAtLeastOneStar`.
- Preparacion y silencio:
  - countdown, timeout de silencio, tiempo maximo, palabras minimas.
- Diagnostico de microfono:
  - segundos de preflight.
  - senal minima.
- Palabras temporales:
  - vida visual de palabras incorrectas.
  - color para palabra incorrecta.

Metodos publicos conectables a UI:

- `StartListening()`: inicia countdown y reconocimiento.
- `StopListening()`: detiene reconocimiento.
- `RetryReading()`: limpia lectura y reintenta.
- `ClearRecognizedText()`: limpia texto reconocido.
- `ValidateReading()`: evalua coincidencia y guarda progreso.
- `ReturnToMenu()`: vuelve a la escena configurada.
- `MostrarSeleccionCuentos()`: vuelve al panel de seleccion.
- `OpenStoryById(string storyId)`: abre cuento especifico.
- `OpenOtherWorlds()`: navega a otros mundos si esta desbloqueado.

Flujo:

1. `Awake()` crea `ReadingEvaluator`, `StoryProgressRepository` y servicio de voz.
2. Configura historias por defecto si no hay datos.
3. Conecta botones opcionales.
4. Muestra tarjetas y progreso de cuentos.
5. El jugador abre un cuento con `OpenStoryById()`.
6. `MostrarLectura()` carga titulo, icono, texto y estado inicial.
7. `StartListening()` valida microfono y ejecuta countdown.
8. El servicio de voz entrega resultados parciales y finales.
9. `Update()` revisa silencio y tiempo maximo solo mientras se escucha.
10. `ValidateReading()` calcula similitud y estrellas.
11. `MostrarResultado()` presenta puntaje y programa retorno.

Persistencia:

- Usa `StoryProgressRepository`.
- Por defecto, el mundo de cuentos corresponde al `WorldIndex = 1`.
- El mundo se marca como completado en progreso global cuando hay al menos dos cuentos completados.

Dependencias:

- `ReadingEvaluator`.
- `ReadingEvaluationResult`.
- `StoryProgressRepository`.
- `ISpeechToTextService`.
- `WindowsDictationSpeechService`.
- `MicrophoneSettings`.
- `SceneNavigation`.
- `UIStarDisplay`.

Riesgos:

- Varios metodos publicos estan conectados por botones en la escena.
- El reconocimiento de voz depende de configuracion de Windows y del microfono predeterminado del sistema.
- El campo `recognizedPlaceholderText` controla un placeholder separado del texto reconocido.
- No cambiar ids de cuentos sin considerar claves guardadas en `PlayerPrefs`.
- No eliminar `Update()`; solo corre control de timeouts durante escucha activa.

### `ReadingEvaluator`

Ruta: `Assets/Mundo Aprendo/Scripts/MundoCuentos/Scripts/ReadingEvaluator.cs`

Tipo: clase de logica pura.

Responsabilidad:

- Normalizar texto.
- Separar palabras.
- Omitir palabras comunes si esta configurado.
- Comparar lectura esperada vs lectura reconocida.
- Calcular similitud.
- Asignar estrellas segun umbrales.

Metodos publicos:

- `ReadingEvaluator(bool removeCommonWords)`: configura si se filtran palabras comunes.
- `Evaluate(string expected, string recognized, float threeStarsThreshold, float twoStarsThreshold, float oneStarThreshold)`: devuelve resultado de lectura.
- `CountWords(string text)`: cuenta palabras normalizadas.
- `NormalizeText(string input)`: normaliza a minusculas, elimina tildes y puntuacion.
- `GetNormalizedWords(string input)`: devuelve palabras normalizadas.

Salida:

- Devuelve `ReadingEvaluationResult` con similitud, estrellas, palabras coincidentes, palabras esperadas y palabras reconocidas.

Riesgos:

- Cambiar la normalizacion cambia la puntuacion del jugador.
- Cambiar `commonWords` modifica el porcentaje de coincidencia.

### `ReadingEvaluationResult`

Ruta: `Assets/Mundo Aprendo/Scripts/MundoCuentos/Scripts/ReadingEvaluationResult.cs`

Tipo: `readonly struct`.

Responsabilidad:

- Transportar el resultado de una evaluacion de lectura.

Propiedades:

- `Similarity`: valor de 0 a 1.
- `Stars`: estrellas obtenidas.
- `MatchedWords`: palabras coincidentes.
- `ExpectedWords`: palabras esperadas.
- `RecognizedWords`: palabras reconocidas.

### `StoryProgressRepository`

Ruta: `Assets/Mundo Aprendo/Scripts/MundoCuentos/Scripts/StoryProgressRepository.cs`

Tipo: repositorio de progreso de cuentos.

Responsabilidad:

- Guardar mejor puntaje por cuento.
- Guardar mejores estrellas por cuento.
- Marcar cuentos completados.
- Registrar ids de cuentos conocidos.
- Calcular cuentos completados.
- Reiniciar progreso de cuentos.
- Sincronizar progreso del mundo de cuentos con `WorldProgressRepository`.

Constantes:

- `WorldIndex = 1`.
- `DefaultStoryId = "tres_cerditos"`.
- `CompletedKey = "MundoAprendo_World_1_Completed"`.
- `StarsKey = "MundoAprendo_World_1_Stars"`.

Claves `PlayerPrefs` por cuento:

- `MundoCuentos_Completado_{id}`.
- `MundoCuentos_Puntaje_{id}`.
- `MundoCuentos_Estrellas_{id}`.
- `MundoCuentos_CuentosRegistrados`.

Metodos principales:

- `Save(int stars, bool completeOnlyWithAtLeastOneStar)`.
- `SaveStoryResult(string cuentoId, int score, int stars, bool completeOnlyWithAtLeastOneStar)`.
- `IsStoryCompleted(string cuentoId)`.
- `GetBestStoryScore(string cuentoId)`.
- `GetBestStoryStars(string cuentoId)`.
- `CountCompletedStories(IEnumerable<CuentoData> cuentos)`.
- `HasUnlockedOtherWorlds(IEnumerable<CuentoData> cuentos, int requiredCompletedStories)`.
- `GetStoryCompletedKey(string cuentoId)`.
- `GetStoryScoreKey(string cuentoId)`.
- `GetStoryStarsKey(string cuentoId)`.
- `ResetAllStories(bool save = true)`.
- `ResetStory(string cuentoId, bool save = true)`.

Regla de completado global:

- Si hay dos o mas cuentos completados, guarda el mundo de cuentos como completado.
- Si hay menos de dos, actualiza mejores estrellas sin marcar el mundo completo.

Riesgos:

- No cambiar formatos de claves sin migracion.
- No cambiar `DefaultStoryId` si ya hay partidas guardadas.
- `NormalizeStoryId()` transforma ids a texto normalizado con guiones bajos.

### `ISpeechToTextService`

Ruta: `Assets/Mundo Aprendo/Scripts/MundoCuentos/Scripts/ISpeechToTextService.cs`

Tipo: interfaz.

Responsabilidad:

- Definir contrato comun para servicios de reconocimiento de voz.

Eventos:

- `OnPartialResult`: texto parcial.
- `OnFinalResult`: texto final.
- `OnError`: error amigable.
- `OnStatusChanged`: estado del servicio.

Propiedades:

- `IsListening`: indica si el servicio esta escuchando.

Metodos:

- `StartListening()`.
- `StopListening()`.
- `DisposeService()`.

Uso:

- `VoiceRecognitionTest` depende de esta interfaz para desacoplar el controlador del servicio concreto.

### `WindowsDictationSpeechService`

Ruta: `Assets/Mundo Aprendo/Scripts/MundoCuentos/Scripts/WindowsDictationSpeechService.cs`

Tipo: implementacion de `ISpeechToTextService`.

Responsabilidad:

- Encapsular `DictationRecognizer`.
- Gestionar inicio, parada, errores y liberacion del reconocedor.
- Traducir causas de finalizacion a mensajes amigables.

Condicion de plataforma:

- Usa `UnityEngine.Windows.Speech` solo en `UNITY_STANDALONE_WIN` o `UNITY_EDITOR_WIN`.
- En otras plataformas informa que el reconocimiento no esta disponible.

Metodos publicos:

- `StartListening()`: valida microfono, crea reconocedor e inicia dictado.
- `StopListening()`: detiene el dictado.
- `DisposeService()`: desuscribe eventos y libera recursos.

Eventos que emite:

- Texto parcial.
- Texto final.
- Error.
- Estado.

Notas importantes:

- Unity no permite pasar directamente el dispositivo elegido en `Microphone.devices` al `DictationRecognizer`.
- El reconocedor usa el microfono predeterminado de Windows.
- El microfono seleccionado en opciones se usa como validacion previa y diagnostico.

Riesgos:

- Depende de permisos y configuracion de voz de Windows.
- No eliminar `DisposeService()`, porque evita fugas de eventos del recognizer.

## Mundo de los Tamanos

### `SizeWorldController`

Ruta: `Assets/Mundo Aprendo/Scripts/SizeWorldController.cs`

Namespace: `Bolin`

Tipo: `MonoBehaviour`

Responsabilidad:

- Controla el minijuego de elegir el animal mas grande o mas pequeno.
- Selecciona habitat y animales comparables.
- Decide el objetivo de cada ronda.
- Valida respuesta.
- Cuenta errores.
- Calcula estrellas.
- Guarda progreso.
- Controla botones y texto de feedback.
- Delega animaciones visuales a `SizeWorldAnimationController`.

Tipos definidos:

- `AnimalSizeType`: `Pequeno` o `Grande`.
- `AnimalData`: datos de animal.
  - `animalName`, `animalSprite`, `sizeType`, `minScale`, `maxScale`.
- `HabitatData`: datos de habitat.
  - `habitatName`, `backgroundSprite`, `animals`.

Eventos:

- `OnRoundStarted`: se invoca al iniciar una ronda.
- `OnAnswerValidated(bool isCorrect)`: se invoca al validar respuesta.
- `OnActivityCompleted(int stars)`: se invoca al completar actividad.

Campos principales del Inspector:

- `habitats`: lista de habitats y animales.
- Imagenes de fondo y animales.
- Textos de pregunta, feedback y nombres de animales.
- Botones de animales y retorno.
- Outlines de seleccion.
- Area segura para animales.
- Panel y texto de resultado.
- Estrellas y `UIStarDisplay`.
- Parametros de animacion: entrada, pulso y demora.
- Parametros de posicion y escala.
- Flujo: rondas requeridas, escena de retorno, inicio automatico.

Metodos publicos:

- `StartActivity()`: inicia partida desde cero.
- `RestartActivity()`: reinicia partida.
- `SelectLeftAnimal()`: selecciona animal izquierdo.
- `SelectRightAnimal()`: selecciona animal derecho.
- `ReturnToWorldSelection()`: vuelve a seleccion de mundos.
- `RegisterMistake()`: registra error y recalcula estrellas.
- `CompleteActivity()`: finaliza, guarda progreso y muestra resultado.

Flujo:

1. `Awake()` configura botones y estado inicial.
2. `Start()` inicia automaticamente si `startAutomatically` esta activo.
3. `StartActivity()` reinicia contadores y llama `StartNextRound()`.
4. `StartNextRound()` elige habitat, animales y pregunta.
5. Se muestra una entrada animada de animales.
6. El jugador pulsa izquierda o derecha.
7. `SubmitAnimalChoice()` compara referencia del animal elegido con `targetAnimal`.
8. Si acierta, aumenta rondas completadas y avanza.
9. Si falla, registra error y permite reintentar.
10. Al completar rondas, guarda progreso.

Persistencia:

- Usa `WorldProgressRepository.SaveBestResult(WorldIndex, currentStars)`.
- `WorldIndex = 2`.

Dependencias:

- `SizeWorldAnimationController`.
- `WorldProgressRepository`.
- `SceneNavigation`.
- `UIStarDisplay`.

Riesgos:

- Los datos de animales/habitats son serializados.
- No cambiar tipos de `AnimalData` o `HabitatData` sin revisar escena.
- No eliminar metodos publicos conectados a botones.
- La seleccion correcta se basa en referencias a los objetos `AnimalData`, no solo en nombres.

### `SizeWorldAnimationController`

Ruta: `Assets/Mundo Aprendo/Scripts/MundoTamanos/SizeWorldAnimationController.cs`

Tipo: clase estatica interna.

Responsabilidad:

- Concentrar animaciones visuales del mundo de tamanos.
- Mantener el controlador principal enfocado en mecanica.

Metodos:

- `PlayRoundEntrance(...)`: mueve animales desde fuera de pantalla hasta su posicion.
- `Pulse(...)`: escala un objetivo hacia arriba y vuelve a su escala base.
- `ShakeHorizontal(...)`: sacude horizontalmente una respuesta incorrecta.
- `ApplyDepthScale(...)`: calcula escala visual segun posicion vertical y rangos configurados.

Uso:

- Solo debe controlar transformaciones visuales.
- No decide respuestas, estrellas ni progreso.

### `SizeWorldSceneBuilder`

Ruta: `Assets/Mundo Aprendo/Scripts/MundoTamanos/Editor/SizeWorldSceneBuilder.cs`

Tipo: script de editor.

Responsabilidad:

- Construir o reconstruir la escena `Assets/Scenes/MundoTamanos.unity`.
- Crear canvas, camara, event system, paneles, botones y assets graficos simples.
- Asignar referencias serializadas al `SizeWorldController`.
- Agregar escena a Build Settings.
- Actualizar la escena de seleccion de mundos cuando corresponde.

Entrada principal:

- `Build()`: metodo de menu/editor para generar la escena.

Rutas importantes:

- `ScenePath = "Assets/Scenes/MundoTamanos.unity"`.
- `ArtFolder = "Assets/Mundo Aprendo/MundoTamanos/Arte"`.
- `WorldSelectionScenePath = "Assets/Scenes/SeleccionMundos.unity"`.

Riesgos:

- Es un script destructivo a nivel de escena: reconstruye objetos.
- No ejecutarlo sin respaldo si la escena tiene cambios manuales importantes.
- No es runtime; pertenece a `Editor`.

## Mundo de las Emociones

### `EmotionRoundView`

Ruta: `Assets/Mundo Aprendo/Scripts/MundoEmociones/EmotionRoundView.cs`

Tipos:

- `EmotionType`: `Joy`, `Sadness`, `Anger`, `Surprise`, `Fear`.
- `EmotionRoundView`: datos serializables para cada emocion.

Campos:

- `emotion`: tipo de emocion.
- `rootObject`: objeto raiz de la vista.
- `animatedRect`: rect transform animado.
- `canvasGroup`: grupo para fades.
- `displayName`: nombre visible.
- `instructionAudio`: audio opcional.

Uso:

- `EmotionGameManager` usa esta estructura para elegir rondas y mostrar la expresion activa.

### `EmotionGameManager`

Ruta: `Assets/Mundo Aprendo/Scripts/MundoEmociones/EmotionGameManager.cs`

Tipo: `MonoBehaviour`

Responsabilidad:

- Controla el flujo del mundo de emociones.
- Elige emociones por ronda.
- Valida respuestas.
- Cuenta aciertos y errores.
- Calcula estrellas.
- Guarda progreso.
- Maneja panels, textos, feedback y audio.
- Delega animaciones visuales a `EmotionGameAnimationController`.

Constantes:

- `WorldIndex = 3`.
- `CompletedKey = "MundoAprendo_World_3_Completed"`.
- `StarsKey = "MundoAprendo_World_3_Stars"`.

Eventos:

- `OnRoundStarted(EmotionType emotion)`.
- `OnAnswerValidated(bool isCorrect)`.
- `OnActivityCompleted(int stars)`.

Campos principales del Inspector:

- Rondas:
  - `emotionViews`.
  - `answerButtons`.
  - `includeFear`.
  - `totalRounds`.
  - duraciones de feedback.
- Estrellas por errores:
  - limites para 3, 2 y 1 estrella.
- Paneles:
  - inicio, juego, feedback, resultado.
- Textos:
  - instruccion, progreso, feedback y dialogo de Nuna.
- Feedback:
  - icono correcto e icono de reintento.
- Nuna:
  - rect transform y parametros de flotacion.
- Audio:
  - musica, sfx y clips de estado.

Metodos publicos:

- `StartActivity()`: comienza actividad.
- `RestartActivity()`: reinicia actividad.
- `SubmitAnswer(EmotionType selectedEmotion)`: valida emocion elegida.
- `ReturnToWorldSelection()`: vuelve a seleccion de mundos.

Flujo:

1. `Awake()` prepara estado inicial.
2. `Start()` inicia flotacion de Nuna y musica ambiente.
3. `StartActivity()` resetea contadores, oculta inicio y muestra juego.
4. `ShowNextRound()` elige una emocion evitando repetir la anterior cuando sea posible.
5. El jugador pulsa un `EmotionAnswerButton`.
6. `SubmitAnswer()` compara seleccion con `currentView.emotion`.
7. Si acierta, avanza luego de feedback.
8. Si falla, permite reintento.
9. `CompleteActivity()` calcula estrellas, guarda progreso y muestra resultado.

Persistencia:

- Usa `WorldProgressRepository.SaveBestResult(WorldIndex, stars)`.
- `WorldIndex = 3`.

Dependencias:

- `EmotionRoundView`.
- `EmotionAnswerButton`.
- `EmotionResultPanel`.
- `EmotionGameAnimationController`.
- `WorldProgressRepository`.
- `StarRatingCalculator`.
- `SceneNavigation`.
- `UIProgressBar`.

Riesgos:

- `includeFear` controla visibilidad de miedo.
- Los botones dependen de que cada `EmotionAnswerButton` tenga `EmotionType` correcto.
- No cambiar limites de estrellas sin revisar balance.
- No cambiar textos o paneles serializados sin revisar escena.

### `EmotionAnswerButton`

Ruta: `Assets/Mundo Aprendo/Scripts/MundoEmociones/EmotionAnswerButton.cs`

Tipo: `MonoBehaviour`

Responsabilidad:

- Representa un boton de respuesta de emocion.
- Envia la respuesta al `EmotionGameManager`.
- Controla interactividad, visibilidad y pulso visual.

Campos principales:

- `gameManager`: manager receptor.
- `emotion`: emocion asociada al boton.
- `button`: componente UI.
- `pulseTarget`: objetivo visual para pulso.
- `iconImage`, `labelText`.
- `pulseDuration`, `pulseScale`.

Metodos publicos:

- `Submit()`: envia `emotion` al manager.
- `SetInteractable(bool interactable)`.
- `SetVisible(bool visible)`.
- `Pulse()`.

Riesgos:

- Si `gameManager` no esta asignado, el boton no envia respuesta.
- Si `emotion` no coincide con la opcion visual, se validara una emocion incorrecta.

### `EmotionResultPanel`

Ruta: `Assets/Mundo Aprendo/Scripts/MundoEmociones/EmotionResultPanel.cs`

Tipo: `MonoBehaviour`

Responsabilidad:

- Muestra resultado final del mundo de emociones.
- Presenta aciertos, errores y estrellas.
- Reproduce estrellas con delay y sonido si no hay `UIStarDisplay`.

Campos:

- `rootObject`.
- `canvasGroup`.
- textos de resultado.
- imagenes y sprites de estrellas.
- `UIStarDisplay`.
- `fadeDuration`.
- `delayBetweenStars`.

Metodos publicos:

- `Show(int correctAnswers, int mistakes, int stars, AudioSource sfxSource, AudioClip starClip)`.
- `Hide()`.

Riesgos:

- Requiere `rootObject` para mostrarse.
- Si hay `UIStarDisplay`, delega la animacion de estrellas y no usa el flujo manual.

### `EmotionGameAnimationController`

Ruta: `Assets/Mundo Aprendo/Scripts/MundoEmociones/EmotionGameAnimationController.cs`

Tipo: clase estatica interna.

Responsabilidad:

- Concentrar animaciones visuales del mundo de emociones.
- Evitar que `EmotionGameManager` contenga interpolaciones directas.

Metodos:

- `FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration)`.
- `PlayEmotionEntrance(EmotionRoundView view, float duration)`.
- `FloatAnchoredPosition(RectTransform target, float distance, float speed)`.

Uso:

- No valida respuestas.
- No guarda progreso.
- No decide estrellas.

## Dependencias Compartidas Usadas por los Mundos

Estas dependencias no son documentadas como scripts de mundo, pero aparecen en el flujo:

- `WorldProgressRepository`: guarda completado y estrellas globales por mundo.
- `SceneNavigation`: carga escenas de retorno.
- `MundoAprendoSceneNames`: centraliza nombres de escenas.
- `StarRatingCalculator`: calcula estrellas por errores.
- `UIStarDisplay`: muestra estrellas.
- `UIProgressBar`: muestra progreso de rondas.
- `MicrophoneSettings`: seleccion de microfono para Cuentos.

## Claves de Progreso por Mundo

| Mundo | Indice global | Script que guarda | Notas |
| --- | --- | --- | --- |
| Musical | 0 | `MundoMusicalSequenceGame` | Guarda mejores estrellas al completar actividad. |
| Cuentos | 1 | `StoryProgressRepository` | Guarda por cuento y marca mundo completo al completar cuentos suficientes. |
| Tamanos | 2 | `SizeWorldController` | Guarda al completar rondas. |
| Emociones | 3 | `EmotionGameManager` | Guarda al completar actividad. |

## Metodos Publicos que no se deben eliminar sin revisar Inspector

- Musical:
  - `StartActivity`, `RestartActivity`, `CompleteActivity`, `RegisterMistake`, `UpdateStarsUi`, `ReturnToWorldSelection`, `PlayCurrentSequence`, `NextSequence`, `SelectSequence`.
- Cuentos:
  - `StartListening`, `StopListening`, `RetryReading`, `ClearRecognizedText`, `ValidateReading`, `ReturnToMenu`, `MostrarSeleccionCuentos`, `OpenStoryById`, `OpenOtherWorlds`.
- Tamanos:
  - `StartActivity`, `RestartActivity`, `SelectLeftAnimal`, `SelectRightAnimal`, `ReturnToWorldSelection`, `RegisterMistake`, `CompleteActivity`.
- Emociones:
  - `EmotionGameManager.StartActivity`, `RestartActivity`, `SubmitAnswer`, `ReturnToWorldSelection`.
  - `EmotionAnswerButton.Submit`, `SetInteractable`, `SetVisible`, `Pulse`.
  - `EmotionResultPanel.Show`, `Hide`.

## Recomendaciones para Futuro

- Si se agregan mas mundos, mantener un `WorldIndex` unico y documentarlo.
- Para mundos nuevos, separar desde el inicio:
  - controlador de mecanica.
  - controlador de animacion.
  - repositorio de progreso si tiene progreso propio.
  - datos configurables.
- Evitar que scripts de animacion guarden progreso o calculen estrellas.
- Evitar que scripts de mecanica conozcan detalles internos de animaciones.
- Agregar pruebas de escena si se conectan nuevos botones por Inspector.

