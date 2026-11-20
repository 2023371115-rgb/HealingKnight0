# Healing Knight

*Run and gun* 2D de un jugador (estilo **Metal Slug**) hecho en Unity 6. Un nanobot médico recorre los sistemas del cuerpo del Paciente 001 y derrota enfermedades usando el medicamento correcto.

**Entrega de diciembre:** Sistema Digestivo: Estómago → Intestino delgado (la lombriz aparece y huye) → Intestino grueso (pelea final con la lombriz). La **Boca** es un nivel secreto: se desbloquea al tocar un pasaje escondido en el Estómago.

**Siguiente (prototipo listo):** Sistema Respiratorio: Nariz (rinovirus, polen, S. aureus) → Tráquea (tos ferina) → Pulmones (neumonía, influenza y el jefe volador: la **nave de tuberculosis**; da la fórmula 2 y cierra con el aviso de que la infección va al cerebro). Mecánica del GDD: el paso por los bronquiolos está bloqueado por **mucosidad** (Mucolítico; te frena si la tocas) e **inflamación** (Antiinflamatorio; se contrae al dañarla). El **polen** es un alérgeno: Antihistamínico.

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

Son los de **Cuphead**. Se cambian en **Controles** (menú principal o pausa): ahí eliges el esquema de teclado
(Cuphead o Clásico), puedes poner la tecla o el botón que quieras en cada acción (si ya lo usaba otra, se
intercambian) y activar **Apuntar con mouse**. Se guarda solo y se aplica al momento.

Apuntas en 8 direcciones como en Cuphead: arriba, diagonal arriba mientras corres, abajo y diagonal abajo en el aire.
Mantén **Fijar** para quedarte quieto y apuntar a cualquiera de las 8 (también diagonal abajo en el suelo).
El stick derecho apunta libre; el mouse, solo si lo activas en Controles.

| Acción | Cuphead (teclado) | Clásico (teclado) | Control (Xbox) |
|---|---|---|---|
| Moverse · apuntar | Flechas | W A S D | Stick izq. / cruceta |
| Encogerse (se hace pequeño y esquiva disparos altos) | Flecha abajo | S | Abajo |
| Saltar (altura fija) · **doble salto** · mantener al caer = **caída lenta** (1.5 s) | Z | Espacio | A |
| Disparar (mantener = ráfaga). Si un enemigo está pegado, **cuchillazo** automático. Hacia abajo en el aire frena la caída | X | J | X |
| **Fijar** (mantener): quieto y apuntando en 8 direcciones | C | U | RB |
| **Dash**: rápido y recto, sin caer; en el aire uno por salto | Shift izq. | Shift izq. | Y |
| **Bomba de medicamento** (el EX). Empiezas con 5; se recargan con daño efectivo, cuchillazos y parry (máx. 10) | V | L | B |
| Escudo burbuja (mantener) — levántalo justo antes del golpe para hacer **parry** | A | K | LT |
| Cambiar medicamento | Tab | E | LB |
| Medicamento anterior | Q | Q | Select |
| **Herramienta** (hilo dental en la Boca): engancharte / soltarte | S | R | RT |
| Medicamento directo | 1 a 5 | 1 a 5 | — |
| Pausa | Esc | Esc | Start |

El mouse siempre sirve para clic izq. disparar, clic der. escudo, clic central herramienta y la rueda para cambiar de
medicamento; para **apuntar** con él activa **Apuntar con mouse** en Controles.

**Medicamentos de cada nivel:** solo traes los que **sirven** ahí (los efectivos contra algún enemigo del nivel; mínimo
2). Los calcula `SceneSetup.AssignLevelLoadout` al regenerar, revisando los enemigos sueltos, las arenas, las oleadas de
vuelo, los jefes y lo que ellos sueltan, y los guarda en `LevelManager.levelLoadout` (en la consola sale la lista de cada
nivel). Pueden salir los genéricos, el regional del sistema y el antiparasitario. El HUD muestra todos en una fila (el
actual, más grande) con las teclas para ir hacia adelante y hacia atrás.

Los controles se definen en `Scripts/Gameplay/Player/PlayerControls.cs` (el menú está en `UI/ControlsScreen.cs`).
En los mensajes de NODO escribe `{saltar}`, `{disparar}`, `{fijar}`, `{dash}`, `{bomba}`, `{escudo}`, `{medicamento}`,
`{herramienta}`, `{mover}`, `{arriba}`, `{abajo}` o `{pausa}` y el juego pone la tecla o el botón que tenga el jugador.

---

## Arte del caballero

Está en `Assets/_Project/Art/Player` (esa carpeta no la borra "Regenerar proyecto base"):

| Archivo | Qué es |
|---|---|
El caballero está cortado en piezas, como un muñeco articulado, y se anima por código (`PlayerRigAnimator`): camina balanceando las piernas y doblando las rodillas, recoge las piernas al saltar, las abre al caer y respira quieto. Cada pieza tiene su PNG y su `_Color` (lo que pinta el medicamento, en blanco):

| Pieza | Qué es | Gira en |
|---|---|---|
| `Caballero_Torso` | Cabeza, torso y cadera | (fija; sube y baja al caminar) |
| `Caballero_PiernaFrente_Muslo` | Muslo (se usa para las dos piernas) | La cadera |
| `Caballero_PiernaFrente_Pie` | Rodilla, espinilla y pie (las dos piernas) | La rodilla |
| `Caballero_Brazo` | Brazo con el cañón | El hombro (apunta) |

La pierna de atrás usa el mismo dibujo que la de enfrente, un poco inclinada hacia atrás, para que las dos sean simétricas y completas. La importación (Sprite, 850 píxeles por unidad y pivotes) y la posición de cada pieza están en `Editor/PlayerArt.cs`. Si redibujas una pieza con el mismo tamaño y pose, no hay que tocar nada. Los ángulos del paso, salto y caída se ajustan en el componente `PlayerRigAnimator` del prefab.

**Encogerse:** abajo ya no aplasta al caballero; lo hace más pequeño de forma pareja (brazo, escudo y disparo incluidos) y baja su colisionador. El tamaño y la velocidad del cambio están en el PlayerConfig (`crouchHeightFactor`, `shrinkSpeed`).

## Comida flotante (Estómago)

En los lagos y en la marea hay comida a medio digerir (dona, manzana, salchicha, hamburguesa) que sirve de
plataforma: en los lagos son islas para cruzar sin pisar el ácido, y en la marea **suben con el ácido** y te llevan
encima. Los PNG están en `Assets/_Project/Art/Comida/` (`Comida_<Nombre>.png`, fondo transparente, estilo caricatura a
256 píxeles por unidad); los dibuja `Tools/Art/generar_comida.py`. En `SceneSetup`: `Food("Dona", x, alturaDelPiso)` donde la altura es la fracción del dibujo
(0 = abajo, 1 = arriba) en la que se pisa. Si la x cae dentro de un lago o una marea, flota en él.

Para agregar otra comida: dibuja o genera el PNG con fondo transparente (que no sea el cuadriculado pintado),
guárdalo en esa carpeta y agrega una línea `Food(...)`.

## Dibujos del Estómago (enemigos, jefe y objetos)

Los dibujos hechos con IA (fondo verde quitado) ya están en el juego:
- `Art/Enemigos/Estomago/<Enemigo>_<Pose>.png`: `HPylori`, `HPyloriMadre`, `HPyloriUreasa` y `Anisakis`.
  Poses: `Normal` (quieto), `Mueve`, `Ataca`, `Dano` (le pegan) y `Muere`; el anisakis usa `Dormido` (hasta que te ve)
  y `Arrastra`, y la madre usa `Suelta` cuando suelta crías. Si falta una pose se usa la más parecida.
  El cambio de pose lo hace `Enemies/EnemyPoseArt.cs`; el tamaño de cada enemigo está en `ApplyEnemyArt` (`PrefabSetup`).
- `Art/Jefes/Bezoar/`: `Bezoar_Normal`, `Bezoar_Enojado`, `Bezoar_Derrotado` y los pedazos que avienta (`Pedazo_*`).
- `Art/Estomago/`: `Omeprazol`, `Contraccion` (pivote en la cara, al frente) y `Piloro_Cerrado` / `Piloro_Abierto`.
- `Art/Comida/`: dona, hamburguesa, manzana, salchicha y sushi dibujados con IA. **Ojo:** `generar_comida.py` los
  reemplaza por los dibujados por código si lo vuelves a correr.

Para cambiar un dibujo: guarda el PNG nuevo con el mismo nombre (fondo transparente, mirando a la derecha) y regenera.

## Letreros e indicaciones

Cada nivel tiene **letreros** con el estilo de su sistema y NODO habla en un **globo** con su cara.
- **Automáticos:** Bienvenida con el nombre del órgano al empezar, Salida al final, "¡JEFE!" antes de cada jefe y un
  aviso de Peligro antes del primer hueco o lago y de los grandes ("¡ÁCIDO!", "¡BILIS!", "¡SALIVA!", "¡HUECO!").
- **De cada nivel:** un letrero de Info donde se enseña algo nuevo (DUODENO, CILIOS, RETINA...). Se ve desde lejos y,
  al llegar, NODO lo explica. Flechas donde el camino sube o baja (colon ascendente y descendente, corrientes de aire),
  avisos de zonas (¡CORRE!, ¡TOS!, ¡ESTORNUDO!, ¡TORMENTA!) y un "?" chiquito debajo de los pasajes secretos.
- **Globos de botón:** cerca de un gancho de hilo dental sale "[tecla] Hilo dental" y junto a un escondite del Ojo
  "[tecla] Esconderse", siempre con la tecla o el botón que tengas puesto.
- **Globo de NODO:** baja desde arriba, escribe letra por letra y dura según lo largo del texto; si llegan varios,
  hace fila. Su color va con el sistema y su cara cambia: normal, alerta (rojo, si el mensaje empieza con "¡" o
  habla de peligro) y feliz (verde, premios y zona limpia).
- **Dibujos (ya están los de los tres sistemas y las caras de NODO; si borras uno, se arma con figuras):**
  - Letreros: `Art/Letreros/{Digestivo|Respiratorio|Nervioso}/Letrero_Bienvenida, _Info, _Peligro, _Salida,
    _FlechaDerecha, _FlechaArriba, _FlechaAbajo, _Secreto.png`. Tablero en blanco: la palabra la escribe el juego
    dentro de la zona medida de cada dibujo (`SignTextAreas` en `SceneSetup`); si cambias un dibujo, ajusta ahí su zona.
  - NODO: `Art/UI/Resources/Nodo/Nodo_Normal.png`, `Nodo_Alerta.png` y `Nodo_Feliz.png`.
- **Dónde está:** `Gameplay/Level/Signpost.cs` (letrero), `ButtonPrompt.cs` (globo de botón), `UI/NodoBubble.cs`
  (globo de NODO). Se colocan en `SceneSetup`: `Tip(x, "PALABRA", "lo que dice NODO")` y `Sign(tipo, x, y, "PALABRA")`.

## Estilo de arte: caricatura (tipo Hora de Aventura)

El juego ya no usa pixel art: colores planos, contorno grueso oscuro (el mismo tono en todo), formas redondas,
brillos ovalados y manchitas. Lo que se dibuje nuevo (enemigos, objetos) debe seguir esas reglas para que combine.

## Piso y plataformas de tejido

En el Estómago el piso y las plataformas ya no son bloques de color: son tejido estilo caricatura con la
misma paleta del fondo (contorno grueso, labio claro, manchas, brillos y gotas de moco) y sombras de contacto (el fondo se oscurece
arriba del piso y debajo de cada plataforma; las paredes de huecos y lagos llevan sombra).

- Las imágenes están en `Assets/_Project/Art/Tejidos/` y las genera `Tools/Art/generar_tejido.py`
  (`python generar_tejido.py`). Se repiten sin cortes: Unity las usa con *Draw Mode = Tiled*.
- La plataforma tiene puntas fijas (96 px a cada lado) y el centro se repite: sirve para cualquier ancho.
- Para otro órgano: agrega una paleta en `PALETAS` del script, genera, y en el `LevelStyle` de ese nivel pon
  `tissue = "Nombre"`. Si dibujas tú los PNG, respeta los nombres y tamaños (256 píxeles = 1 unidad: piso 512×256,
  relleno 256×256, plataforma 448×176 con puntas de 96 px) y que se repitan sin cortes a los lados.

## Ácido del Estómago (lago y marea)

El Estómago (rediseño, 200 de largo) tiene seis partes que suben de dificultad:
1. **Inicio seco** (tutorial, Arena 1).
2. **Lagos de ácido con géiseres:** dos lagos hondos (`basins`) que se cruzan sobre comida. Hay chorros de ácido
   (`AcidGeyser`) que burbujean 1 s de aviso y luego saltan: hay que medir el brinco. Primera cápsula de omeprazol.
3. **La colonia (Arena 2):** H. pylori madre (grande, cada 5 s suelta bacterias hijas; mátala primero) y H. pylori con
   **ureasa** (burbuja turquesa: el ácido no quema dentro de ella, biología real: la ureasa hace amoníaco que neutraliza el ácido).
4. **Gas y marea (Arena 3):** burbujas de gas (`GasBubbleVent`) que suben como elevadores y revientan arriba, y la
   alberca de marea (`tidePool`) con comida que sube con el ácido. Un sushi tiene **anisakis** escondido.
5. **Pliegues y lago de hamburguesas:** subes por los **pliegues** del estómago (rugas, `SolidBlock`) entre chorros de
   ácido que salen de las glándulas, y bajas a un lago donde casi todo es hamburguesa que se hunde (no te detengas);
   la dona te sube a un omeprazol. En la orilla salen H. pylori escondidas.
6. **Contracción y jefe:** una pared de músculo (`PeristalsisWave`) te persigue y te empuja al píloro, que está tapado por
   el **Bezoar** (bola de comida pegada, Arena 4). Al vencerlo se abre el píloro (`PyloricDoor`) y sigue el intestino.

Comida con comportamiento (`FloatingFood.FoodKind`):
- **Normal:** flota y sube con la marea.
- **Bouncy (dona):** trampolín, te lanza alto.
- **Rolling (salchicha):** rueda hacia donde caminas, como tronco.
- **Sinking (hamburguesa):** se hunde despacio mientras estás encima; no te quedes.
- `Food(nombre, x, altura, kind, anisakis: true)` esconde un anisakis que sale al pisarla.

**Omeprazol** (`AntacidPickup`): cápsula que baja todo el ácido 8 s (no quema y los géiseres se apagan); reaparece a los 14 s.
Es un medicamento real contra la acidez y parte de la triple terapia contra H. pylori.

**Bezoar** (`Enemies/BezoarBoss.cs`, 320 de vida): rueda de pared a pared (sáltalo), avienta pedazos de comida en arco y,
desde la mitad de vida, brinca y aplasta soltando H. pylori. Rueda de verdad (el dibujo gira) y se endereza al detenerse.

En `SceneSetup`:
- `Geyser(x, pisoY, altura, intervalo, desfase)`, `GasVent(x, altura, intervalo)`, `Omeprazol(x, y)`,
  `Peristalsis(desdeX, hastaX, velocidad)` y `Pylorus(x, arenaDelJefe)`.
- `basins = new[] { new Vector2(inicio, ancho), ... }` en el `LevelStyle`: lagos hundidos en el piso.
- `Lake(desdeX, hastaX)` pone un lago sobre el piso (sin hundir).
- `tidePool = new Vector4(inicio, ancho, hondo, alturaMaxima)` en el `LevelStyle`: alberca de marea que nunca se seca.
- `Tide(desdeX, hastaX, altura)` pone una marea sobre el piso que sí se seca.
- `MucusLayer(desdeX, hastaX)` dibuja la capa de moco.

En la escena puedes ajustar a mano cada lago: colores, ancho de columna de la ola (más grande = más pixel art),
daño y los tiempos de la marea (seco, aviso, subida, espera, bajada). Sirve igual para la bilis del intestino
cambiando los colores.

## Intestino delgado (rediseño)

247 de largo, en las tres partes reales del intestino delgado:
1. **Duodeno:** arena de Giardia y un lago de bilis con **gotas de grasa** (`Hazards/FatDroplet.cs`) que sirven de plataforma.
   Los **chorros de bilis** (el mismo `AcidGeyser` del estómago con `SetColors`) las rompen en gotitas, como la bilis de verdad
   (emulsiona la grasa); a los 2.8 s se vuelven a juntar.
2. **Yeyuno:** **vellosidades** que se mecen (`Hazards/SwayingVillus.cs`; te paras en la punta y te lleva) sobre la bilis,
   con **Giardias escondidas** que se te avientan (`Level/AmbushSpawner.cs`). En la arena del rotavirus, el **rotavirus
   aplana las vellosidades** que tiene cerca (se recuperan despacio cuando ya no hay). Luego una **placa de Peyer**
   (`Level/PeyerPatch.cs`: punto de guardado y cura un poco) y **suero oral** (`Level/HealPickup.cs`: cura 35).
3. **Tenia (minijefe, Arena 3):** `Enemies/TeniaBoss.cs`. Está **anclada al piso** en un pliegue de la pared (la tenia
   real se agarra con su cabeza; aquí la base es su ancla y la cabeza se asoma arriba) y su cabeza **te mira a ti**.
   - **Discos:** varios pedazos de su cuerpo brillan y se sueltan como DISCOS que vuelan rectos hacia ti, cada uno a su
     altura: los de abajo se saltan, los del medio se esquivan agachado y a veces hay un hueco para meterse. Tus balas
     los rompen.
   - **Mordida:** se inclina hacia ti y muerde. **Azote:** se echa para atrás y se deja caer encima (tiembla antes);
     después queda tirada un momento: es cuando más daño le haces.
   - Cada 11 s suelta un pedazo de la base que camina (**proglótido**) y el cuello le vuelve a crecer (máximo 9).
     Abajo de la mitad de vida se enoja: telegrafía menos y lanza más rápido.
4. **Íleon, río de quimo (más largo):** te subes a la salchicha (`FloatingFood`, tipo `Raft`) y avanza sola río abajo,
   aunque brinques encima; si te caes, se detiene. Son **dos balsas**: la primera te deja en una isla con placa de Peyer y
   la segunda (la hamburguesa) sigue el río. Hay chorros de bilis que brincar y rotavirus que llegan volando (emboscadas).
5. **Persecución de la lombriz:** `Enemies/WormChase.cs`. La pantalla se cierra alrededor del punto donde está la lombriz:
   sale del piso una y otra vez (antes tiembla el montículo) y solo le puedes pegar cuando está afuera. Cuando le bajas
   un pedazo de vida, huye al siguiente punto y la pantalla avanza. En la **válvula ileocecal** se escapa al intestino
   grueso (no muere aquí). Si mueres, lo que le bajaste se queda.
6. **Tenia:** el cuerpo vuelve a crecer desde el cuello cada 4.5 s (máximo 9 pedazos) y solo suelta pedazos con huevos
   si tiene más de 5.

**Enemigos de suelo:** ahora atacan hasta 3 a la vez; los demás amagan acercándose y alejándose en vez de quedarse quietos.
Se meten a los lagos (el ácido y la bilis no les hacen nada) y bajan de plataformas bajas para seguirte. Si uno con turno
de ataque se queda lejos o atorado, suelta el turno para que otro ataque.

En `SceneSetup`: `BileGeyser`, `FatDrop`, `Villus`, `Ambush`, `OralSerum`, `Peyer`, `WormChaseAt` e `IleocecalValve`.
Dibujos (fondo verde ya quitado):
- `Art/Enemigos/Delgado/`: `Giardia_*`, `Rotavirus_*` (Normal, Ataca, Dano, Muere) y `Proglotido_Normal` / `_Ataca`.
- `Art/Jefes/Tenia/`: `Cabeza_Normal`, `Cabeza_Muerde`, `Cabeza_Derrotada` (el cuello abajo es donde nace el cuerpo) y
  `Segmento`, `Segmento_Cuello`, `Segmento_Huevos` (los 2 primeros pedazos son de cuello y los 3 últimos llevan huevos).
- `Art/Jefes/Lombriz/`: `Cabeza_Sale`, `Cabeza_Ruge`, `Cabeza_Dano`, `Anillo`, `Anillo_Chico`, `Cola` y `Monticulo`.
- `Art/Delgado/`: `GotaGrasa` (+ `_Rota`), `Vellosidad` (+ `_Danada`, se mezclan al aplanarse), `SueroOral`,
  `Peyer_Apagada` / `_Encendida` y `Valvula_Abierta` / `_Cerrada`.
Si borras un PNG, ese objeto vuelve a dibujarse con figuras.

## Intestino grueso (rediseño)

215 de largo, siguiendo el colon de verdad: sube, cruza por arriba y baja.
1. **Ciego y apéndice:** arena de amebas y **haustras** (`Level/HaustraPouch.cs`): bolsas del piso que tiemblan y te
   lanzan hacia arriba. Con la de en medio y doble salto llegas al **apéndice secreto** (+vida y +3 bombas).
2. **Colon ascendente:** subida por plataformas en zigzag con el **líquido que sube** detrás (`Hazards/RisingFlood.cs`:
   empieza cuando trepas; si sales de la zona o mueres, baja). Oxiuros emboscados y una burbuja de gas.
3. **Colon transverso (arriba, altura 16):** un bloque de tejido sólido (`SolidBlock`). Aquí vive la **flora buena**
   (`Enemies/FriendlyBacteria.cs`): no ataca; si la matas con **antibiótico** sale **C. difficile** en su lugar. El
   **probiótico** (`Level/ProbioticPickup.cs`) es la botella del yogur hundida en un charco en el piso, con sus
   **lactobacilos** nadando alrededor (nada flota). Al tocarla los lactobacilos se van a reforzar la flora y la ponen a
   atacar a los enemigos 10 s; luego se reproducen uno por uno y la colonia se vuelve a llenar (20 s). Arena de C. difficile con flora adentro.
4. **Colon descendente:** bajada con **bolos** que suben y bajan (`Level/MovingPlatform.cs`), **lodo** que te frena y más
   si te quedas parado (`Hazards/MudZone.cs`) y **úlceras** de las que salen amebas (`Enemies/EnemyNest.cs`).
5. **Sigmoide y recto:** huevos de oxiuro sueltos, arena de nidos y la **pelea final con la lombriz**: abajo del 35 % de
   vida llegan oleadas de **movimiento de masa** (`Hazards/MassWave.cs`) que barren el piso: súbete a las plataformas.

Al reaparecer, el juego busca en esa X el piso sólido (con espacio encima) **más parecido a la altura donde estabas parado
por última vez**: así funciona arriba, en el transverso, y en la Boca no apareces encima de la encía del techo. Si ahí hay
un hueco, busca un poco más atrás.
Dibujos opcionales (si no están, se dibuja con figuras): `Art/Grueso/` → `Haustra`, `Bolo`, `Ulcera`, `Probiotico`,
`Apendice`; `Art/Enemigos/Grueso/` → `Flora_Normal`, `Entamoeba_*`, `CDifficile_*` (+ `CDifficile_Espora`),
`Oxiuro_*` y `OxiuroHuevo_Normal` / `_Abre`.

## Respiratorio y Nervioso (rediseño)

Los seis niveles son más largos (190–235 u) y cada uno tiene mecánicas propias. Todo se dibuja por código;
si pones un PNG con el nombre indicado en `Art/Respiratorio/` o `Art/Nervioso/`, se usa ese dibujo.

**Nariz (230):**
- **Glándulas de moco** (`MucusCannon`): lanzan bolas en arco. Si te pegan te frenan. Las que caen se pegan un rato y sirven de plataforma. El Mucolítico (o el Antihistamínico) las disuelve.
  - Arte: `GlandulaMoco.png`, `BolaMoco.png`.
- **Estornudo** (`WindBlast`): una barra de comezón arriba de la pantalla se llena sola, y más rápido con cada polen vivo. Al llenarse avisa "¡A... a...!" y luego ¡ACHÚ!, y el aire te avienta hacia atrás.
  - Para que no te avienten, ponte entre los **pelos nasales** (`NasalHair`, arte `PeloNasal.png`) o detrás de un cornete.
- Hay cuatro arenas; la segunda está dentro del estornudo y lleva mucho polen.
- **Seno maxilar (sinusitis):** al final, una subida en zigzag con el **moco que sube** detrás (`RisingFlood` con colores de
  moco). Arriba, la orilla del seno y una arena con lo que lo tapaba.

**Tráquea (215):**
- **Epiglotis** (`TimedGate`, arte `Epiglotis.png`): una compuerta que se abre y se cierra.
- **Escalera de cilios** (`CiliaConveyor`): el piso te empuja hacia atrás y te rueda bolas de moco.
- **Tos** (`WindBlast` en modo tos): cada 6 s te avienta. Ponte bajo los **anillos de cartílago** (arte `AnilloCartilago.png`).
- **Corrientes de aire** (`AirVent`): te lanzan a una ruta alta.
- **La carina (dos rutas):** la tráquea se divide en los dos bronquios. Arriba, una corriente te sube a repisas con
  influenza volando y O₂; abajo, cilios que te empujan y tos ferina bajo anillos de cartílago. Se juntan antes del jefe.
- **Jefe: Reina de la tos ferina** (`PertussisQueenBoss`, arte opcional `Art/Jefes/TosFerina/TosFerina_Normal.png`):
  - Se infla antes de toser y tose 3 veces. Cada tos manda ondas por el piso que hay que saltar.
  - Al 60 % de vida llama a bacterias de tos ferina.
  - Al 30 % también escupe moco.

**Pulmones (202):**
- **Todo el pulmón respira** (`BreathingCycle`, sincronizado con los alvéolos):
  - Al inhalar te jala hacia adelante.
  - Al exhalar te empuja hacia atrás.
- **O₂** (`OxygenBubble`, arte `O2.png`): cura.
- **CO₂** (`CO2Cloud`, arte `CO2.png`): asfixia y flota con el aire.
- **Bronquiolos con dos rutas:**
  - La de arriba es difícil y tiene oxígeno y una bomba.
  - La de abajo es fácil, pero está llena de CO₂.
- Al final hay un **viaje en glóbulo rojo** por un capilar (arte `GloboRojo.png`). El glóbulo espera a que te subas.

**Médula (235):**
- Tormentas.
- **Arco reflejo** (`ReflexPad`): al pisarlo te lanza sobre una pared.
- **Surfear el impulso nervioso** por un axón (arte `Impulso.png`).
- **Conducción saltatoria** (`SaltatoryNodes`): los nodos de Ranvier se encienden en orden y solo el encendido es sólido.
- **Ganglio de la raíz dorsal:** arena de herpes zóster (el virus de la varicela se esconde ahí por años).
- **Saltatoria rápida:** fibras más gruesas = nodos que se encienden más seguido, sobre un hueco largo; un último arco
  reflejo te lanza hacia la salida.

**Cerebro (190):** cada lóbulo tiene su regla.
- **Occipital** (`DarknessZone`): todo está oscuro y solo destellos de neuronas muestran el camino.
- **Temporal** (`BeatPlatforms`): las plataformas aparecen al ritmo.
- **Cerebelo** (`TiltPlatform`): las plataformas son subibajas y te resbalas si te vas a la orilla.
- **Frontal** (`SequencePuzzle`): las neuronas se encienden en un orden y hay que tocarlas en ese mismo orden para abrir la compuerta.

**Hipocampo (195):**
- **Burbujas de memoria** (`FadingPlatform`): se revientan si te quedas quieto encima.
- **Río de recuerdos:** burbujas en escalera sobre un hueco largo... bajo una tormenta (no te detengas, pero mira dónde cae el rayo).
- **Recordar el camino** (`SequencePuzzle` con su propio mensaje): los recuerdos se encienden en un orden y hay que
  tocarlos igual, pero cada uno flota sobre una burbuja de memoria que se olvida si te detienes.
- **Fragmentos de memoria** (`MemoryFragment`, arte `FragmentoMemoria.png`): se apagan si tardas. Si salvas los 6, ganas una bomba.
- **Jefe rabia con hidrofobia:** reventar una **burbuja de agua** (`WaterBubble`, arte `BurbujaAgua.png`) cerca de sus cadenas las paraliza 3 s.

Los niveles largos tienen **puntos de regreso** invisibles (`CheckpointTrigger`).


**Dibujos de enemigos del Respiratorio, Nervioso y Boca:**
- Ponlos en `Art/Enemigos/Respiratorio/`, `Art/Enemigos/Nervioso/` o `Art/Enemigos/Boca/`.
- El nombre de cada archivo es `<Prefijo>_<Pose>.png`.
- Poses: Normal, Mueve, Ataca, Dano, Muere. Si falta alguna, se usa la más parecida.
- Prefijos:
  - Boca: SMutans.
  - Respiratorio: Rinovirus, Polen, SAureus, SAureusCoco, Pertussis, Neumococo, Influenza, Tuberculosis.
  - Nervioso: Poliovirus, Meningococo, Herpes, Rabia.

- Jefes y bloqueos con dibujo:
  - `Art/Jefes/TosFerina/` (Normal, Infla, Tose, Dano, Derrotada).
  - `Art/Jefes/Rabia/` (Cabeza_Normal/Ataca/Miedo/Derrotada, Segmento, Segmento_Dano, Cola). Al asustarse con el agua, la cabeza pone cara de pánico.
  - `Art/Jefes/Colmena/Nave_*` (Normal, Ataca, Dano, Derrotada): la nave de tuberculosis, jefe de los Pulmones.
  - `Art/Nervioso/Neurona_*` (Sana, Danada, Roja, Calmada): al calmar una roja aparece dormida un momento.
  - `Art/Respiratorio/Mucosidad_*` e `Inflamacion_*` (Normal, Dano, Disuelta/Desinflada): al tratarlos dejan su charco en el piso.

## Boca y niveles secretos (Amígdalas, Ojo)

**Boca (215, secreto del Estómago: el diente):** su mecánica propia es el **hilo dental** (`FlossGrapple` + `FlossHook`).
- **Nada flota:** los dientes de arriba y las muelas salen de la **encía del techo** (`GumCeiling`) y los de abajo salen
  del piso (`ToothStep`, dientes sólidos que sirven de escalón).
- **Gingivitis (nuevo):** un hueco largo sin piso que solo se cruza columpiándose de diente en diente (suelta y vuelve a
  lanzar el hilo en el aire), con un diente-isla a la mitad y una placa que se arranca colgado.
- Con R (clic central / Select) te enganchas al gancho más cercano, que se marca con un círculo amarillo.
- Con A/D te columpias; con W/S recoges o sueltas hilo. Saltar o volver a apretar R te suelta con impulso.
- La **placa entre dientes** (`PlaqueWall`) no se quita con balas: engánchala y jálala.
- **Muelas que mastican** (`ChewingMolar`): bajan a ritmo y aplastan.
- **Lengua** (`MovingPlatform`, estilo Tongue): plataformas que suben y bajan en ola.
- **Lluvia de azúcar** (`SugarRain`): si un S. mutans se come un dulce, se cura y crece; rompe los dulces disparándoles.
- **Olas de saliva** (`SalivaWave`): te curan y dañan a las bacterias.
- **Jefe: el Sarro** (`SarroBoss`).
  - Mientras tenga placas, las balas no le hacen nada.
  - Engánchale el hilo a una placa para arrancarla (le duele).
  - Sin placas queda expuesto 7 s; luego le vuelven a crecer.
- Arte opcional:
  - En `Art/Boca/`: DienteArriba, Muela, Placa, Dulce.
  - En `Art/Jefes/Sarro/`: Sarro_Normal, Sarro_Ataca, Sarro_Expuesto, Sarro_Derrotado y Placa.

**Amígdalas (190, secreto de la Tráquea):**
- **Entrada:** en la zona de la tos, junto al último anillo de cartílago, hay una corriente de aire escondida que te sube a una repisa con la entrada a la garganta.
- **Mecánica:**
  - Letreros **PASO 1, 2 y 3** explican una cosa a la vez.
  - El estreptococo (`Bacteria_SPyogenes`) y el rinovirus sueltan un **antígeno** al morir.
  - Se recoge con solo tocarlo y da vueltas a tu alrededor (`AntigenSystem`): puedes llevar **3** sin frenarte y sin
    dejar de disparar. Si te pegan se te cae uno.
  - En el **centro germinal** (`GerminalCenter`, brilla cuando llevas antígenos) se entregan todos: cada uno se vuelve un
    **linfocito B** (`AllyLymphocyte`) que te sigue y dispara anticuerpos 35 s (máximo 3 a la vez).
  - **Garganta profunda (nuevo):** olas de saliva al tragar (te curan y arrastran gérmenes), rinovirus que caen de arriba y
    una cripta infectada (arena) con su propio centro germinal.
- **Jefe: el Absceso** (`AbscessBoss`): es una bolsa cerrada y tus balas rebotan. Cuando un anticuerpo lo **marca**
  (brilla amarillo 4 s), **todas** tus balas le hacen daño. Llama estreptococos y escupe gotas.
- **Premio:** un linfocito B que te sigue en todos los niveles (secreto `premio_linfocito`).
- Arte opcional:
  - Enemigo: `Art/Enemigos/Respiratorio/SPyogenes_*`.
  - Jefe: `Art/Jefes/Absceso/Absceso_Normal, _Ataca, _Dano, _Derrotado`.

**Ojo (210, secreto del Cerebro):**
- **Entrada:** en el lóbulo occipital (la oscuridad), sobre el tercer hueco, hay dos repisas con el **nervio óptico**. Se ve bien con los destellos.
- **Mecánica:** sigilo (`StealthMeter`), con una barra de inflamación arriba.
  - La barra sube si un **vigilante** (`Watcher`, macrófago con cono de visión) te ve.
  - También sube si pisas la **retina** (`NoisyFloor`), si te alumbra un **haz de luz** (`LightBeam`) o si disparas.
  - Si se llena: **uveítis**. Todo se oscurece salvo un círculo, durante 6 s, y llegan herpes.
  - Escóndete agachado tras los pliegues (`HidingSpot`) o dentro de las **moscas volantes**.
  - El **punto ciego** (`BlindSpot`) es zona segura.
  - **Vasos de la retina (nuevo):** dos huecos de sangre que se cruzan en **glóbulo rojo**, con un islote vigilado en
    medio. En el glóbulo no te escondes: usa las moscas volantes que pasan.
- **Jefe: Toxoplasma** (`ToxoplasmaBoss`).
  - Se esconde en quistes; el quiste donde está **late y se le asoman los ojos** (pista).
  - Si te acercas sin que te vea, lo **sorprendes**: se marea 3.5 s y le puedes pegar (antiparasitario).
  - Si te ve (o se cansa de esperar), el quiste se hincha y **sale a atacar**: abanico de medias lunas o ráfaga directa.
    Luego se vuelve a esconder en otro quiste.
  - **El cristalino:** dispárale a la lente de arriba y enfoca la luz en el quiste más cercano a ti. Si el toxoplasma
    está ahí, lo **deslumbra** (se marea). El haz de luz que barre la arena también lo deslumbra si lo agarra afuera.
  - Abajo de la mitad de vida se enoja: se esconde menos, dispara más y salta de quiste en quiste soltando esporas.
- **El fondo está vivo** (`EyeBackdrop`).
  - El iris (`Art/Fondos/Fondo_Ojo_Iris.png`, recortado del fondo) te sigue con la mirada y tiembla si la inflamación sube.
  - Cada ~10 s el ojo **parpadea**: los párpados cierran la pantalla un momento y mientras están cerrados nadie te ve.
- **Premio:** el **escáner** (`SecretRewards`). Sobre cada enemigo aparece un puntito con el color del medicamento que le gana, en todos los niveles (secreto `premio_escaneo`).
- Arte opcional:
  - En `Art/Ojo/`: Macrofago, Cristalino.
  - En `Art/Jefes/Toxoplasma/`: Toxoplasma_Normal, _Mareado, _Ataca, _Derrotado.

## Sistema circulatorio (completo)

El cuarto sistema ya se juega completo desde el mapa del cuerpo: **Bazo** (a pie) → **Corazón** (a pie, al ritmo del
latido) → **Arterias y venas** (en vuelo por todo el cuerpo, 8 tramos; termina con el jefe del sistema, **el gran
trombo**), más el **Riñón** secreto. (El gran trombo era un nivel aparte; ahora es el final de Arterias y venas.) Plan y biología en el proyecto de Claude: `claude/plan-sistema-circulatorio.md`. Cada nivel también se
abre con *Healing Knight → Abrir escena → Circulatorio · ...*. Al desinfectar los cuatro sistemas sale el **final del
juego** (abajo).

**La nave** (`Gameplay/Flight/ShipFlight.cs`): NODO se vuelve una nanocápsula con el caballero sentado adentro.
- {mover} vuela en 8 direcciones, sin gravedad. Disparas siempre hacia adelante y las cápsulas llegan más lejos (×1.7).
- {dash}: empujón rápido hacia donde te mueves, invencible un instante. Mantener {fijar}: la nave se **encoge** (más
  chica y más lenta).
- La mitad de atrás de la cápsula y el faro se pintan del medicamento que traes.
- Un golpe no te aturde: solo parpadeas y te empuja un poco. El área de daño es chica (perdona los roces).

**El latido** (`Heartbeat.cs`): el reloj del nivel (84 por minuto). Suena "lub-dub", la pantalla tiembla y en cada
sístole la sangre empuja: la pantalla avanza de golpe y luego va lenta. Los enemigos disparan **al ritmo** y se ponen
rojos medio latido antes. `TachycardiaTrigger` sube el ritmo (taquicardia).

**La pantalla avanza sola** (`FlightRoute.cs`): la cámara sigue una línea de puntos (el centro del vaso) más el empujón de
cada latido. La **velocidad sube por zonas** (`ConfigureSpeeds`, sale el letrero "¡MÁS RÁPIDO!"): 3.1 u/s en la aorta
ascendente, 3.4 en el arco, 3.8 en la descendente, 4.1 en la abdominal, 4.4 en las ilíacas, 4.6 en la femoral y 5.2 en el
sprint después del minijefe. Al empezar esperas dentro del **ventrículo izquierdo** y el latido te **dispara a la aorta**. Se
detiene frente a lo que tapa todo (`FlightBlockers`: un coágulo completo o un minijefe, solo si está ADELANTE de la nave) y
al final. Si te quedas atorado contra algo sólido y la orilla izquierda te alcanza, te aplasta (−15) y te acomoda en el
lugar libre más cercano. Si mueres, la pantalla regresa al punto de guardado, se borran los
enemigos y balas de la pantalla y se rearman las oleadas de adelante (las que ya se veían salen escalonadas).

**Piezas del nivel:**
- `FlightWave` + `FlightEnemy`: oleadas de los enemigos de siempre (vida, debilidades y dibujo), con su IA de piso
  apagada. Patrones: `Line` (cruza, con ondas), `Swoop` (picada hacia tu altura), `Hover` (se queda disparando y se va),
  `Chase` (te persigue un rato). Entran por la derecha, arriba o abajo; con `worldX` salen de la boca de una rama.
  Con `splitPrefab` se separan al morir (racimo de S. aureus → cocos).
- `BloodStream`: tráfico de glóbulos rojos (`Art/Respiratorio/GloboRojo.png`). Te empujan suave y **detienen las balas
  enemigas** (sirven de escudo); tus cápsulas y las de tus linfocitos los **atraviesan**.
- `ShipMissile`: en la nave, {bomba} lanza un **MISIL** en vez de la bomba (el HUD dice MISILES). Sale hacia adelante,
  acelera, **busca** al enemigo más cercano que tenga enfrente y revienta con el medicamento que traes (daño de bomba ×1.3,
  radio ×1.15); la onda borra las balas enemigas cercanas. Gasta las mismas cargas que las bombas.
- `FlightPowerUp` (en `FlightPowerUps.cs`): **power-ups** que entran por la derecha cuando la pantalla llega a su X y se
  quedan atrás despacio; la nave los agarra al tocarlos (si mueres, los de adelante vuelven a salir).
  **TRIPLE DISPARO** (burbuja dorada): 3 cápsulas en abanico por 15 s (otro suma tiempo, hasta 30; aro dorado que
  parpadea al final). **+MISILES** (burbuja roja): +3, aunque pases del máximo (hasta 15). **GLÓBULO BLANCO** (célula
  blanca con núcleo morado): se vuelve un **LINFOCITO** de escolta (`FlightAlly`, hasta 3) que vuela junto a la nave
  **todo el nivel** y le dispara al enemigo más cercano con tu medicamento (60 % del daño); con la escolta completa da +2
  misiles. En Arterias y venas hay 25 (por rama en las ilíacas; 6 en el corazón y la persecución final). Las oleadas del arranque de la
  aorta son más chicas y espaciadas.
- `ClotGate`: coágulo de plaquetas y fibrina. Solo lo deshace el **ANTICOAGULANTE**, la cápsula regional nueva
  (turquesa, como la heparina; amenaza `Coagulo`). Completo = la pantalla se detiene hasta disolverlo; pegado a la pared =
  te lastima si lo tocas. Con `formTime` se va **formando**: al entrar a la pantalla sus dos mitades crecen desde las
  paredes; si lo disuelves antes (o cruzas el hueco) pasas, si se cierra se vuelve completo.
- `FlightSpeedPad` (en `FlightSpeed.cs`): **anillos de velocidad**. **ADRENALINA** (naranja, flechas hacia adelante):
  2.6 s con la pantalla ×1.75, la nave invencible y **embistiendo** enemigos (golpe de 40), pero el corazón late +30 y
  disparan más seguido. **BETABLOQUEADOR** (azul, flechas hacia atrás): 4 s con todo ×0.5 y el corazón −26 (antes de las
  partes difíciles). Si mueres y regresas antes del anillo, vuelve a salir.
- `ThickBloodZone`: **sangre espesa** (niebla amarilla con gotitas): mientras la pantalla va por ahí, todo va a ×0.6, la
  nave a ×0.55 y sin dash. Está en la ilíaca derecha (muchos glóbulos rojos) y en la femoral (mucha grasa).
- `SpeedBanner`: el letrero grande de arriba ("¡MÁS RÁPIDO!", "¡ADRENALINA!", "SANGRE ESPESA").
- `PlaqueBump`: **placa de colesterol** pegada a la pared (sólida). Se encoge con cada golpe hasta romperse; el
  antiinflamatorio le hace más (las demás cápsulas, parcial).
- `FlowZone`: corriente que te empuja (abajo de las ramas del arco de la aorta te jala hacia arriba).
- `HeartPulse`: adornos que laten (el músculo del ventrículo se aprieta, la válvula aórtica se abre, las paredes del vaso
  se ensanchan con el pulso).
- Paredes del vaso: `VesselWall` / `VesselWallAlong` en `SceneSetup` (siguen la curva y son sólidas). Tejido nuevo
  `Tejido_Arteria_*` (`python Tools/Art/generar_tejido.py Arteria`). El fondo es el tema `Blood` de `AmbientLife`:
  glóbulos lejanos, plasma y rayitas de velocidad que corren más rápido en cada latido.

**Tramo 1, la Aorta** (0–360, ~1.7 min): ventrículo y válvula → aorta ascendente (sepsis: S. aureus y neumococo,
tráfico de glóbulos) → arco aórtico (sube; sus tres ramas te jalan y de ellas bajan enemigos) → el primer coágulo completo
(tutorial del anticoagulante) → aorta descendente (coágulos pegados, taquicardia y el enjambre final de sepsis).

**Tramo 2, aorta abdominal e ilíacas** (360–780, ~1.8 min):
- Cruzas el diafragma y el corazón se calma. De la aorta abdominal salen ramas que **jalan** hacia arriba o hacia abajo:
  tronco celíaco (estómago, hígado y bazo), mesentérica superior, las dos **renales** (jalan más fuerte: a los riñones les
  llega una quinta parte de la sangre) y la mesentérica inferior. Enemigos que bajan o suben de las ramas.
- **Meningococo** (meningococemia): cruza rapidísimo; antes de entrar parpadea un aviso (!) en la orilla (`warn`).
- **La bifurcación**: la aorta se parte en las dos **ilíacas** (una por pierna). Escoges pasando por arriba o por abajo de
  la punta. Arriba (izquierda): la pantalla va 15 % más rápido, más enemigos y dos **sueros intravenosos** (+vida y
  +1 bomba). Abajo (derecha): más glóbulos rojos, coágulos pegados y otro coágulo completo, y un suero.
- Si mueres antes de la punta, vuelves a escoger.

**Bifurcaciones en el código:** `FlightRoute.ConfigureFork(rutaArriba, rutaAbajo, punta, ...)`. `Branch` dice cuál escogiste
(1 arriba, 2 abajo). Lo que solo existe en una rama lleva `branch`: `FlightWaveSpec.branch`, `ClotGate.SetBranch`,
`BloodStream` y `FlightMessage` (mensaje de NODO por la X del centro de la pantalla). En `SceneSetup`: `WallWithGaps`
(paredes con huecos para las ramas), `BranchTube`, `Divider` (la punta y el tejido entre las ilíacas), `FlightNote` e
`IVSerum`.

**Tramo 3, la arteria femoral enferma** (806–1206, ~2.5 min con el minijefe): las dos ilíacas no se juntan, así que al
acabar la que escogiste la pantalla se va a **negro** y sigues por "una pierna" (`FlightRoute.ConfigureContinuation`).
- **Aterosclerosis**: placas de colesterol arriba y abajo (zigzag), y dos estrechos donde dos placas casi se tocan: pasas
  **encogido** ({fijar}) o rompiéndolas.
- Enemigos nuevos (figuras por ahora): **colesterol LDL** (bolitas amarillas, cualquier cápsula las revienta) y
  **células espumosas** (macrófagos llenos de grasa que te persiguen; antiinflamatorio).
- Dos **coágulos que se forman** y una placa rota con un coágulo encima (así empiezan los infartos). Un suero.
- **Minijefe: la PLACA DE ATEROMA** (`AtheromaBoss`): casi tapa la arteria y detiene la pantalla. Escupe gotas de grasa
  al ritmo del latido y suelta células espumosas. A la mitad de su vida **se rompe** y le sale un **coágulo** en la cara de
  enfrente: mientras exista, tus balas no le hacen nada; disuélvelo con anticoagulante (vuelve a salir a los 9 s).
- Código en `SceneSetup`: `FemoralCenterY`, `Plaque`, `PlaqueNarrow`, `PlaqueDome`, `FormingClot`, `AtheromaBossAt` y
  `BuildDiseasedFemoral`. Datos nuevos: `Lipido_LDL`, `Celula_Espumosa`, `Bloqueo_PlacaColesterol`, `Jefe_Ateroma`.

**Vuelo hacia ARRIBA (la cámara gira):** `FlightRoute.ConfigureRoll` gira la cámara por zonas. Con −90° lo que está
adelante (+X) se ve arriba: el mundo sigue siendo horizontal (todo lo que va por X sigue igual) pero se juega como nave
vertical. El control se gira igual (`ScreenToWorld`: arriba = adelante), `View`/`HalfWidth`/`HalfHeight` se miden en el
mundo (con −90° se cambian el ancho y el alto: adelante solo se ven 5.4) y el fondo gira con la cámara.

**Tramo 4, CAPILARES** (1214–1470): la sangre va más lento que en ningún lado (2.6). Tres **carriles** separados por
tejido con huecos para cambiarte; los glóbulos van en **fila** por carril (`BloodStream.ConfigureLanes`). **Células sin
oxígeno** (`OxygenCell`, azules): pasa cerca y se ponen rositas (+3 de vida); con el 80 % hay premio (+20 y una bomba).
**Pericitos** que angostan el carril (solo cabes encogido). Al final se juntan en una vena y la cámara **gira** (1446–1472).

**Tramo 5, VENAS DE LA PIERNA** (1474–1846, hacia arriba): `GravityPull` te jala para abajo. **Válvulas**
(`VenousValve`): dos aletas en "V" que se abren con la **bomba muscular** (`MusclePump`: cada ~4 s el músculo aprieta, te
avienta hacia arriba y la pantalla acelera) y se cierran (sólidas, la pantalla espera; si te agarran adentro, −8 y para
abajo). **Picadura de mosquito**: entran los **esporozoítos** del Plasmodium. **Várices**: la vena se hincha, válvulas
rotas, sangre que se regresa (`FlowZone` hacia atrás), **remolinos** (`FlowSwirl`), sangre estancada y un coágulo que se
forma (trombosis venosa profunda).

**Tramo 6, VENA CAVA** (1846–2196, ancha: casi toda la pantalla): llega la vena de la otra pierna, las renales y las hepáticas (`SideVein`, te empujan
hacia adentro); merozoítos y esporozoítos. **Jefe: PLASMODIUM** (`PlasmodiumBoss`): un anillo de glóbulos rojos da
vueltas y el parásito está escondido en el que tiene un **anillito morado**. Los sanos tapan las balas; si le pegas 4
veces al infectado lo revientas. Cada 11 s hay **FIEBRE**: el glóbulo revienta solo, salen merozoítos y el Plasmodium
queda al descubierto 5.5 s (antiparasitario). Después se mete a otro. Al vencerlo: "¡AL CORAZÓN!" y fin del nivel.
Datos nuevos: `Plasmodium_Esporozoito`, `Plasmodium_Merozoito`, `Jefe_Plasmodium`.

Sonidos nuevos: `Latido_Lub`, `Latido_Dub`, `Nave_Despegue`, `Coagulo_Disuelve`, `Disparo_Anticoagulante` e
`Impacto_Anticoagulante`. Música: **tribal** en los órganos e **industrial** en los vasos (ver "Música adaptativa").

### Bazo (a pie, 220 u)
- **Pulpa roja:** el piso es un río lento de glóbulos (`BloodConveyor`: banda transportadora; a favor vas rapidísimo).
  **Cordones** del bazo (`SplenicSlit`): ranuras que se abren un momentito (antes brillan): cruza con {dash}. Un
  **macrófago** gigante te persigue (`MacrophageChase`): se come a lo que se queda atorado.
- **Reserva de plaquetas:** montones de plaquetas tapan el paso; tutorial a pie del **anticoagulante**.
- **Pulpa blanca:** linfocitos que te ayudan con anticuerpos.
- **Jefe: EPSTEIN-BARR** (`EpsteinBarrBoss`, mononucleosis): casi no le hacen las balas; te ayudan 4 **linfocitos B**
  que le disparan. Cada rato infecta a uno (rayo morado): se vuelve morado y te ataca. Cúralo a balazos (el antiviral le
  hace más) y sus **linfocitos T** le pegan un golpazo al virus.

### Corazón (a pie, 230 u)
En este nivel el **ANTIPARASITARIO** (para el T. cruzi) toma el lugar del antiinflamatorio (`LevelManager.levelMedication`
/ `replacesMedication`: cualquier nivel puede dar un medicamento propio así).
Todo va al ritmo del **latido** (68 por minuto): aurícula → **válvulas** que se abren justo después de cada latido
(`BeatValve`; si se cierran contigo en medio te avientan) → ventrículo con el **techo que se aprieta** en la sístole
(`SqueezingChamber`: quédate abajo en el latido) → **cuerdas tendinosas** que son trampolines (`ChordaeTrampoline`) →
el **sistema eléctrico** (`ElectricPath`: párate en la tira y **surfea** el impulso) → una **vegetación** de
endocarditis (S. viridans) → mitral → ventrículo izquierdo → aórtica → miocardio con el jefe.
- **Jefe: TRYPANOSOMA CRUZI** (`TrypanosomaBoss`, Chagas): nada en ocho, parpadea y se lanza, escupe parásitos. Desde el
  65 % causa **ARRITMIA**: se esconde en el músculo, el latido se vuelve desordenado (`Heartbeat.Irregular`) y el piso da
  **toque** en cada latido (brilla antes: ¡salta!). Antiparasitario.

### Final de Arterias y venas: el corazón y EL GRAN TROMBO (jefe del sistema)
Al salir de la vena cava (x 2196) la pantalla se **endereza** para cruzar el **corazón** (horizontal: aurícula, válvulas,
músculo que se aprieta, suero y un glóbulo blanco). La arritmia que dejó el T. cruzi suelta un coágulo... y al salir por
la aorta (x 2290) la pantalla **vuelve a girar**: segundo tramo hacia ARRIBA (el primero son las venas de la pierna con el
Plasmodium), por la aorta y la **carótida** persiguiendo un coágulo enorme que va al cerebro
(`GreatThrombusBoss`). Suelta **émbolos**, a media vida **redes de fibrina** (una fila con un solo hueco) y al final
**taquicardia**. Solo el **anticoagulante** lo deshace. La barra de la derecha dice cuánto le falta para llegar al
cerebro: si llega, **EMBOLIA** y regresas al último punto de guardado (el coágulo no se cura).

### Riñón (secreto, ACUÁTICO, 384 u)
**Entrada:** en el tramo 2 de Arterias y venas, la arteria **renal izquierda** (la rama de arriba, x≈452) jala hacia
arriba. En su **boca** brilla un riñoncito: tócalo con la nave (NODO da una pista). Antes estaba más adentro de la rama,
fuera del alcance de la nave.
Sigue el camino del agua por la **nefrona**; casi todo se **nada** (`BuoyancyZone`: {saltar} da una brazada, cerca de la
superficie un salto fuera del agua, mantener abajo te hunde). Bajo el agua la música se oye apagada, hay burbujas, el
fondo es más oscuro y cada tramo tiene su color de agua. Aquí el **ANTIPARASITARIO** reemplaza al antihistamínico (por
las cercarias).
1. **Arteria renal y glomérulo (a pie):** arena de E. coli. Un colador tapa el paso: dispárale a las dos **arteriolas**
   (botones `ShootSwitch`, 9 s prendidos; brillan mientras están apagados), la AFERENTE desde el piso y la EFERENTE desde
   la plataforma. Con las dos prendidas sube la **presión**, el colador se abre 4 s y te empuja (`GlomerulusFilter`).
2. **Cápsula de Bowman y túbulo proximal (agua clara):** **reabsorción**: burbujas de glucosa, aminoácidos y agua que
   curan un poquito (`ReabsorptionBubble`); "bombas" de la pared que te jalan arriba o abajo (`WaterCurrent` con pulso);
   pasar por debajo y por encima de paredes; arena de **Leptospira**, **cercarias** y un **Proteus**; el **DIURÉTICO**
   (`DiureticPickup`: 8 s nadas más rápido y las corrientes de lado te llevan adelante).
3. **Asa de Henle, rama que baja (agua salada, ámbar):** entre más hondo más flotas: hay que mantener abajo para pasar
   por debajo de tres paredes; **cristales de sal** que giran y lastiman (`SaltCrystal`, 3 balazos los rompen);
   corrientes de ida y vuelta (la `HenleCurrent` del equipo vive junto a `WaterCurrent`); premios en lo hondo.
4. **Rama que sube:** un chorro te lanza arriba a una orilla alta (E. coli y cercarias; suero).
5. **Minijefe: COLONIA DE PROTEUS** (`ProteusColonyBoss`): una **biopelícula** que no se mueve. Dispara cristales en
   abanico, suelta Proteus (máx. 3) y a la mitad le caen cristales desde arriba (con sombra de aviso). Solo la **cara**
   recibe daño y **se regenera** si dejas de dispararle 2.5 s.
6. **Túbulo colector:** dos ductos, cada uno con su botón **ADH** que sube su agua para salir de un salto
   (`SwitchToWater`). Proteus y cercarias.
7. **Uréter:** agua bajita que **corre hacia adelante** y una **piedra** que rueda detrás de ti (`RollingStoneChase`).
8. **Jefe secreto: CÁLCULO GIGANTE** (`KidneyStoneBoss`): las balas **rebotan**; solo le hace daño pegarle en la
   **grieta que brilla** (litotricia). Tres grietas en orden; al romperse una suelta piedritas que ruedan, y da saltos con
   ondas por el piso. **Premio: DEPURACIÓN** (lo que te alenta te dura la mitad, en todo el juego).

Enemigos de agua (todos con dibujo en `Art/Enemigos/Circulatorio`): **Leptospira** (se lanza en picada; antibiótico),
**Cercaria** de Schistosoma (enjambres rápidos y débiles; antiparasitario), **Proteus** (dispara cristales; antibiótico).
La colonia de Proteus está en `Art/Jefes/ProteusColonia` (el dibujo se voltea para mirar hacia ti).

### Dibujos del circulatorio (por hacer)
Todo el sistema se ve con figuras hasta que existan sus PNG. Guarda cada dibujo con el **nombre exacto** y corre
*Regenerar proyecto base*: el juego lo usa solo (si falta uno, se queda la figura). Los prompts en inglés para hacerlos
están en el proyecto de Claude: `claude/sprites-circulatorio.md`.
- Hazlos con **fondo verde puro** (#00FF00), o **magenta** (#FF00FF) si el personaje es verde, y **mirando a la
  derecha**. `python Tools/Art/quitar_fondo_verde.py <archivo o carpeta>` quita el fondo (adivina el color por las
  esquinas), el halo de las orillas y recorta.
- **Hojas con todas las poses:** `python Tools/Art/cortar_hoja.py hoja.png <ruta/Prefijo> Normal Ataca Dano Muere`
  quita el fondo, separa los dibujos (en orden de lectura) y los guarda a la misma escala. Si ya existe
  `<Prefijo>_Normal.png` no lo reemplaza y escala las demás poses para que midan lo mismo; una pose `-` se ignora.
- **Ya están** (20 nov): los 7 jefes con sus 4 poses (Ateroma, Plasmodium, Epstein-Barr, T. cruzi, Gran trombo,
  Cálculo y la colonia de Proteus), los 10 enemigos con sus 4 poses (`LDL`, `CelulaEspumosa`, `Esporozoito`, `Merozoito`,
  `Embolo`, `SViridans`, `EColi`, `Leptospira`, `Cercaria` y `Proteus`), `Plaquetas` (bestiario), la nave, el linfocito B, los 8 letreros y los fondos del Bazo, el
  Corazón y el Riñón: el arte del sistema circulatorio está completo. Con dibujo, la placa de
  ateroma mide lo que su dibujo (a lo alto tapa casi toda la arteria), las grietas del cálculo brillan donde están
  dibujadas, y el gran trombo y T. cruzi ajustan dónde se les puede pegar.

| Qué | Archivo(s) | Notas |
|---|---|---|
| Enemigos | `Art/Enemigos/Circulatorio/<Nombre>_Normal.png` (+ `_Ataca`, `_Dano`, `_Muere`) | `LDL`, `CelulaEspumosa`, `Esporozoito`, `Merozoito`, `Embolo`, `SViridans`, `EColi`. Solo `_Normal` es obligatoria |
| Jefes | `Art/Jefes/<Jefe>/<Jefe>_Normal.png` (+ `_Ataca`, `_Enojado`, `_Derrotado`) | `Ateroma`, `Plasmodium`, `EpsteinBarr`, `TCruzi`, `GranTrombo`, `Calculo` (`BossPoses` cambia la pose: ataca, enojado desde la mitad, derrotado) |
| Linfocito B | `Art/Aliados/Resources/Aliados/LinfocitoB.png` y `LinfocitoB_Infectado.png` | Los aliados del Bazo, los del jefe Epstein-Barr y el premio de las Amígdalas |
| Nave | `Art/Nave/Nave_Capsula.png` | La cápsula blanca vista de lado, nariz a la derecha; su mitad de atrás se pinta sola del medicamento |
| Ficha del coágulo | `Art/Enemigos/Circulatorio/Plaquetas_Normal.png` | Solo para el bestiario |
| Letreros | `Art/Letreros/Circulatorio/Letrero_<Bienvenida, Info, Peligro, Salida, FlechaDerecha, FlechaArriba, FlechaAbajo, Secreto>.png` | Tablero en blanco. Al ponerlos hay que medir dónde va la palabra (`SignTextAreas`) |
| Fondos | `Art/Fondos/Fondo_Bazo.png`, `Fondo_Corazon.png`, `Fondo_Rinon.png` | ≈ 2:1, oscuros y sin cosas que parezcan plataformas |

En código: `Enemies/BossPoses.cs`, `PrefabSetup.AddBossPoses` / `DrawBoss`, `SceneSetup.BossDrawing` y
`CodeSprites.ResourceArt` / `AddArt` (para lo que se dibuja en Start). Bazo, Corazón y Riñón ya tienen sus letreros
(con figuras rojas) y el globo de NODO del circulatorio es rojo sangre con turquesa.

### Final del juego
Al desinfectar los cuatro sistemas, en el mapa del cuerpo sale una vez la pantalla **"¡PACIENTE 001: SANO!"**
(`BodyMapScreen.BuildEnding`, se guarda `endingSeen`).

**Controles guardados:** los cambios de teclas se guardan por acción y posición (`hk.controles2` en PlayerPrefs:
"acción|posición|tecla"). Antes se guardaba el JSON de Unity, que busca por un id que cambia cada vez que le das Play,
así que el esquema Clásico (WASD) se perdía y se regresaba a las flechas. Lo guardado a la antigua se rescata solo.

## Menú y mapa del cuerpo (estilo caricatura)

Toda la interfaz usa un lienzo de 1920 x 1080 que **nunca se aplasta**: en ventanas más anchas o más altas
(Free Aspect, 21:9, 4:3, 16:10) sobra espacio a los lados o arriba en vez de encimarse los textos
(`UIFactory.CreateCanvas`, modo *Expand*).

El menú principal y el mapa del cuerpo se dibujan por código con el estilo del juego (`UI/CartoonUI.cs`): formas
redondas con contorno grueso, brillo y sombra, letra **Titan One** (`Art/UI/Fuentes`, licencia OFL libre), botones que se
inflan y rebotan (`UI/CartoonButton.cs`) y microbios de los niveles flotando de fondo (`UI/UIMotion.cs`).
- Menú: fondo pintado (el del estómago, o `Art/UI/Portada.png` si existe), título que flota (o `Art/UI/Logo.png`),
  prólogo de NODO que se escribe letra por letra.
- Mapa: una tarjeta por sistema con la pintura de su primer órgano, número en insignia, estado en pastilla y cada nivel
  con una "foto" redonda de su fondo y palomita si ya está completado; cápsulas para las fórmulas obtenidas.

## Bestiario (tipo Pokédex)

Una ficha por cada patógeno del juego (37: 13 del Digestivo, 9 del Respiratorio, 5 del Nervioso y 10 del Circulatorio,
jefes incluidos). Las del Circulatorio todavía no tienen dibujo: se ven con silueta hasta que haya
`Art/Enemigos/Circulatorio/<Nombre>_Normal.png` o `Art/Jefes/<Jefe>/<Jefe>_Normal.png` (las rutas ya están puestas).
Se abre con el botón **Bestiario** en el menú principal, en el mapa del cuerpo (arriba a la derecha) y en la **pausa**.
- **Silueta:** mientras no lo encuentras sale su dibujo en negro con "???" y una pista de dónde buscarlo. La primera vez
  que el patógeno aparece **en pantalla** se desbloquea su ficha y sale un aviso arriba a la derecha
  ("¡NUEVO EN EL BESTIARIO!"). La primera vez que lo derrotas se marca con palomita. Todo se guarda en la partida
  (`seenPathogens` y `defeatedPathogens` en `SaveData`; "Borrar progreso" también lo borra).
- **Ficha:** dibujo grande, nombre común y científico, tipo (bacteria, virus, parásito...), dónde vive en el juego,
  **qué medicamento del juego le gana** (sale de la tabla de efectividad, así que si cambias el balance la ficha se
  actualiza sola) y los textos reales: qué enfermedad causa, datos de la vida real, cómo se trata de verdad, cómo se
  previene y una pista para el juego. Abajo lleva el aviso de que es información educativa y que solo un médico
  diagnostica y receta.
- **Controles:** flechas / stick para elegir, **Q / E** o **LB / RB** para cambiar de sistema, rueda del mouse,
  AvPág / RePág o stick derecho para leer más, **Esc / B** para volver.
- **Dónde está:** los textos viven en `Editor/DataSetup.cs` → `CreateBestiary()` (se crea `Data/Resources/Bestiario.asset`).
  El dibujo de cada ficha es el `_Normal` del enemigo (o del jefe) y lo asigna "Regenerar proyecto base". La pantalla es
  `UI/BestiaryScreen.cs` y quien desbloquea las fichas en los niveles es `UI/BestiaryTracker.cs` (va en cada escena).
- **Un patógeno nuevo:** agrega un `Ficha(...)` en `CreateBestiary()` con su `EnemyData` (el principal primero; después
  sus variantes, como crías, segmentos o su versión de jefe, para que también lo desbloqueen).

## Fondos de nivel (imagen)

El Estómago ya usa una imagen de fondo: `Assets/_Project/Art/Fondos/Fondo_Estomago.png` (pintura estilo caricatura, 1672×941).
Reemplaza la pared y los adornos de placeholder y se desliza lento con la cámara (`LevelBackdrop`): al inicio del
nivel se ve el lado izquierdo de la imagen y al final el derecho, así parece que está muy lejos.

Fondos ya puestos: Estómago (`Fondo_Estomago`, magenta), Intestino delgado (`Fondo_Delgado`, durazno, con bruma
al centro agregada) e Intestino grueso (`Fondo_Grueso`, la imagen original pasada a malva). Cada uno con su tejido
(`tissue = "Estomago" / "Delgado" / "Grueso"`) y su líquido: ácido verde, bilis amarilla y líquido turbio olivo
(`liquidDeep/liquidSurface/liquidOutline/liquidCrest` en el `LevelStyle`).

Respiratorio (morados fríos, como los colores originales de esos niveles): Nariz (`Fondo_Nariz`, ciruela, moco
amarillo lima), Tráquea (`Fondo_Traquea`, índigo, moco azulado) y Pulmones (`Fondo_Pulmones`, noche índigo, líquido
turquesa). A los tres se les bajó un poco la saturación y el brillo, con bruma al centro y orillas más oscuras, para que
no compitan con la acción. Con fondo pintado ya no se dibujan los cilios de relleno sobre el piso.

Nervioso (violeta oscuro con un acento de luz por nivel): Médula (`Fondo_Medula`, turquesa; líquido cefalorraquídeo),
Hipocampo (`Fondo_Hipocampo`, magenta con memorias de colores; líquido dorado) y Cerebro (`Fondo_Cerebro`, índigo con
sinapsis doradas; líquido magenta). Mismo filtro ligero que el respiratorio. Con tejido nuevo las plataformas ya no
llevan el tallo de relleno.

Boca (nivel secreto): `Fondo_Boca` (dientes, lengua, úvula y saliva), tejido rojo vino (más oscuro que la lengua
para que las plataformas resalten) y saliva azul pálido en los huecos.

Para ponerle fondo a otro nivel:
1. Guarda el PNG en `Art/Fondos/` (por ejemplo `Fondo_Intestino.png`). Ideal: más ancho que 16:9 (≈ 2:1) y sin
   cosas que parezcan plataformas; tonos oscuros/apagados para que el caballero y los enemigos resalten.
2. En `SceneSetup`, en el `LevelStyle` de ese nivel, agrega `backdrop = "Fondo_Intestino"`.
3. *Healing Knight → Crear/Regenerar proyecto base*.

El importador lo deja solo en Sprite, filtro Bilinear (pintura lisa) y tamaño ≈ 1.2 veces el alto de la pantalla.
En la escena, el objeto `Environment/Fondo` tiene opciones para ajustar a mano: cuánto se desliza, subirlo o bajarlo
y cuánto se queda atrás cuando la cámara sube. Si borras el PNG, el nivel vuelve a su fondo de placeholder.

## Fondos vivos

Cada nivel tiene vida de adorno en el fondo (`AmbientLife`). Es solo decoración: no hace daño ni estorba. Se pone sola según la imagen de fondo.
- Las partículas tienen **profundidad**: las lejanas son chicas, tenues y casi no se mueven al caminar; las cercanas son
  más grandes y se deslizan más (parallax). Las que flotan dan la vuelta por los lados, así nunca "se acaban".
- Una **viñeta** suave oscurece las orillas de la pintura (no el juego) y unas pocas **manchas de luz desenfocadas**
  pasan por delante, muy tenues, para dar sensación de profundidad (en el Ojo no, para ver todo claro).
- Las figuras nuevas son `CodeSprites.Glow` (luz suave), `Ring` (aro de burbuja), `Beam` (rayo) y `Vignette`.

| Fondo | Partículas y adornos | Movimiento del fondo |
|---|---|---|
| Nariz | polvo y corrientes de aire que cambian de sentido al respirar; gotas de moco que cuelgan, caen y salpican | respira despacio |
| Tráquea | corrientes de aire y bolitas de moco que los cilios suben hacia la garganta | respira despacio |
| Pulmones | glóbulos rojos que se ponen rojo vivo al inhalar (se oxigenan) y partículas que lleva el aire | respira al ritmo del nivel; racimos de alvéolos que se inflan |
| Estómago | burbujas de ácido con orilla que revientan con un aro | peristalsis y un vaivén (el estómago revuelve) |
| Intestino delgado | nutrientes dorados que suben hacia las vellosidades (absorción) | peristalsis con apretón |
| Intestino grueso | bacterias de la flora meneándose, burbujas de gas y gotas de moco | peristalsis con apretón |
| Boca | gotas de saliva que cuelgan, caen y salpican; burbujas; destellos en los dientes | — |
| Médula | impulsos que SALTAN de nodo en nodo por los axones de la pintura (conducción saltatoria) | pulso del líquido cefalorraquídeo y destellos |
| Cerebro | impulsos que viajan con su estela y "sinapsis" que se encienden al llegar | destellos de las neuronas |
| Hipocampo | burbujas de memoria con orilla tornasol que revientan; impulsos dorados | destellos |
| Amígdalas | gotas que caen, linfocitos flotando y burbujitas | late y se enrojece como si estuviera inflamado |
| Ojo | moscas volantes, polvo y **rayos de luz** que salen de la pupila (más fuertes si el ojo está alerta) | el iris te sigue y el ojo parpadea (`EyeBackdrop`) |
| Arterias y venas | glóbulos rojos lejanos, plasma, plaquetas y rayitas de velocidad que corren hacia atrás | late con el corazón (`Heartbeat`) |

## Easter egg: el diente

En el Estómago, en la plataforma más alta entre la primera y la segunda arena, flota un diente con cara. Es la entrada secreta a la **Boca**: al tocarlo brinca, gira, avisa y te lleva ahí. Desde ese momento la Boca aparece también en el mapa del cuerpo. La imagen está en `Art/Secreto/Diente_Secreto.png` y su sonido es `Diente_Secreto`.

**Mundo secreto (solo de prueba):** una idea para después, un planeta vivo invadido por patógenos alienígenas. No está en el juego ni en el mapa; solo se abre desde **Healing Knight → Abrir escena → Extra · Mundo secreto (prueba)**.

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
| Tuberculosis | Lenta y dura; de lejos tose (se ve la nube de la tos) gotitas en arco; se encierra en granuloma | Pared cerosa; granulomas | Solo Antibiótico |
| Meningococo | Se agazapa y cruza a toda velocidad | Infección rapidísima | Antibiótico |
| Poliovirus | Apunta (línea roja) y lanza un RAYO que te paraliza (lento); el dibujo y el ataque ya coinciden | Ataca neuronas motoras | Antiviral |
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
| `<archivo del enemigo>_<evento>` | Un enemigo en particular, p. ej. `Virus_Rinovirus_Disparo` (estornuda) o `Bacteria_SAureus_Muere` (¡plip-plop!) |
| `<tipo>_<evento>` | Todo un tipo si el enemigo no tiene el suyo: `Virus_`, `Bacteria_`, `Parasito_`, `Alergeno_`, `Bloqueo_`, `Jefe_`... |
| `Enemigo_<evento>` | Lo que no tenga ninguno de los dos anteriores |
| Eventos de enemigo | `Disparo`, `Muere`, `Prepara` (el aviso antes de atacar: se infla, gruñe, toma aire), `Ataca` (embestida, mordida, brinco, sale de abajo), `Especial` (se entierra, se vuelve espora o granuloma, se hace latente, aparece el huevo), `Eclosiona` (el huevo se rompe). Si un evento no tiene archivo, no suena |
| `Neurona_Hiperexcitada_Muere` | Calmar una neurona roja (vuelve a ser sana) |
| `Acido_Burbujeo` (bucle), `Acido_Quema` | Charco de ácido: burbujea cerca, chisporrotea al quemarte |
| `Lombriz_Retumbo`, `Lombriz_Sale`, `Lombriz_Entra`, `Lombriz_Ruge` | Aviso bajo tierra, salir, meterse, quedar expuesta / huir |
| `Alveolo_Infla`, `Alveolo_Desinfla`, `Alveolo_Rebote` | Alvéolos de los pulmones (inhala, exhala, trampolín) |
| `Colmena_Enjambre` | La colmena suelta su enjambre (su disparo es `Jefe_Colmena_Disparo` o `Jefe_Disparo`) |
| `Rayo_Aviso`, `Rayo_Cae` | Tormenta: el segundo de aviso y el rayo |
| `Neurona_Carga`, `Neurona_Descarga`, `Neurona_Golpe`, `Neurona_Infecta` | Neuronas: carga y descarga de la roja, golpe e infección de la sana |
| `Campo_Neuroprotector` | Bomba de anticonvulsivo |
| `Rabia_Chillido` | La cadena de rabia da la vuelta |
| `Diente_Secreto` | Tocar el diente secreto del Estómago (lleva a la Boca) |
| `Gas_Revienta`, `Moco_Dispara`, `Moco_Splat`, `Grasa_Revienta`, `Saliva_Ola`, `Azucar_Rompe`, `Muela_Mastica`, `Peristalsis_Ola`, `Puerta_Abre` | Burbujas de gas, cañón de moco, gotas de grasa, ola de saliva, lluvia de azúcar, muela, peristalsis, píloro |
| `Estornudo_Aviso` / `Estornudo`, `Tos_Aviso` / `Tos_Rafaga` | Ráfagas de la nariz ("a... a... ¡ACHÚ!") y de la tráquea |
| `Tos_Inhala`, `Tos_Fuerte` | Reina de la tos ferina: toma aire con silbido ("whoop") y tose |
| `Aire_Impulso`, `Oxigeno_Recoge` | Ventila de aire, burbuja de oxígeno |
| `Quiste_Sale`, `Quiste_Entra` | El toxoplasma sale y se esconde en sus quistes |
| `Ojo_Parpadeo`, `Memoria_Desvanece`, `Memoria_Recoge` | Parpadeo del ojo, burbuja de memoria que se borra, fragmento de memoria |
| `Piso_Ruidoso`, `Sigilo_Detectado` | Piso que cruje en el Ojo (te pueden oír) y el "¡!" cuando te ven |
| `Pulso_Ritmo`, `Reflejo_Arma`, `Reflejo_Salta` | Plataformas al ritmo, almohadilla de reflejo |
| `Secuencia_Nota_0`…`_4`, `Secuencia_Error`, `Secuencia_Correcta` | Puzle del lóbulo frontal: cada neurona tiene su nota (el orden también se aprende de oído) |
| `Recoger_Vida`, `Recoger_Capsula`, `Inmune_Activa`, `Placa_Rompe`, `Hilo_Engancha` | Vida, cápsulas (omeprazol, probiótico), placas de Peyer y centro germinal, placa que se rompe, hilo dental |
| `Nodo_Mensaje`, `Nodo_Alerta`, `Bestiario_Nuevo` | Llega un mensaje de NODO (normal o urgente), patógeno nuevo en el bestiario |
| `Ambiente_<Escena>` (bucle) | Fondo de cada nivel (los 12 tienen el suyo): el estómago revuelve y burbujea, la nariz respira, los pulmones respiran al mismo ritmo que el nivel (5.5 s) con el corazón al lado, la médula zumba y manda impulsos de un oído al otro, el hipocampo tiene campanitas de recuerdos, el ojo casi no suena (sigilo). Si una escena no tiene el suyo, usa `Ambiente_<Sistema>` |

Los sonidos que pasan fuera de la pantalla se oyen más bajo y, si están muy lejos, no suenan. Disparos e impactos: cortos (menos de 0.2 s) y en mono.

Los efectos los hace `Tools/Audio/generar_sonidos.py` (ahora con filtros, FM, reverb y eco por sonido) y los ambientes
de nivel `Tools/Audio/generar_ambientes.py` (bucles estéreo de 16 s; casi todo ruido y eventos sueltos, sin notas,
para no chocar con la música).

## Música adaptativa (por capas)

**Menú y mapa:** suenan con `Musica_Mapa` (si no existe, `Musica_Digestivo`). Al entrar ahí se apaga de golpe la canción
del nivel (y los remates), y al entrar a un nivel se apaga la del mapa: nunca se oyen dos canciones encimadas.

Cada nivel tiene su canción separada en 6 capas que suenan sincronizadas; el juego sube o baja cada una según lo
que pasa (`Scripts/Gameplay/Audio/AdaptiveMusic.cs`):

| Situación | Qué suena |
|---|---|
| Explorando | Ambiente + Armonía + Melodía suave |
| Enemigos a menos de 11 unidades | + Bajo |
| Arena en pelea | + Percusión, Melodía completa |
| Muchos enemigos / jefe | + Combate |
| Arena limpiada | remate "Victoria" en el siguiente tiempo del compás (la canción baja mientras suena), y luego se calma |
| Poca vida (35 % o menos) | todo "bajo el agua" (filtro) + latido de corazón |
| Jefe | cambia a la canción `<Sistema>_Jefe` al empezar un compás, con una subida ("Entrada"); al vencerlo, la del nivel regresa cuando acaba el remate |

La intensidad sube en ~1 s, espera 3 s y baja en ~4 s (para que no "parpadee").

- Archivos: `Assets/_Project/Audio/Resources/MusicAdaptive/<NombreDeLaEscena>/` con `Ambiente, Armonia, Melodia,
  Bajo, Percusion, Combate, Victoria` (y `Entrada` en las de jefe) + `manifest.json` (BPM, compases, largo).
- Los genera `Tools/Audio/generar_musica_adaptativa.py` (v5) (`--todos`, o `--nivel Digestivo_Estomago --preview`
  para oír la mezcla completa en `Tools/Audio/preview/`). Cada capa queda a un volumen fijo para que ninguna tape a
  las demás, y todas juntas nunca pasan de 0.9.
- Cada nivel sigue el género de su sistema:

| Nivel | Género | BPM | Qué la hace distinta |
|---|---|---|---|
| Boca (secreto) | Acid house | 124 | Rebota: acid de onda cuadrada, shuffle y cencerro |
| Estómago | Acid house | 128 | El clásico: 303 con acentos y slides, órgano de house |
| Intestino delgado | Acid house | 126 | Groove: más swing, acordes de 4 notas y shaker |
| Intestino grueso | Acid techno | 130 | Oscuro: bombo con retumbo, 303 distorsionada, stabs |
| Jefe digestivo | Acid house pesado | 134 | Dos 303, palmadas en cada tiempo al final |
| Nariz | Trance | 136 | Aire: lidio, bajo galopante, pad picado ("trancegate") |
| Tráquea | Trance | 138 | Clásico: bajo rodante, arpegios con eco |
| Pulmones | Trance | 138 | Respira: el pad se abre y crece al inhalar, bajo en contratiempo |
| Amígdalas (secreto) | Trance aireado | 136 | Campanitas de vidrio en lugar de arpegio |
| Jefe respiratorio | Psytrance | 145 | Bajo "bum-ba-ba-ba", acid agudo, gritos psicodélicos |
| Médula | Neurofunk | 172 | Rodante: bajo reese que no para |
| Cerebro | Neurofunk | 174 | Stabs con eco, reese que "habla" en el combate |
| Hipocampo | Neurofunk | 174 | Memoria: campanas de vidrio con mucho eco y recuerdos al revés |
| Ojo (secreto) | Liquid drum & bass | 160 | Sigilo: rhodes, sub tibio, batería suave |
| Jefe nervioso | Neurofunk pesado | 176 | Batería partida, reese y growl más duros |
| Bazo | Tribal (aldea) | 112 | Taikos y djembes, marimba pentatónica, coro "ooo", gritos ¡HEY! y flauta en la parte B |
| Arterias y venas | Industrial (tubería) | 124 | Bombo saturado, bajo EBM en dieciseisavos, metales, pistones de vapor, campanas metálicas |
| Corazón | Tribal (latido) | 136 | El taiko hace lub-dub cada 2 tiempos (= 68 latidos por minuto), coro "uuu", didgeridoo |
| Riñón (secreto) | Industrial (planta de agua) | 108 | Más tranquilo: bajo limpio, agua que corre por los tubos, gotas |
| El gran trombo | Industrial (máquina) | 130 | Locrio, retumbo de máquina grande, lead saturado |
| Jefe circulatorio | Tribal + industrial (guerra) | 140 | Tambores de guerra con caja de fierro, bajo de máquina y lead saturado (todos los jefes del sistema) |

- Producción v5: arreglo de 32 compases (A, A', B, B') con la melodía que cambia, redobles al final de cada 8,
  subidas de ruido y platillo antes de cada parte nueva, reverb y eco que dan la vuelta al loop (no se nota el corte),
  bombeo con el bombo (sidechain), swing en el house y un limitador suave en la batería.
- **Para Victor:** si hace su música, que exporte cada canción en esas 6 capas (mismo largo exacto, en loop) con los
  mismos nombres, y edite el `manifest.json` (sobre todo `bpm` y `bars`, que se usan para cambiar en el compás).
- Si una escena no tiene carpeta, suena la pista normal de `Resources/Music` como antes.
- El importador (`Editor/MusicAdaptiveImporter.cs`) pone esos audios en "Decompress On Load" para que las capas
  no se desfasen.

## Música (ejemplos)

Cada sistema tiene un género electrónico que suena a lo que pasa en ese órgano, con versión de jefe más rápida y pesada:

| Sistema | Género | BPM | Archivos |
|---|---|---|---|
| Digestivo | Acid house (bajo "acid" burbujeante = jugos gástricos) | 128 / 134 | `Musica_Digestivo`, `Musica_Digestivo_Jefe` |
| Respiratorio | Trance (pads que suben y bajan = respirar) | 138 / 145 | `Musica_Respiratorio`, `Musica_Respiratorio_Jefe` |
| Nervioso | Neurofunk (drum & bass eléctrico = neuronas) | 174 / 176 | `Musica_Nervioso`, `Musica_Nervioso_Jefe` |

Los niveles secretos tienen su propia música adaptativa: **Amígdalas** (`Respiratorio_Amigdalas`, trance aireado, 136 BPM, dórico) y **Ojo** (`Nervioso_Ojo`, liquid drum & bass, 160 BPM, menor armónica, más misterioso para el sigilo). Sus jefes usan la canción de jefe de su sistema.

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
- El combate **no** usa colisiones de física: disparos, cuchillo y bombas buscan `Hurtbox` (rectángulos) con matemática simple (`HitMath`). Por eso encogerse esquiva disparos altos y salta por encima de la embestida de la lombriz.
- Apuntado: libre con mouse o stick derecho (`AimMath.Toward`) o en 8 direcciones con teclas (`AimMath.Resolve`). Probado en `Tests/EditMode/Combat2DTests.cs`.

## Cómo agregar contenido

| Quiero... | Pasos |
|---|---|
| **Cambiar balance** (daño, velocidad, vida) | Edita el asset en `Data/` (funciona incluso en Play Mode) |
| **Un enemigo nuevo** | Crea un `EnemyData` (clic derecho → Create → Healing Knight → Enemigo), duplica un prefab de `Prefabs/Enemies`, asígnale el dato y cambia el arte de `Visual` |
| **Un nivel nuevo** | Duplica una escena de `Scenes/Levels`, crea un `LevelData` con el nombre exacto de la escena, agrégalo a la lista del `SystemData` y a Build Settings |
| **Una arena** | GameObject con `ArenaZone`: `triggerX`, límites, `floorY` y oleadas (prefab + dato + cantidad). La pantalla se cierra hasta acabar las oleadas |
| **Plataformas y huecos** | Plataforma: objeto en capa **Ground** con `BoxCollider2D` (*Used By Effector*) + `PlatformEffector2D`. Hueco: deja un espacio entre dos tramos de piso |
| **Una ficha del bestiario** | Agrega un `Ficha(...)` en `DataSetup.CreateBestiary()` con sus textos, el dibujo y los `EnemyData` que la desbloquean |
| **Un nivel secreto** | En su `LevelData` marca `hidden` y escribe un `secretId`; coloca un prefab `SecretPassage` con ese mismo id en algún nivel |
| **Un sistema nuevo** | Crea `SystemData` y sus `LevelData`, y agrégalo al `GameCatalog` |
| **Tu arte del personaje** | Importa el PSD, arma las piezas dentro de `Visual` del prefab `HealingKnight` y borra las figuras de prueba. Los scripts de la raíz no cambian |

## Reglas del repo

- `main` = estable. Trabaja en ramas `feature/nombre` y únelas con Pull Request.
- Una persona por escena a la vez.
- Mueve o renombra archivos **desde Unity** (para que su `.meta` viaje con ellos).
- Si usas archivos grandes (PSD, WAV), instala Git LFS una vez: `git lfs install`.
