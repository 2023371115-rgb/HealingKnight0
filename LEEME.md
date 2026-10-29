# Healing Knight

*Run and gun* 2D de un jugador (estilo **Metal Slug**) hecho en Unity 6. Un nanobot médico recorre los sistemas del cuerpo del Paciente 001 y derrota enfermedades usando el medicamento correcto.

**Entrega de diciembre:** Sistema Digestivo: Estómago → Intestino delgado (la lombriz aparece y huye) → Intestino grueso (pelea final con la lombriz). La **Boca** es un nivel secreto: se desbloquea al tocar un pasaje escondido en el Estómago.

**Siguiente (prototipo listo):** Sistema Respiratorio: Nariz (rinovirus, polen, S. aureus) → Tráquea (tos ferina) → Pulmones (neumonía, influenza y el jefe volador: la **colmena de tuberculosis**; da la fórmula 2 y cierra con el aviso de que la infección va al cerebro). Mecánica del GDD: el paso por los bronquiolos está bloqueado por **mucosidad** (Mucolítico; te frena si la tocas) e **inflamación** (Antiinflamatorio; se contrae al dañarla). El **polen** es un alérgeno: Antihistamínico.

**Prototipo del Sistema Nervioso:** Médula espinal (poliovirus, meningococo, rabia; aprendes las **tormentas eléctricas**: un destello avisa 1 s dónde cae el rayo de 2 u y el escudo no protege) → Cerebro (herpes y los demás entre **neuronas sanas**: tus cápsulas las atraviesan, pero los disparos enemigos las dañan (−3 de **salud del cerebro**) y las infectan. Las neuronas rojas sueltan descargas y desgastan el cerebro hasta que las calmas. Si la salud del cerebro llega a 0, desenlace negativo y se reinicia el nivel) → **Hipocampo** (jefe: el **virus de la rabia** como el ciempiés de Centipede: una cadena de viriones que baja de riel en cada orilla, se parte en dos si rompes el medio, invoca rayos, infecta neuronas y en furia corre a ras de piso; da la fórmula 3). El **Anticonvulsivo** es la cápsula regional: calma neuronas rojas (+5 cerebro) y su bomba deja un campo neuroprotector.

---

## Primer uso (5 minutos)

1. Abre el proyecto en Unity (6000.6.3f1). La primera vez tarda en importar.
2. En la barra de menús: **Healing Knight → Crear proyecto base**.
   Crea los sprites de prueba, los datos (medicamentos, enemigos, sistemas), los prefabs y las escenas.
   Se puede volver a ejecutar: **nunca sobrescribe** lo que ya existe.
3. Abre `Assets/_Project/Scenes/MainMenu.unity` y dale **Play**.
4. Haz commit de todo lo que se generó, incluidos los archivos `.meta`.

Para probar mecánicas sin pasar por el menú, abre **Healing Knight → Abrir escena → Sandbox** (tiene todos los enemigos y al jefe).

Si cambia la estructura del proyecto base (nuevos enemigos, niveles, etc.) y todavía no has personalizado los assets generados, usa **Healing Knight → Regenerar proyecto base**: borra `Data`, `Prefabs`, `Scenes` y `Art/Placeholders` de `_Project` y los vuelve a crear.

## Controles

Apuntas **libre con el mouse** (o el stick derecho del control). Si no usas el mouse, apuntas en 8 direcciones como en Metal Slug: W arriba, W + A/D en diagonal, S abajo en el aire. Mover el mouse cambia al apuntado con mouse; presionar W regresa al de teclas.

| Acción | Teclado / mouse | Gamepad |
|---|---|---|
| Moverse | A / D | Stick izq. / cruceta |
| Apuntar libre (miras hacia donde apuntas) | Mouse | Stick der. |
| Apuntar en 8 direcciones | W, W + A/D, S en el aire | Stick izq. |
| Agacharse | S | Abajo |
| Disparar (mantener = ráfaga). Si un enemigo está pegado, **cuchillazo** automático. Disparar hacia abajo en el aire frena la caída | J / clic izquierdo | X / gatillo der. |
| Escudo burbuja (mantener) — levántalo justo antes del golpe para hacer **parry** | K / clic derecho | Gatillo izq. |
| Saltar (altura fija) · **doble salto** en el aire | Espacio | A / LB |
| **Caída lenta**: mantén saltar mientras caes (1.5 s, se recarga al pisar el suelo) | mantener Espacio | mantener A / LB |
| **Bomba de medicamento** (granada). Empiezas con 5; la barra bajo el contador se llena con daño efectivo, cuchillazos y parry, y al llenarse ganas otra (máx. 10) | L / F | B / RB |
| **Fijar** (mantener): te quedas quieto para apuntar con calma | U / Shift izq. | Clic del stick der. |
| Medicamento anterior / siguiente | Rueda del mouse / Q / E | — / Y |
| Medicamento directo | 1 a 5 | — |
| Pausa | Esc | Start |

Los controles se definen en `Scripts/Gameplay/Player/PlayerInputReader.cs`.

---

## Arte del caballero

Está en `Assets/_Project/Art/Player` (esa carpeta no la borra "Regenerar proyecto base"):

| Archivo | Qué es |
|---|---|
| `Caballero_Cuerpo` | El cuerpo sin el brazo. Pivote entre los pies. |
| `Caballero_Cuerpo_Color` | Lo que cambia de color en el cuerpo: cresta, ojos, círculo del casco y articulaciones (en blanco). |
| `Caballero_Brazo` | El brazo con el cañón, horizontal hacia la derecha. Pivote en el hombro: gira para apuntar. |
| `Caballero_Brazo_Color` | Lo que cambia de color en el cañón (en blanco). |

Las piezas `_Color` van encima y `PlayerShooter` les pone el color del medicamento. La importación (Sprite, 850 píxeles por unidad y los pivotes) la deja lista `Editor/PlayerArt.cs` al crear el prefab; ahí están también la posición del hombro y el largo del cañón. Si reemplazas los PNG por otros con el mismo tamaño y pose, no hay que tocar nada; si cambias la pose, ajusta esos números.

## Enemigos: comportamiento y biología

Cada enemigo tiene un comportamiento (`behavior` en su EnemyData) basado en su microorganismo, y puede tener **debilidades propias** (`weaknesses`) que ganan sobre la tabla general. Los de suelo bajan de las plataformas si estás abajo y saltan para subir si estás arriba; si no te alcanzan, patrullan. Todos se mueven con inercia (aceleran y frenan), rebotan al caminar, se estiran al saltar, se aplastan al caer y los voladores se inclinan hacia donde van. En el Sandbox, la arena "Arena_Enemigos" trae una oleada por comportamiento para probarlos.

| Enemigo | Cómo pelea | Por qué (biología) | Debilidad |
|---|---|---|---|
| H. pylori | Se entierra (inmune), avanza escondido y sale debajo de ti | Se entierra en el moco del estómago para que el ácido no la mate | Antibiótico |
| Giardia | Salta y se te pega: te frena y te daña; un disparo la despega | Disco ventral con el que se pega al intestino | Antiparasitario **y** Antibiótico (metronidazol es las dos cosas) |
| Rotavirus | Rueda voladora: gira a tu alrededor, acelera el giro, se echa para atrás y se lanza como sierra; rebota en piso, paredes y escudo | "Rota" = rueda: así se ve al microscopio | Antiviral |
| Entamoeba | Seudópodo largo que te jala | Fagocitosis: engulle células | Antiparasitario |
| C. difficile | Herida se vuelve espora (todo rebota) y despierta | Sus esporas resisten antibióticos y calor | Solo Antibiótico |
| Oxiuro | Rápido, culebrea; al morir deja un huevo que eclosiona | Autoinfección con sus huevos | Antiparasitario |
| S. mutans | Lanza ácido en arco desde lejos | Ácido láctico que causa la caries | Antibiótico |
| Rinovirus | Vuelo errático, estornuda en abanico; se cura solo | No existe antiviral para el resfriado | Todo le hace poco (Antiviral parcial) |
| Polen | Lo lleva el aire; cerca revienta y te frena | Alérgeno: histamina | Antihistamínico |
| S. aureus | Al morir se separa en 2 cocos | Racimo de uvas (staphylé) | Antibiótico |
| B. pertussis | Se infla y tose 3 veces; cada tos empuja | Ataques de tos ferina | Antibiótico |
| Neumococo | Avanza a saltos y te cae encima | Diplococo que llega en gotitas | Antibiótico |
| Influenza | Se lanza en picada | Espículas con las que se clava | Antiviral (oseltamivir) |
| Tuberculosis | Lenta y dura; de lejos tose gotitas en arco; se encierra en granuloma | Pared cerosa; granulomas | Solo Antibiótico |
| Meningococo | Se agazapa y cruza a toda velocidad | Infección rapidísima | Antibiótico |
| Poliovirus | Sus disparos te paralizan (lento) | Ataca neuronas motoras | Antiviral |
| Herpes | Se vuelve latente (invisible), reaparece y dispara en ráfaga | Latencia en ganglios nerviosos | Antiviral (aciclovir) |
| Rabia (suelta) | Muerde 3 veces seguidas | Vuelve agresivo al infectado | **Anticonvulsivo** (sedar el cerebro); Antiviral parcial |

## Sonido (experimento)

Todos los sonidos viven en `Assets/_Project/Audio/Resources/SFX` y el juego los busca **por nombre de archivo**: para cambiar uno, reemplaza el archivo con el mismo nombre (`.ogg` de preferencia; los `.wav` van por Git LFS). No hay que tocar código ni regenerar. Los actuales son de prueba, sintetizados con `Tools/Audio/generar_sonidos.py` (ahí está explicado cómo hacer más). Los volúmenes por grupo están en `Data/Resources/CombatAudio`.

| Archivo | Cuándo suena |
|---|---|
| `Disparo_<Medicamento>` / `Impacto_<Medicamento>` | Disparar / pegarle a algo (también se pueden arrastrar al campo del MedicationData) |
| `Rebote_Ineficaz` | Cápsula ineficaz que rebota en gris |
| `Jugador_Salto`, `Jugador_SaltoDoble`, `Jugador_Herido`, `Jugador_Muere` | Jugador |
| `Escudo_Bloquea`, `Escudo_Parry`, `Escudo_Rompe` | Escudo |
| `Bomba_Lanza`, `Bomba_Explota`, `Bomba_Ganada` | Bombas (ganada = se llenó la barra peleando) |
| `<archivo del enemigo>_Disparo` / `_Muere` | Un enemigo en particular, p. ej. `Virus_Rinovirus_Muere` |
| `<tipo>_Disparo` / `_Muere` | Todo un tipo si el enemigo no tiene el suyo: `Virus_`, `Bacteria_`, `Parasito_`, `Bloqueo_`, `Jefe_`... |
| `Enemigo_Disparo` / `Enemigo_Muere` | Lo que no tenga ninguno de los dos anteriores |
| `Neurona_Hiperexcitada_Muere` | Calmar una neurona roja (vuelve a ser sana) |
| `Acido_Burbujeo` (bucle), `Acido_Quema` | Charco de ácido: burbujea cerca, chisporrotea al quemarte |
| `Lombriz_Retumbo`, `Lombriz_Sale`, `Lombriz_Entra`, `Lombriz_Ruge` | Aviso bajo tierra, salir, meterse, quedar expuesta / huir |
| `Alveolo_Infla`, `Alveolo_Desinfla`, `Alveolo_Rebote` | Alvéolos de los pulmones (inhala, exhala, trampolín) |
| `Colmena_Enjambre` | La colmena suelta su enjambre (su disparo es `Jefe_Colmena_Disparo` o `Jefe_Disparo`) |
| `Rayo_Aviso`, `Rayo_Cae` | Tormenta: el segundo de aviso y el rayo |
| `Neurona_Carga`, `Neurona_Descarga`, `Neurona_Golpe`, `Neurona_Infecta` | Neuronas: carga y descarga de la roja, golpe e infección de la sana |
| `Campo_Neuroprotector` | Bomba de anticonvulsivo |
| `Rabia_Chillido` | La cadena de rabia da la vuelta |
| `Ambiente_<Sistema>` (bucle) | Fondo de cada sistema según el nombre de la escena (`Ambiente_Digestivo`, `_Respiratorio`, `_Nervioso`). `Ambiente_<Escena>` (p. ej. `Ambiente_Nervioso_Hipocampo`) le gana para un nivel en particular |

Los sonidos que pasan fuera de la pantalla se oyen más bajo y, si están muy lejos, no suenan. Disparos e impactos: cortos (menos de 0.2 s) y en mono.

## Música (ejemplos)

Cada sistema tiene un género electrónico que suena a lo que pasa en ese órgano, con versión de jefe más rápida y pesada:

| Sistema | Género | BPM | Archivos |
|---|---|---|---|
| Digestivo | Acid house (bajo "acid" burbujeante = jugos gástricos) | 128 / 134 | `Musica_Digestivo`, `Musica_Digestivo_Jefe` |
| Respiratorio | Trance (pads que suben y bajan = respirar) | 138 / 145 | `Musica_Respiratorio`, `Musica_Respiratorio_Jefe` |
| Nervioso | Neurofunk (drum & bass eléctrico = neuronas) | 174 / 176 | `Musica_Nervioso`, `Musica_Nervioso_Jefe` |

Van en `Assets/_Project/Audio/Resources/Music` y el juego las elige por el nombre de la escena: `Musica_<Escena>` (p. ej. `Musica_Nervioso_Hipocampo`) le gana a `Musica_<Sistema>`. Cuando empieza un jefe cambia con fundido a `..._Jefe` y al terminar regresa. Al morir no se reinicia. El volumen está en `Data/Resources/CombatAudio` (Music Volume).

Las actuales son de ejemplo, hechas con `Tools/Audio/generar_musica.py` (secuenciador en Python). Para poner la de Víctor: guárdala en `.ogg` con el mismo nombre; que sea un loop que empate al final y que no tenga demasiados graves, para que se oigan los efectos.

## Estructura de carpetas

```
Assets/
├── _Project/                 ← TODO lo del juego va aquí
│   ├── Art/                  Sprites (Placeholders se generan solos; reemplázalos con tu arte)
│   ├── Audio/
│   ├── Data/                 ScriptableObjects: balance y contenido, editables sin código
│   │   ├── Combat/           EffectivenessTable (tabla medicamento × amenaza)
│   │   ├── Enemies/          H. pylori, Giardia, Rotavirus, Entamoeba, C. difficile, Oxiuro, S. mutans, Jefe_Lombriz
│   │   ├── Medications/      Antibiótico, Antiviral, ..., Antiparasitario, Mucolítico
│   │   ├── Player/           PlayerConfig (velocidades, salto, daño, escudo...)
│   │   ├── Resources/        GameCatalog (lista de sistemas del mapa)
│   │   └── Systems/          Un folder por sistema con su SystemData y sus LevelData
│   ├── Prefabs/              Player, Enemies, Bosses, Projectiles, Hazards, Level
│   ├── Scenes/               MainMenu, BodyMap, Levels/<Sistema>/..., Sandbox
│   ├── Scripts/
│   │   ├── Core/             Datos, efectividad, daño, progreso y guardado (sin dependencias)
│   │   ├── Gameplay/         Jugador, enemigos, arenas, peligros, cámara, nivel
│   │   ├── UI/               HUD, pausa, menú principal, mapa del cuerpo
│   │   └── Editor/           Herramienta "Crear proyecto base"
│   └── Tests/EditMode/       Pruebas automáticas (Window → General → Test Runner)
├── _Prototype/               El prototipo entregado para AU (solo referencia; se puede borrar)
├── Settings/                 URP y configuración de render
└── TextMesh Pro/
```

Cada carpeta de `Scripts` es un *assembly* (`.asmdef`): compila por separado y no puede depender de las de "arriba". La dirección es Core ← Gameplay ← UI ← Editor.

## Cómo funciona (vista lateral)

- La **posición** de un personaje son sus pies. El hijo `Visual` se voltea al mirar a la izquierda.
- El jugador y los enemigos que caminan usan `Rigidbody2D` + `BoxCollider2D` + `CharacterMotor2D` (gravedad propia, salto de altura fija, detección de suelo). Los voladores (virus) y el jefe se mueven por script.
- Capas de física: **Ground** (piso y plataformas), **Player**, **Enemy**, **PlayerProjectile**. Los personajes solo chocan con el piso: entre ellos se atraviesan, como en Metal Slug (`PhysicsLayers.cs`).
- Las **plataformas** son de un solo sentido (`PlatformEffector2D`): se atraviesan saltando desde abajo.
- Caer a un hueco cuesta vida y te regresa al último suelo seguro. Los enemigos que caen se eliminan.
- El combate **no** usa colisiones de física: disparos, cuchillo y bombas buscan `Hurtbox` (rectángulos) con matemática simple (`HitMath`). Por eso agacharse esquiva disparos altos y salta por encima de la embestida de la lombriz.
- Apuntado: libre con mouse o stick derecho (`AimMath.Toward`) o en 8 direcciones con teclas (`AimMath.Resolve`). Probado en `Tests/EditMode/Combat2DTests.cs`.

## Cómo agregar contenido

| Quiero... | Pasos |
|---|---|
| **Cambiar balance** (daño, velocidad, vida) | Edita el asset en `Data/` (funciona incluso en Play Mode) |
| **Un enemigo nuevo** | Crea un `EnemyData` (clic derecho → Create → Healing Knight → Enemigo), duplica un prefab de `Prefabs/Enemies`, asígnale el dato y cambia el arte de `Visual` |
| **Un nivel nuevo** | Duplica una escena de `Scenes/Levels`, crea un `LevelData` con el nombre exacto de la escena, agrégalo a la lista del `SystemData` y a Build Settings |
| **Una arena** | GameObject con `ArenaZone`: `triggerX`, límites, `floorY` y oleadas (prefab + dato + cantidad). La pantalla se cierra hasta acabar las oleadas |
| **Plataformas y huecos** | Plataforma: objeto en capa **Ground** con `BoxCollider2D` (*Used By Effector*) + `PlatformEffector2D`. Hueco: deja un espacio entre dos tramos de piso |
| **Un nivel secreto** | En su `LevelData` marca `hidden` y escribe un `secretId`; coloca un prefab `SecretPassage` con ese mismo id en algún nivel |
| **Un sistema nuevo** | Crea `SystemData` y sus `LevelData`, y agrégalo al `GameCatalog` |
| **Tu arte del personaje** | Importa el PSD, arma las piezas dentro de `Visual` del prefab `HealingKnight` y borra las figuras de prueba. Los scripts de la raíz no cambian |

## Reglas del repo

- `main` = estable. Trabaja en ramas `feature/nombre` y únelas con Pull Request.
- Una persona por escena a la vez.
- Mueve o renombra archivos **desde Unity** (para que su `.meta` viaje con ellos).
- Si usas archivos grandes (PSD, WAV), instala Git LFS una vez: `git lfs install`.
