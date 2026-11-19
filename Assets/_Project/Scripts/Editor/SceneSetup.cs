using System.Collections.Generic;
using System.IO;
using HealingKnight.Core.Progression;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Hazards;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealingKnight.EditorTools
{
    /// <summary>
    /// Crea las escenas base: menú, mapa del cuerpo, los 3 niveles del Sistema Digestivo en gris y un Sandbox.
    /// Los niveles son de prueba: muévelos, cambia oleadas y pon tu arte encima.
    /// </summary>
    internal class SceneSetup
    {
        public const string Folder = "Assets/_Project/Scenes";

        private readonly DataSetup data;
        private readonly PrefabSetup prefabs;
        private readonly List<string> created = new List<string>();

        public SceneSetup(DataSetup data, PrefabSetup prefabs)
        {
            this.data = data;
            this.prefabs = prefabs;
        }

        public IReadOnlyList<string> Created => created;

        public static string MainMenuPath => $"{Folder}/MainMenu.unity";
        public static string BodyMapPath => $"{Folder}/BodyMap.unity";
        public static string EstomagoPath => $"{Folder}/Levels/Digestivo/Digestivo_Estomago.unity";
        public static string DelgadoPath => $"{Folder}/Levels/Digestivo/Digestivo_IntestinoDelgado.unity";
        public static string GruesoPath => $"{Folder}/Levels/Digestivo/Digestivo_IntestinoGrueso.unity";
        public static string BocaPath => $"{Folder}/Levels/Digestivo/Digestivo_Boca.unity";
        /// <summary>Solo para verlo en el editor: no está en la compilación ni en el mapa.</summary>
        public static string SecretoPath => $"{Folder}/Extra/Secreto_MundoNuevo.unity";
        public static string SandboxPath => $"{Folder}/Sandbox/Sandbox.unity";
        public static string NarizPath => $"{Folder}/Levels/Respiratorio/Respiratorio_Nariz.unity";
        public static string TraqueaPath => $"{Folder}/Levels/Respiratorio/Respiratorio_Traquea.unity";
        public static string PulmonesPath => $"{Folder}/Levels/Respiratorio/Respiratorio_Pulmones.unity";
        public static string MedulaPath => $"{Folder}/Levels/Nervioso/Nervioso_Medula.unity";
        public static string CerebroPath => $"{Folder}/Levels/Nervioso/Nervioso_Cerebro.unity";
        public static string HipocampoPath => $"{Folder}/Levels/Nervioso/Nervioso_Hipocampo.unity";
        public static string OjoPath => $"{Folder}/Levels/Nervioso/Nervioso_Ojo.unity";
        public static string AmigdalasPath => $"{Folder}/Levels/Respiratorio/Respiratorio_Amigdalas.unity";

        public void Run()
        {
            CreateIfMissing(MainMenuPath, () => BuildMenuScene<MainMenuScreen>("MainMenu", new Color(0.35f, 0.08f, 0.14f)));
            CreateIfMissing(BodyMapPath, () => BuildMenuScene<BodyMapScreen>("BodyMap", new Color(0.12f, 0.06f, 0.1f)));
            CreateIfMissing(EstomagoPath, BuildEstomago);
            CreateIfMissing(DelgadoPath, BuildDelgado);
            CreateIfMissing(GruesoPath, BuildGrueso);
            CreateIfMissing(BocaPath, BuildBoca);
            CreateIfMissing(NarizPath, BuildNariz);
            CreateIfMissing(TraqueaPath, BuildTraquea);
            CreateIfMissing(PulmonesPath, BuildPulmones);
            CreateIfMissing(MedulaPath, BuildMedula);
            CreateIfMissing(CerebroPath, BuildCerebro);
            CreateIfMissing(HipocampoPath, BuildHipocampo);
            CreateIfMissing(OjoPath, BuildOjo);
            CreateIfMissing(AmigdalasPath, BuildAmigdalas);
            CreateIfMissing(SecretoPath, BuildMundoSecreto);
            CreateIfMissing(SandboxPath, BuildSandbox);

            // Se registran SIEMPRE (aunque una escena haya fallado), solo las que existen en disco.
            var scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene(BodyMapPath, true),
                new EditorBuildSettingsScene(EstomagoPath, true),
                new EditorBuildSettingsScene(DelgadoPath, true),
                new EditorBuildSettingsScene(GruesoPath, true),
                new EditorBuildSettingsScene(BocaPath, true),
                new EditorBuildSettingsScene(NarizPath, true),
                new EditorBuildSettingsScene(TraqueaPath, true),
                new EditorBuildSettingsScene(PulmonesPath, true),
                new EditorBuildSettingsScene(MedulaPath, true),
                new EditorBuildSettingsScene(CerebroPath, true),
                new EditorBuildSettingsScene(HipocampoPath, true),
                new EditorBuildSettingsScene(OjoPath, true),
                new EditorBuildSettingsScene(AmigdalasPath, true),
                new EditorBuildSettingsScene(SandboxPath, true),
            };
            EditorBuildSettings.scenes = System.Array.FindAll(scenes, sc => File.Exists(sc.path));
            if (failed.Count > 0)
                Debug.LogError($"[Healing Knight] {failed.Count} escena(s) no se pudieron armar: {string.Join(", ", failed)}. " +
                               "El error de cada una está arriba en la consola (el primero en rojo con 'Falló al armar').");
        }

        private readonly List<string> failed = new List<string>();

        private void CreateIfMissing(string path, System.Action build)
        {
            if (File.Exists(path)) return;
            SetupUtils.EnsureFolder(Path.GetDirectoryName(path));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Al abrir una escena nueva Unity descarga los assets que no se están usando, y las
            // referencias que teníamos a prefabs y datos quedan "destruidas". Se vuelven a cargar
            // del disco (no se crea nada nuevo: ya existen).
            data.Run();
            prefabs.Run();
            signTheme = SignThemeFor(path);
            signRoot = null;
            placedSigns.Clear();

            try
            {
                build();
                FinishSigns();
            }
            catch (System.Exception e)
            {
                // Un error en un nivel no debe impedir que se armen los demás ni que se registren en la compilación.
                Debug.LogError($"[Healing Knight] Falló al armar {Path.GetFileNameWithoutExtension(path)}: {e}");
                failed.Add(Path.GetFileNameWithoutExtension(path));
            }
            EditorSceneManager.SaveScene(scene, path);
            created.Add(path);
        }

        // ---------- Menús ----------

        private void BuildMenuScene<T>(string name, Color background) where T : Component
        {
            CreateCamera(background, false);
            var go = new GameObject(name, typeof(T));
            ConfigureMenuArt(go.GetComponent<T>());
        }

        private const string UIArtFolder = "Assets/_Project/Art/UI";

        /// <summary>
        /// Le da al menú y al mapa del cuerpo el arte del juego: la letra (Titan One), los microbios que flotan de fondo,
        /// el fondo pintado y, en el mapa, la "foto" de cada nivel (su fondo pintado).
        /// Opcionales: Art/UI/Portada.png (fondo del menú) y Art/UI/Logo.png (título dibujado).
        /// </summary>
        private void ConfigureMenuArt(Component screen)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>($"{UIArtFolder}/Fuentes/TitanOne-Regular.ttf");
            SetupUtils.SetRef(screen, "titleFont", font);
            SetupUtils.SetRef(screen, "bodyFont", font);

            var bugs = new List<Object>();
            string[] bugFiles =
            {
                "Estomago/HPylori_Normal", "Estomago/HPyloriUreasa_Normal", "Estomago/Anisakis_Arrastra",
                "Delgado/Giardia_Normal", "Delgado/Rotavirus_Normal", "Delgado/Proglotido_Normal",
                "Grueso/Entamoeba_Normal", "Grueso/CDifficile_Normal", "Grueso/Oxiuro_Normal",
                "Grueso/Flora_Normal", "Grueso/Flora_Bifido", "Grueso/Flora_Strepto", "Grueso/Flora_Akkermansia"
            };
            foreach (string f in bugFiles)
            {
                Sprite sp = PrefabSetup.ArtByWidth($"{PrefabSetup.EnemyArtRoot}/{f}.png", 1.5f, new Vector2(0.5f, 0f));
                if (sp != null) bugs.Add(sp);
            }
            SetupUtils.SetRefArray(screen, "microbes", bugs.ToArray());

            if (screen is MainMenuScreen)
            {
                Sprite cover = PrefabSetup.ArtByWidth($"{UIArtFolder}/Portada.png", 19.2f);
                if (cover == null) cover = LoadBackdrop("Fondo_Estomago");
                SetupUtils.SetRef(screen, "background", cover);
                SetupUtils.SetRef(screen, "logo", PrefabSetup.ArtByWidth($"{UIArtFolder}/Logo.png", 10f));
            }
            else if (screen is BodyMapScreen && data.Catalog != null)
            {
                var ids = new List<string>();
                var art = new List<Object>();
                foreach (SystemData system in data.Catalog.systems)
                {
                    if (system == null) continue;
                    foreach (LevelData level in system.levels)
                    {
                        if (level == null) continue;
                        // "digestivo_estomago" -> Fondo_Estomago
                        string key = level.id.Substring(level.id.IndexOf('_') + 1);
                        Sprite sp = key.Length > 0 ? LoadBackdrop("Fondo_" + char.ToUpper(key[0]) + key.Substring(1)) : null;
                        if (sp == null) continue;
                        ids.Add(level.id);
                        art.Add(sp);
                    }
                }
                var so = new SerializedObject(screen);
                SerializedProperty idsProp = so.FindProperty("levelIds");
                idsProp.arraySize = ids.Count;
                for (int i = 0; i < ids.Count; i++) idsProp.GetArrayElementAtIndex(i).stringValue = ids[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                SetupUtils.SetRefArray(screen, "levelArt", art.ToArray());
            }
        }

        // ---------- Piezas comunes de nivel ----------

        private static Camera CreateCamera(Color background, bool gameplay)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 2.5f, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            go.AddComponent<AudioListener>();
            if (gameplay) go.AddComponent<GameCamera>();

            AddGlobalLight2D();
            return cam;
        }

        /// <summary>
        /// Agrega una luz global 2D (URP). Se busca por nombre porque, según la versión de Unity,
        /// Light2D vive en un assembly distinto; así no dependemos de cuál sea.
        /// </summary>
        private static void AddGlobalLight2D()
        {
            System.Type lightType = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                lightType = assembly.GetType("UnityEngine.Rendering.Universal.Light2D");
                if (lightType != null) break;
            }
            if (lightType == null) return;

            var go = new GameObject("Global Light 2D");
            Component light = go.AddComponent(lightType);

            var typeProp = lightType.GetProperty("lightType");
            if (typeProp != null && typeProp.CanWrite)
                typeProp.SetValue(light, System.Enum.Parse(typeProp.PropertyType, "Global"));

            var intensityProp = lightType.GetProperty("intensity");
            if (intensityProp != null && intensityProp.CanWrite) intensityProp.SetValue(light, 1f);
        }

        private enum Decor { Blobs, Rings, Alveoli, Veins, Villi, Folds, Neurons, None }

        private struct LevelStyle
        {
            public Color background, wall, floor, floorEdge, blob;
            public Decor decor;
            /// <summary>Si alfa > 0: luz al final del nivel y bruma de color.</summary>
            public Color glow;
            /// <summary>Cilios a lo largo del piso (Sistema Respiratorio).</summary>
            public bool cilia;
            /// <summary>Si alfa > 0: líquido brillante en el fondo de los huecos (ácido, bilis...). Caer sigue costando vida.</summary>
            public Color pitLiquid;
            /// <summary>Si alfa > 0: gotas que escurren de las plataformas y la orilla del piso.</summary>
            public Color drip;
            /// <summary>Venas o detalles del fondo (Decor.Veins/Villi/Folds/Neurons).</summary>
            public Color detail;
            /// <summary>Las plataformas tienen tallo hasta el piso (tejido nervioso tipo hongo).</summary>
            public bool platformStems;
            /// <summary>
            /// Imagen de fondo (nombre del PNG en Art/Fondos, sin ".png"). Si existe, reemplaza la pared y los
            /// adornos de placeholder y se desliza lento con la cámara (LevelBackdrop). Si no existe, se usa lo de siempre.
            /// </summary>
            public string backdrop;
            /// <summary>
            /// Lagos de ácido HUNDIDOS en el piso (x = inicio, y = ancho): el piso baja un poco y el ácido llena la
            /// cavidad. Si hay alguno, los huecos también se llenan de ácido animado (en vez del líquido fijo).
            /// </summary>
            public Vector2[] basins;
            /// <summary>
            /// Alberca de marea al final del nivel: x = inicio, y = ancho, z = hondo, w = hasta dónde sube el ácido
            /// SOBRE el piso. Siempre tiene ácido (nunca se seca); solo se cruza sobre la comida que flota.
            /// </summary>
            public Vector4 tidePool;
            /// <summary>
            /// Colores del líquido animado (huecos y lagos): hondo, superficie, contorno, brillo.
            /// Si 'liquidSurface' no tiene alfa, se usa el ácido verde de siempre.
            /// </summary>
            public Color liquidDeep, liquidSurface, liquidOutline, liquidCrest;
            /// <summary>
            /// Arte pixel del piso y las plataformas (Art/Tejidos/Tejido_&lt;nombre&gt;_Piso/_Relleno/_Plataforma.png,
            /// los genera Tools/Art/generar_tejido.py). Vacío o sin PNG = bloques de color de siempre.
            /// </summary>
            public string tissue;
        }

        private const string TissueFolder = "Assets/_Project/Art/Tejidos";
        private const float TissuePixelsPerUnit = 256f; // arte de caricatura en alta resolución (Tools/Art)
        private static readonly Dictionary<string, Sprite> tissueCache = new Dictionary<string, Sprite>();
        private Sprite tissueFloor, tissueFill, tissuePlatform, softShadow, verticalShadow;

        /// <summary>
        /// Carga un PNG de Art/Tejidos listo para repetirse (drawMode Tiled): Sprite, malla completa (FullRect).
        /// 'pixelArt' = filtro Point sin mipmaps; si no, Trilinear con mipmaps (dibujo liso que se ve chico en pantalla).
        /// 'border' = píxeles de las orillas que NO se repiten (izq, abajo, der, arriba), para las puntas de las plataformas.
        /// Se carga una vez por armado (si se reimporta dos veces, la primera referencia se pierde).
        /// </summary>
        private static Sprite LoadTissue(string file, Vector2 pivot, Vector4 border, bool pixelArt = false, string folder = TissueFolder)
        {
            string path = $"{folder}/{file}.png";
            if (tissueCache.TryGetValue(path, out Sprite cached) && cached != null) return cached;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            FilterMode filter = pixelArt ? FilterMode.Point : FilterMode.Trilinear;
            bool needs = importer.textureType != TextureImporterType.Sprite
                         || importer.filterMode != filter
                         || Mathf.Abs(importer.spritePixelsPerUnit - TissuePixelsPerUnit) > 0.01f
                         || settings.spriteMeshType != SpriteMeshType.FullRect
                         || settings.spriteAlignment != (int)SpriteAlignment.Custom
                         || (settings.spritePivot - pivot).sqrMagnitude > 1e-4f
                         || importer.spriteBorder != border
                         || importer.mipmapEnabled == pixelArt;
            if (needs)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = TissuePixelsPerUnit;
                importer.filterMode = filter;
                importer.mipmapEnabled = !pixelArt;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spriteBorder = border;
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            tissueCache[path] = sprite;
            return sprite;
        }

        /// <summary>Prepara el arte de tejido del nivel (si tiene). Lo llama BuildLevelBase.</summary>
        private void LoadLevelTissue(string name)
        {
            tissueCache.Clear();
            tissueFloor = tissueFill = tissuePlatform = null;
            var top = new Vector2(0.5f, 1f);
            if (!string.IsNullOrEmpty(name))
            {
                tissueFloor = LoadTissue($"Tejido_{name}_Piso", top, Vector4.zero);
                tissueFill = LoadTissue($"Tejido_{name}_Relleno", top, Vector4.zero);
                tissuePlatform = LoadTissue($"Tejido_{name}_Plataforma", top, new Vector4(96f, 0f, 96f, 0f));
                if (tissueFloor == null || tissueFill == null) tissueFloor = tissueFill = null;
            }
            softShadow = LoadTissue("Sombra_Suave", new Vector2(0.5f, 0.5f), Vector4.zero, false);
            verticalShadow = LoadTissue("Sombra_Vertical", new Vector2(0.5f, 0f), Vector4.zero, false);
        }

        /// <summary>Sprite que se REPITE (no se estira) para cubrir 'size'. El pivote del sprite decide dónde queda.</summary>
        private static SpriteRenderer TiledChild(Transform parent, string name, Sprite sprite, Vector3 localPosition, Vector2 size, int order, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        private static readonly Color ShadowColor = new Color(0.06f, 0f, 0.03f, 1f);

        private const float BasinDepth = 1.2f;
        /// <summary>Huecos y lagos hundidos del nivel actual (para no poner moco encima de ellos).</summary>
        private List<Vector2> currentCuts = new List<Vector2>();

        private const string BackdropFolder = "Assets/_Project/Art/Fondos";

        /// <summary>
        /// Los fondos son pinturas lisas: filtro Bilinear, sin compresión y con píxeles por unidad tal que
        /// la imagen mida 1.2 veces el alto de la pantalla (10.8 unidades). LevelBackdrop igual lo reescala si hace falta.
        /// </summary>
        /// <summary>Partículas de adorno y "latido" del fondo según el órgano (AmbientLife).</summary>
        private static void AddAmbientLife(GameObject back, string backdropName)
        {
            AmbientLife.Theme theme;
            int count = 30;
            switch (backdropName)
            {
                case "Fondo_Nariz": case "Fondo_Traquea": theme = AmbientLife.Theme.Air; count = 40; break;
                case "Fondo_Pulmones": theme = AmbientLife.Theme.Lungs; count = 36; break;
                case "Fondo_Estomago": theme = AmbientLife.Theme.Acid; break;
                case "Fondo_Delgado": theme = AmbientLife.Theme.Gut; break;
                case "Fondo_Grueso": theme = AmbientLife.Theme.Colon; count = 24; break;
                case "Fondo_Boca": theme = AmbientLife.Theme.Mouth; break;
                case "Fondo_Medula": case "Fondo_Cerebro": theme = AmbientLife.Theme.Nerve; count = 34; break;
                case "Fondo_Hipocampo": theme = AmbientLife.Theme.Memory; break;
                case "Fondo_Amigdalas": theme = AmbientLife.Theme.Throat; break;
                case "Fondo_Ojo": theme = AmbientLife.Theme.Eye; count = 24; break;
                default: return;
            }
            back.AddComponent<AmbientLife>().Configure(theme, count);
        }

        /// <summary>
        /// Ojo: si existe 'Fondo_Ojo_Iris.png' (el iris recortado del fondo), lo pone encima en su lugar y le agrega
        /// EyeBackdrop (el iris te sigue con la mirada y el ojo parpadea).
        /// </summary>
        private static void AddEyeIris(GameObject back, string backdropName, Sprite backdrop)
        {
            if (backdropName != "Fondo_Ojo") return;
            string path = $"{BackdropFolder}/Fondo_Ojo_Iris.png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null) return;
            float ppu = backdrop.rect.height / (10.8f * 1.2f);
            Sprite irisSprite = PrefabSetup.ArtSprite(path, ppu, new Vector2(0.5f, 0.5f));
            var iris = new GameObject("Iris");
            iris.transform.SetParent(back.transform, false);
            // El iris estaba centrado en (906, 201) del dibujo de 1774 x 887.
            Vector2 px = new Vector2(906f, 201f);
            iris.transform.localPosition = new Vector3((px.x - backdrop.rect.width * 0.5f) / ppu, (backdrop.rect.height * 0.5f - px.y) / ppu, 0f);
            var r = iris.AddComponent<SpriteRenderer>();
            r.sprite = irisSprite;
            r.sortingOrder = -299;
            back.AddComponent<EyeBackdrop>().Configure(iris.transform);
        }

        private static Sprite LoadBackdrop(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string path = $"{BackdropFolder}/{name}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            float ppu = height / (10.8f * 1.2f);
            if (importer.textureType != TextureImporterType.Sprite
                || importer.filterMode != FilterMode.Bilinear
                || Mathf.Abs(importer.spritePixelsPerUnit - ppu) > 0.01f
                || importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = ppu;
                importer.filterMode = FilterMode.Bilinear; // pintura lisa (no pixel art)
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 4096;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private const float FloorY = 0f;   // la parte de arriba del piso
        private LevelStyle currentStyle;
        private Transform environment;

        /// <summary>
        /// Nivel base en vista lateral: pared de fondo, piso sólido (capa Ground) con huecos opcionales,
        /// jugador, HUD y LevelManager. 'pits' = huecos en el piso (x = inicio, y = ancho): caer cuesta vida.
        /// </summary>
        private LevelManager BuildLevelBase(float length, LevelStyle style, string intro, params Vector2[] pits)
        {
            CreateCamera(style.background, true);
            currentStyle = style;

            float startX = -10f;
            float endX = startX + length;

            environment = new GameObject("Environment").transform;
            currentLakes.Clear();
            new GameObject("PremiosSecretos").AddComponent<SecretRewards>();   // linfocito aliado y escáner (si ya los ganaste)
            LoadLevelTissue(style.tissue);
            Sprite backdrop = LoadBackdrop(style.backdrop);
            if (backdrop != null)
            {
                var back = new GameObject("Fondo");
                back.transform.SetParent(environment, false);
                var renderer = back.AddComponent<SpriteRenderer>();
                renderer.sprite = backdrop;
                renderer.sortingOrder = -300;
                back.AddComponent<LevelBackdrop>().Configure(startX, endX);
                AddEyeIris(back, style.backdrop, backdrop);
                AddAmbientLife(back, style.backdrop);
                style.decor = Decor.None; // la imagen ya trae su decorado
            }
            else
            {
                SetupUtils.SpriteChild(environment, "BackWall", PlaceholderArt.Square, style.wall,
                    new Vector3(startX + length * 0.5f, FloorY, 0f), new Vector2(length + 30f, 10f), -200);
            }

            // Piso por tramos (los huecos y los lagos hundidos quedan entre tramos).
            var sorted = new List<Vector2>(pits ?? new Vector2[0]);
            sorted.Sort((a, b) => a.x.CompareTo(b.x));
            var cuts = new List<Vector2>(sorted);
            if (style.basins != null) cuts.AddRange(style.basins);
            if (style.tidePool.y > 0f) cuts.Add(new Vector2(style.tidePool.x, style.tidePool.y));
            cuts.Sort((a, b) => a.x.CompareTo(b.x));
            currentCuts = cuts;
            float segmentStart = startX - 15f;
            int index = 0;
            foreach (Vector2 cut in cuts)
            {
                FloorSegment($"Floor_{index++}", segmentStart, cut.x);
                segmentStart = Mathf.Max(segmentStart, cut.x + cut.y);
            }
            FloorSegment($"Floor_{index}", segmentStart, endX + 15f);

            // Sombra de contacto: el fondo se oscurece justo arriba del piso (el piso deja de verse "pegado").
            if (tissueFloor != null && verticalShadow != null)
            {
                Vector2 aoSize = verticalShadow.bounds.size;
                var aoColor = new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, 0.6f);
                SetupUtils.SpriteChild(environment, "SombraPiso", verticalShadow, aoColor,
                    new Vector3(startX + length * 0.5f, FloorY - 0.05f, 0f), new Vector2((length + 30f) / aoSize.x, 1.7f / aoSize.y), -160); // opaca abajo
            }

            // Líquido animado en huecos: si hay lagos o si el nivel usa el arte de tejido nuevo.
            bool animatedAcid = (style.basins != null && style.basins.Length > 0) || tissueFloor != null;
            if (style.basins != null)
            {
                foreach (Vector2 basin in style.basins) Basin(basin.x, basin.y);
            }
            if (style.tidePool.y > 0f)
            {
                Vector4 t = style.tidePool;
                BasinFloor(t.x, t.y, t.z);
                AcidLake tide = Lake(t.x, t.x + t.y, t.z - 0.15f, FloorY - t.z);
                tide.name = "MareaAcido";
                tide.Configure(t.y, t.z - 0.15f, true, t.z + t.w);
                currentLakes.Add(tide);
            }

            if (style.pitLiquid.a > 0f)
            {
                foreach (Vector2 pit in sorted)
                {
                    if (animatedAcid) PitAcid(pit.x, pit.y);
                    else PitLiquid(pit.x, pit.y);
                }
            }

            var random = new System.Random(length.GetHashCode());
            switch (style.decor)
            {
                case Decor.Rings: // anillos de cartílago de la tráquea
                    for (float x = startX - 5f; x < endX + 10f; x += 3.2f)
                    {
                        SetupUtils.SpriteChild(environment, "Ring", PlaceholderArt.Square, style.blob,
                            new Vector3(x, FloorY, 0f), new Vector2(0.9f, 10f), -150);
                        SetupUtils.SpriteChild(environment, "RingEdge", PlaceholderArt.Square, new Color(style.glow.r, style.glow.g, style.glow.b, 0.25f),
                            new Vector3(x + 0.45f, FloorY, 0f), new Vector2(0.12f, 10f), -149);
                    }
                    break;

                case Decor.Alveoli: // racimos de alvéolos: burbujas translúcidas
                    for (float x = startX - 5f; x < endX + 10f; x += 5f + (float)random.NextDouble() * 4f)
                    {
                        float cy = FloorY + 3f + (float)random.NextDouble() * 4.5f;
                        int count = 4 + random.Next(4);
                        for (int i = 0; i < count; i++)
                        {
                            float size = 0.8f + (float)random.NextDouble() * 1f;
                            Vector3 p = new Vector3(x + (float)(random.NextDouble() - 0.5) * 2.2f, cy + (float)(random.NextDouble() - 0.5) * 2.2f, 0f);
                            SetupUtils.SpriteChild(environment, "Alveolo", PlaceholderArt.Circle, style.blob, p, new Vector2(size, size), -150);
                            SetupUtils.SpriteChild(environment, "Brillo", PlaceholderArt.Circle, new Color(1f, 1f, 1f, 0.25f),
                                p + new Vector3(-size * 0.2f, size * 0.2f, 0f), new Vector2(size * 0.3f, size * 0.3f), -149);
                        }
                    }
                    break;

                case Decor.Veins: // estómago: venas cian ramificadas y bultos de tejido
                    for (float x = startX - 5f; x < endX + 10f; x += 6f + (float)random.NextDouble() * 4f)
                    {
                        float y = FloorY + 1f;
                        float angle = 90f + (float)(random.NextDouble() - 0.5) * 30f;
                        for (int i = 0; i < 5; i++)
                        {
                            float len = 1.6f + (float)random.NextDouble() * 1.2f;
                            float rad = angle * Mathf.Deg2Rad;
                            Vector3 mid = new Vector3(x + Mathf.Cos(rad) * len * 0.5f, y + Mathf.Sin(rad) * len * 0.5f, 0f);
                            SetupUtils.SpriteChild(environment, "Vena", PlaceholderArt.Capsule, style.detail, mid, new Vector2(len, 0.16f), -148)
                                .transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                            // Ramita
                            if (i % 2 == 1)
                                SetupUtils.SpriteChild(environment, "Ramita", PlaceholderArt.Capsule, style.detail,
                                    mid + new Vector3(0.4f, 0.2f, 0f), new Vector2(len * 0.6f, 0.1f), -148)
                                    .transform.localRotation = Quaternion.Euler(0f, 0f, angle - 45f);
                            x += Mathf.Cos(rad) * len;
                            y += Mathf.Sin(rad) * len;
                            angle += (float)(random.NextDouble() - 0.5) * 50f;
                        }
                    }
                    for (float x = startX - 5f; x < endX + 10f; x += 7f + (float)random.NextDouble() * 5f)
                    {
                        float size = 1.5f + (float)random.NextDouble() * 2f;
                        Vector3 p = new Vector3(x, FloorY + 4f + (float)random.NextDouble() * 4f, 0f);
                        SetupUtils.SpriteChild(environment, "Bulto", PlaceholderArt.Circle, style.blob, p, new Vector2(size, size * 1.3f), -150);
                        SetupUtils.SpriteChild(environment, "Brillo", PlaceholderArt.Circle, new Color(1f, 1f, 1f, 0.12f),
                            p + new Vector3(-size * 0.2f, size * 0.3f, 0f), new Vector2(size * 0.3f, size * 0.4f), -149);
                    }
                    break;

                case Decor.Villi: // intestino delgado: vellosidades (dedos) en el fondo
                    for (float x = startX - 5f; x < endX + 10f; x += 0.9f + (float)random.NextDouble() * 0.6f)
                    {
                        float h = 1.8f + (float)random.NextDouble() * 2.2f;
                        float w = 0.55f + (float)random.NextDouble() * 0.25f;
                        SetupUtils.SpriteChild(environment, "Vellosidad", PlaceholderArt.CircleBottom, style.blob,
                            new Vector3(x, FloorY - 0.3f, 0f), new Vector2(w, h), -150);
                        SetupUtils.SpriteChild(environment, "VenaVellosidad", PlaceholderArt.Square, style.detail,
                            new Vector3(x, FloorY - 0.3f, 0f), new Vector2(0.06f, h * 0.8f), -149);
                    }
                    break;

                case Decor.Folds: // intestino grueso: pliegues (haustras) en arco
                    for (float x = startX - 5f; x < endX + 10f; x += 4.5f)
                    {
                        SetupUtils.SpriteChild(environment, "Pliegue", PlaceholderArt.Circle, style.blob,
                            new Vector3(x, FloorY + 4.5f, 0f), new Vector2(4.2f, 9f), -151);
                        SetupUtils.SpriteChild(environment, "PliegueSombra", PlaceholderArt.Circle, Color.Lerp(style.wall, Color.black, 0.25f),
                            new Vector3(x + 0.35f, FloorY + 4.5f, 0f), new Vector2(3.5f, 8.2f), -150);
                        SetupUtils.SpriteChild(environment, "VenaPliegue", PlaceholderArt.Capsule, style.detail,
                            new Vector3(x - 1.6f, FloorY + 4.5f, 0f), new Vector2(5f, 0.12f), -149)
                            .transform.localRotation = Quaternion.Euler(0f, 0f, 80f);
                    }
                    break;

                case Decor.Neurons: // cerebro: lóbulos de fondo, red de neuronas (sanas azules, infectadas rojas) y chispas
                    {
                        Color[] lobes = { new Color(0.2f, 0.25f, 0.75f, 0.5f), new Color(0.6f, 0.18f, 0.5f, 0.5f), new Color(0.15f, 0.6f, 0.7f, 0.45f) };
                        for (float x = startX - 5f; x < endX + 10f; x += 7f + (float)random.NextDouble() * 4f)
                        {
                            Color lobe = lobes[random.Next(lobes.Length)];
                            for (int i = 0; i < 4; i++)
                            {
                                float size = 2.5f + (float)random.NextDouble() * 2f;
                                SetupUtils.SpriteChild(environment, "Lobulo", PlaceholderArt.Circle, lobe,
                                    new Vector3(x + (i - 1.5f) * 1.6f, FloorY + 6.5f + (float)(random.NextDouble() - 0.5) * 2f, 0f),
                                    new Vector2(size, size * 0.8f), -152);
                            }
                        }
                        for (float x = startX - 5f; x < endX + 10f; x += 3.5f + (float)random.NextDouble() * 3f)
                        {
                            bool infected = random.NextDouble() < 0.25;
                            Color c = infected ? new Color(1f, 0.25f, 0.3f, 0.7f) : style.detail;
                            Vector3 p = new Vector3(x, FloorY + 2.5f + (float)random.NextDouble() * 5f, 0f);
                            SetupUtils.SpriteChild(environment, "NeuronaFondo", PlaceholderArt.Circle, new Color(c.r, c.g, c.b, 0.2f), p, new Vector2(1.6f, 1.6f), -149);
                            SetupUtils.SpriteChild(environment, "NeuronaFondoSoma", PlaceholderArt.Circle, c, p, new Vector2(0.45f, 0.45f), -148);
                            for (int i = 0; i < 5; i++)
                            {
                                float a = (float)random.NextDouble() * 360f;
                                float len = 1f + (float)random.NextDouble() * 1.8f;
                                float rad = a * Mathf.Deg2Rad;
                                SetupUtils.SpriteChild(environment, "Dendrita", PlaceholderArt.Capsule, new Color(c.r, c.g, c.b, 0.55f),
                                    p + new Vector3(Mathf.Cos(rad) * len * 0.5f, Mathf.Sin(rad) * len * 0.5f, 0f), new Vector2(len, 0.07f), -149)
                                    .transform.localRotation = Quaternion.Euler(0f, 0f, a);
                            }
                        }
                        for (float x = startX - 5f; x < endX + 10f; x += 0.8f + (float)random.NextDouble() * 1.5f)
                            SetupUtils.SpriteChild(environment, "Sinapsis", PlaceholderArt.Circle, new Color(1f, 0.85f, 0.4f, 0.8f),
                                new Vector3(x, FloorY + 1f + (float)random.NextDouble() * 8f, 0f), new Vector2(0.1f, 0.1f), -147);
                        // Rayos de fondo (estáticos) para la tormenta eléctrica.
                        for (float x = startX + 10f; x < endX; x += 25f + (float)random.NextDouble() * 10f)
                        {
                            float y = FloorY + 10f;
                            float bx = x;
                            for (int i = 0; i < 6; i++)
                            {
                                float nx = bx + (float)(random.NextDouble() - 0.5) * 1.6f;
                                float ny = y - 1.3f;
                                Vector3 mid = new Vector3((bx + nx) * 0.5f, (y + ny) * 0.5f, 0f);
                                float ang = Mathf.Atan2(ny - y, nx - bx) * Mathf.Rad2Deg;
                                SetupUtils.SpriteChild(environment, "RayoFondo", PlaceholderArt.Capsule, new Color(0.85f, 0.6f, 1f, 0.35f),
                                    mid, new Vector2(1.45f, 0.12f), -151).transform.localRotation = Quaternion.Euler(0f, 0f, ang);
                                bx = nx; y = ny;
                            }
                        }
                    }
                    break;

                case Decor.None: // fondo de imagen: sin adornos
                    break;

                default:
                    for (float x = startX - 5f; x < endX + 10f; x += 4f + (float)random.NextDouble() * 3f)
                    {
                        float size = 1f + (float)random.NextDouble() * 2.5f;
                        float y = FloorY + 2.5f + (float)random.NextDouble() * 5f;
                        SetupUtils.SpriteChild(environment, "Blob", PlaceholderArt.Circle, style.blob,
                            new Vector3(x, y, 0f), new Vector2(size, size * (0.7f + (float)random.NextDouble() * 0.6f)), -150);
                    }
                    break;
            }

            // Con fondo de imagen no van las gotas del techo ni la bruma: el dibujo ya trae su ambiente.
            if (style.drip.a > 0f && backdrop == null)
            {
                // Gotas que escurren de la orilla del piso hacia los huecos y del borde de la pared.
                for (float x = startX - 5f; x < endX + 10f; x += 1.5f + (float)random.NextDouble() * 2f)
                {
                    float len = 0.3f + (float)random.NextDouble() * 0.6f;
                    SetupUtils.SpriteChild(environment, "Gota", PlaceholderArt.Capsule, style.drip,
                        new Vector3(x, FloorY + 9.6f - len * 0.5f, 0f), new Vector2(len, 0.16f), -147).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }

            if (style.glow.a > 0f)
            {
                // Bruma de color abajo y luz al final del nivel (como el arte de referencia).
                for (float x = startX - 5f; x < endX + 10f && backdrop == null; x += 6f)
                    SetupUtils.SpriteChild(environment, "Bruma", PlaceholderArt.Circle, new Color(style.glow.r, style.glow.g, style.glow.b, 0.08f),
                        new Vector3(x, FloorY + 1.2f, 0f), new Vector2(9f, 3.5f), -145);
                SetupUtils.SpriteChild(environment, "LuzSalida", PlaceholderArt.Circle, new Color(style.glow.r, style.glow.g, style.glow.b, 0.35f),
                    new Vector3(endX + 2f, FloorY + 3.5f, 0f), new Vector2(3.5f, 7f), -140);
                SetupUtils.SpriteChild(environment, "LuzSalidaCentro", PlaceholderArt.Circle, new Color(0.9f, 1f, 1f, 0.6f),
                    new Vector3(endX + 2f, FloorY + 3.5f, 0f), new Vector2(1.6f, 5f), -139);
            }

            if (style.cilia && tissueFloor == null) // con fondo pintado los cilios ya vienen dibujados
            {
                // Cilios a lo largo del piso.
                for (float x = startX - 10f; x < endX + 12f; x += 0.45f)
                {
                    float h = 0.25f + (float)random.NextDouble() * 0.25f;
                    SetupUtils.SpriteChild(environment, "Cilio", PlaceholderArt.Square, new Color(style.glow.r, style.glow.g, style.glow.b, 0.55f),
                        new Vector3(x, FloorY - 0.05f, 0f), new Vector2(0.08f, h), -95).transform.localRotation =
                        Quaternion.Euler(0f, 0f, (float)(random.NextDouble() - 0.5) * 30f);
                }
            }

            Vector2 spawn = new Vector2(startX + 3f, FloorY + 0.05f);
            currentSpawn = spawn;
            currentExitX = endX - 4f;
            signFont = AssetDatabase.LoadAssetAtPath<Font>($"{UIArtFolder}/Fuentes/TitanOne-Regular.ttf");

            var managerGo = new GameObject("LevelManager");
            var manager = managerGo.AddComponent<LevelManager>();
            // yMin = altura de caída al vacío.
            SetupUtils.SetRect(manager, "levelBounds", new Rect(startX, FloorY - 6f, length, 20f));
            SetupUtils.SetVector2(manager, "spawnPoint", spawn);
            SetupUtils.SetFloat(manager, "exitX", endX - 4f);
            SetupUtils.SetRef(manager, "playerPrefab", prefabs.Player);
            SetupUtils.SetString(manager, "introMessage", intro);

            // El jugador se coloca en la escena para poder verlo y probar; el LevelManager lo usa.
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Player.gameObject);
            player.transform.position = new Vector3(spawn.x, spawn.y, 0f);

            foreach (string face in new[] { "Normal", "Alerta", "Feliz" })   // caras de NODO como Sprite (Resources)
                PrefabSetup.ArtSprite($"{UIArtFolder}/Resources/Nodo/Nodo_{face}.png", 100f, new Vector2(0.5f, 0.5f));
            var hud = new GameObject("HUD", typeof(HUD)).GetComponent<HUD>();
            SetupUtils.SetRef(hud, "titleFont", signFont);                        // letra del globo de NODO y de los letreros
            SetupUtils.SetString(hud, "theme", signTheme.system ?? "Digestivo");  // color del globo de NODO
            new GameObject("PauseMenu", typeof(PauseMenu));
            new GameObject("Bestiario", typeof(BestiaryTracker));   // desbloquea fichas al ver / derrotar patógenos

            return manager;
        }

        private void FloorSegment(string name, float fromX, float toX)
        {
            float width = toX - fromX;
            if (width <= 0.01f) return;
            const float depth = 4f;

            var go = new GameObject(name);
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(fromX + width * 0.5f, FloorY, 0f);
            go.layer = PhysicsLayers.Ground;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, depth);
            box.offset = new Vector2(0f, -depth * 0.5f);

            if (tissueFloor != null)
            {
                TissueGround(go.transform, width, depth, Color.white);
                return;
            }
            SetupUtils.SpriteChild(go.transform, "Fill", PlaceholderArt.Square, currentStyle.floor,
                new Vector3(0f, -depth, 0f), new Vector2(width, depth), -100);
            SetupUtils.SpriteChild(go.transform, "Edge", PlaceholderArt.Square, currentStyle.floorEdge,
                new Vector3(0f, -0.2f, 0f), new Vector2(width, 0.2f), -99);
        }

        /// <summary>Piso de tejido: franja de arriba (pliegue con poros) + relleno, y paredes oscuras en las orillas.</summary>
        private void TissueGround(Transform parent, float width, float depth, Color tint, bool edgeShadows = true)
        {
            float strip = tissueFloor.bounds.size.y;
            TiledChild(parent, "Tejido", tissueFloor, Vector3.zero, new Vector2(width, strip), -99, tint);
            TiledChild(parent, "Relleno", tissueFill, new Vector3(0f, -strip + 0.01f, 0f), new Vector2(width, depth + 2f - strip), -100, tint);
            // Orillas (paredes de los huecos y lagos): sombra hacia adentro para que se vea el grosor.
            // (empieza abajo del borde de arriba, para no oscurecer donde se pisa)
            if (edgeShadows && softShadow != null)
            {
                Vector2 edgeSize = softShadow.bounds.size;
                var edgeColor = new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, 0.55f);
                for (int side = -1; side <= 1; side += 2)
                    SetupUtils.SpriteChild(parent, "SombraOrilla", softShadow, edgeColor,
                        new Vector3(side * width * 0.5f, -0.45f - depth * 0.5f, 0f), new Vector2(0.7f / edgeSize.x, depth / edgeSize.y), -98);
            }
        }

        /// <summary>Plataforma de un solo sentido: se atraviesa saltando desde abajo y se pisa desde arriba.</summary>
        private void Platform(float centerX, float topY, float width)
        {
            const float thickness = 0.35f;
            var go = new GameObject("Platform");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(centerX, topY, 0f);
            go.layer = PhysicsLayers.Ground;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, thickness);
            box.offset = new Vector2(0f, -thickness * 0.5f);
            box.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;

            bool tissue = tissuePlatform != null;
            if (tissue)
            {
                // Trozo de pliegue de tejido (las puntas no se repiten) y su sombra sobre el fondo.
                float tissueHeight = tissuePlatform.bounds.size.y;
                TiledChild(go.transform, "Tejido", tissuePlatform, Vector3.zero, new Vector2(width + 0.3f, tissueHeight), -90, Color.white);
                if (softShadow != null)
                {
                    Vector2 shadowSize = softShadow.bounds.size;
                    SetupUtils.SpriteChild(go.transform, "Sombra", softShadow, new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, 0.5f),
                        new Vector3(0.15f, -tissueHeight * 0.75f, 0f), new Vector2((width * 1.25f) / shadowSize.x, 1.2f / shadowSize.y), -160);
                }
            }
            else
            {
                SetupUtils.SpriteChild(go.transform, "Sprite", PlaceholderArt.Square, Color.Lerp(currentStyle.floor, Color.black, 0.1f),
                    new Vector3(0f, -thickness, 0f), new Vector2(width, thickness), -90);
                SetupUtils.SpriteChild(go.transform, "Edge", PlaceholderArt.Square, currentStyle.floorEdge,
                    new Vector3(0f, -0.12f, 0f), new Vector2(width, 0.12f), -89);
            }

            if (currentStyle.platformStems && !tissue) // con tejido nuevo la plataforma flota (sin tallo de relleno)
            {
                // Tejido nervioso tipo hongo: borde redondeado y tallo hasta el piso.
                float stemHeight = topY - FloorY + 3f;
                Color stem = Color.Lerp(currentStyle.floor, currentStyle.wall, 0.4f);
                SetupUtils.SpriteChild(go.transform, "Tallo", PlaceholderArt.Square, stem,
                    new Vector3(0f, -stemHeight, 0f), new Vector2(Mathf.Min(0.8f, width * 0.35f), stemHeight - thickness), -92);
                SetupUtils.SpriteChild(go.transform, "TalloBorde", PlaceholderArt.Square, new Color(currentStyle.floorEdge.r, currentStyle.floorEdge.g, currentStyle.floorEdge.b, 0.35f),
                    new Vector3(Mathf.Min(0.8f, width * 0.35f) * 0.4f, -stemHeight, 0f), new Vector2(0.06f, stemHeight - thickness), -91);
                SetupUtils.SpriteChild(go.transform, "Tapa", PlaceholderArt.Circle, Color.Lerp(currentStyle.floor, Color.white, 0.08f),
                    new Vector3(0f, -0.2f, 0f), new Vector2(width + 0.3f, 0.55f), -91);
            }

            if (currentStyle.drip.a > 0f && !tissue) // el tejido ya trae sus gotas de moco dibujadas
            {
                // Plataforma de carne tipo hongo: borde redondeado y gotas colgando.
                if (!tissue)
                    SetupUtils.SpriteChild(go.transform, "Tapa", PlaceholderArt.Circle, Color.Lerp(currentStyle.floor, Color.white, 0.12f),
                        new Vector3(0f, -0.18f, 0f), new Vector2(width + 0.4f, 0.5f), -91);
                // Con tejido: menos gotas y desde más abajo (el tejido cuelga más que el bloque).
                float dripTop = tissue ? 0.42f : thickness;
                int drips = Mathf.Max(tissue ? 1 : 2, Mathf.RoundToInt(width / (tissue ? 1.4f : 0.8f)));
                for (int i = 0; i < drips; i++)
                {
                    float x = -width * 0.5f + (i + 0.5f) * width / drips + ((i * 37) % 10 - 5) * 0.02f;
                    float len = 0.25f + (i % 3) * 0.18f;
                    SetupUtils.SpriteChild(go.transform, "Gota" + i, PlaceholderArt.Capsule, currentStyle.drip,
                        new Vector3(x, -dripTop - len * 0.5f + 0.05f, 0f), new Vector2(len, 0.14f), -88)
                        .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }
        }

        /// <summary>Líquido en el fondo de un hueco (solo se ve; caer sigue costando vida).</summary>
        private void PitLiquid(float fromX, float width)
        {
            Color c = currentStyle.pitLiquid;
            SetupUtils.SpriteChild(environment, "Liquido", PlaceholderArt.Square, new Color(c.r * 0.6f, c.g * 0.6f, c.b * 0.6f, 0.95f),
                new Vector3(fromX + width * 0.5f, FloorY - 4f, 0f), new Vector2(width, 3.1f), -98);
            SetupUtils.SpriteChild(environment, "LiquidoSuperficie", PlaceholderArt.Square, c,
                new Vector3(fromX + width * 0.5f, FloorY - 1.05f, 0f), new Vector2(width, 0.18f), -97);
            SetupUtils.SpriteChild(environment, "LiquidoBrillo", PlaceholderArt.Circle, new Color(c.r, c.g, c.b, 0.25f),
                new Vector3(fromX + width * 0.5f, FloorY - 0.9f, 0f), new Vector2(width * 1.2f, 1.2f), -96);
            for (int i = 0; i < 3; i++)
                SetupUtils.SpriteChild(environment, "Burbuja", PlaceholderArt.Circle, new Color(1f, 1f, 1f, 0.35f),
                    new Vector3(fromX + width * (0.25f + i * 0.25f), FloorY - 0.7f + (i % 2) * 0.35f, 0f), new Vector2(0.22f, 0.22f), -95);
        }

        private ArenaZone Arena(string name, float triggerX, float width, string startMessage, params Wave[] waves)
        {
            var arena = new GameObject(name).AddComponent<ArenaZone>();
            arena.transform.position = new Vector3(triggerX, FloorY, 0f);
            arena.floorY = FloorY;
            arena.triggerX = triggerX;
            arena.leftBound = triggerX - 3f;
            arena.rightBound = triggerX - 3f + width;
            arena.waves = new List<Wave>(waves);
            arena.effectivenessTable = data.Table;
            arena.startMessage = startMessage;
            BossSignIfNeeded(triggerX, waves);
            return arena;
        }

        private static Wave W(float delay, params SpawnEntry[] entries)
        {
            return new Wave { delayBefore = delay, entries = new List<SpawnEntry>(entries) };
        }

        private static SpawnEntry E(EnemyHealth prefab, EnemyData enemyData, int count, SpawnSide side = SpawnSide.Right)
        {
            return new SpawnEntry { prefab = prefab, data = enemyData, count = count, side = side };
        }

        private void Acid(float x)
        {
            var acid = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Acid.gameObject);
            acid.transform.position = new Vector3(x, FloorY, 0f);
        }

        // ---------- Niveles del Sistema Digestivo ----------

        /// <summary>
        /// Bloqueo tratable de los bronquiolos (GDD 5.12): mucosidad (Mucolítico) o inflamación (Antiinflamatorio).
        /// Es una pared más alta que cualquier salto: hay que disolverla para seguir.
        /// </summary>
        private void Block(float x, bool mucus)
        {
            EnemyHealth prefab = mucus ? prefabs.Mucosidad : prefabs.Inflamacion;
            var block = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject);
            block.transform.position = new Vector3(x, FloorY, 0f);
        }

        /// <summary>Arena que termina en un bloqueo: los enemigos de la derecha aparecen antes de la pared.</summary>
        private ArenaZone BlockedArena(string name, float triggerX, float width, string startMessage, bool mucus, params Wave[] waves)
        {
            ArenaZone arena = Arena(name, triggerX, width, startMessage, waves);
            arena.rightSpawnInset = 2.2f;
            Block(arena.rightBound - 1.2f, mucus);
            return arena;
        }

        /// <summary>Lago hundido: piso más bajo (sólido) con ácido hasta casi el borde.</summary>
        private const string FoodFolder = "Assets/_Project/Art/Comida";
        /// <summary>Lagos de ácido del nivel actual (para poner comida flotando en ellos).</summary>
        private readonly List<AcidLake> currentLakes = new List<AcidLake>();

        private AcidLake LakeAt(float x)
        {
            foreach (AcidLake lake in currentLakes)
            {
                if (lake == null) continue;
                if (Mathf.Abs(x - lake.transform.position.x) <= lake.Width * 0.5f) return lake;
            }
            return null;
        }

        /// <summary>
        /// Comida a medio digerir que flota en el ácido (Art/Comida/Comida_&lt;nombre&gt;.png) y sirve de plataforma.
        /// 'topFraction' = a qué altura del dibujo está la parte que se pisa (0..1 desde abajo).
        /// Descansa en el piso o el fondo del lago donde está, y sube si sube el ácido (marea).
        /// </summary>
        private void Food(string name, float x, float topFraction,
            FloatingFood.FoodKind kind = FloatingFood.FoodKind.Normal, bool anisakis = false)
        {
            Sprite sprite = LoadTissue($"Comida_{name}", new Vector2(0.5f, 0f), Vector4.zero, false, FoodFolder);
            if (sprite == null) return;
            AcidLake lake = LakeAt(x);
            float restY = lake != null ? lake.transform.position.y : FloorY;
            Vector2 size = sprite.bounds.size;

            var root = new GameObject($"Comida_{name}");
            root.transform.SetParent(environment, false);
            root.transform.position = new Vector3(x, restY, 0f);

            var art = new GameObject("Dibujo");
            art.transform.SetParent(root.transform, false);
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -5; // detrás de los personajes y del ácido (así se ve hundida)
            sr.color = new Color(0.92f, 0.86f, 0.86f, 1f); // un poco apagada: ya va a medio digerir

            const float thickness = 0.3f;
            var floor = new GameObject("Piso");
            floor.transform.SetParent(root.transform, false);
            floor.transform.localPosition = new Vector3(0f, size.y * topFraction, 0f);
            floor.layer = PhysicsLayers.Ground;
            var box = floor.AddComponent<BoxCollider2D>();
            box.size = new Vector2(size.x * 0.8f, thickness);
            box.offset = new Vector2(0f, -thickness * 0.5f);
            box.usedByEffector = true;
            var effector = floor.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;

            root.AddComponent<FloatingFood>().Configure(lake, restY, size.y, size.y * topFraction, size.x * 0.8f, art.transform, kind);
            if (anisakis) root.AddComponent<AnisakisNest>().Configure(prefabs.Anisakis, data.Anisakis, art.transform);
        }

        // ---------- Elementos del Estómago (rediseño) ----------

        /// <summary>Chorro de ácido. 'groundY' = donde nace (piso o fondo del lago). 'offset' desfasa chorros vecinos.</summary>
        private AcidGeyser Geyser(float x, float groundY, float height = 4.5f, float interval = 3.5f, float offset = 0f)
        {
            var go = new GameObject("ChorroAcido");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(x, groundY, 0f);
            var geyser = go.AddComponent<AcidGeyser>();
            geyser.Configure(height, interval, offset);
            return geyser;
        }

        // ---------- Elementos del Intestino delgado (rediseño) ----------

        private const string SmallIntestineArt = "Assets/_Project/Art/Delgado";

        /// <summary>Chorro de bilis (sale por la ampolla de Vater): el mismo chorro del estómago, pero amarillo.</summary>
        private AcidGeyser BileGeyser(float x, float groundY, float height = 4.5f, float interval = 3.2f, float offset = 0f)
        {
            AcidGeyser g = Geyser(x, groundY, height, interval, offset);
            g.gameObject.name = "ChorroBilis";
            g.SetColors(new Color(0.92f, 0.8f, 0.2f, 0.92f), new Color(0.3f, 0.22f, 0.04f, 1f), new Color(1f, 0.96f, 0.62f, 1f));
            return g;
        }

        /// <summary>Gota de grasa flotando (plataforma). 'topY' = donde se pisa. Si hay un chorro de bilis abajo, la rompe.</summary>
        private void FatDrop(float x, float topY, float width = 1.8f, AcidGeyser geyser = null)
        {
            var go = new GameObject("GotaGrasa");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(x, topY, 0f);
            var drop = go.AddComponent<FatDroplet>();
            drop.Configure(width, geyser);
            // Dibujos (todas las gotas miden 1.8 de ancho); pivote cerca de arriba, donde se pisa.
            drop.SetArt(PrefabSetup.ArtByWidth($"{SmallIntestineArt}/GotaGrasa.png", 1.8f, new Vector2(0.5f, 0.9f)),
                PrefabSetup.ArtByWidth($"{SmallIntestineArt}/GotaGrasa_Rota.png", 1.8f, new Vector2(0.5f, 0.75f)));
        }

        /// <summary>Vellosidad que se mece (plataforma en la punta). 'baseY' = piso o fondo del lago. El rotavirus la aplana.</summary>
        private void Villus(float x, float baseY, float height)
        {
            var go = new GameObject("Vellosidad");
            go.transform.SetParent(environment, false);
            // Un poco hundida en el piso para que se vea plantada (no parada encima).
            go.transform.position = new Vector3(x, baseY - 0.2f, 0f);
            var villus = go.AddComponent<SwayingVillus>();
            villus.Configure(height + 0.2f, x * 0.77f, data.Rotavirus);
            villus.SetArt(PrefabSetup.ArtByWidth($"{SmallIntestineArt}/Vellosidad.png", 2.1f, new Vector2(0.5f, 0f)),
                PrefabSetup.ArtByWidth($"{SmallIntestineArt}/Vellosidad_Danada.png", 2.1f, new Vector2(0.5f, 0f)));
        }

        /// <summary>
        /// Emboscada. Con 'hidden' se ve una Giardia agarrada (en 'y') que se te avienta cuando pasas cerca;
        /// sin él, los enemigos aparecen 'ahead' unidades adelante del jugador (a la altura 'y').
        /// </summary>
        private void Ambush(float x, float y, EnemyHealth prefab, EnemyData enemyData, int count, float range, float ahead,
            Vector2 velocity, bool hidden, string message = null)
        {
            var go = new GameObject("Emboscada_" + (enemyData != null ? enemyData.displayName : "?"));
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(x, y, 0f);
            Transform figure = null;
            if (hidden && enemyData != null)
            {
                GameObject f = SetupUtils.Child(go.transform, "Escondida", Vector3.zero);
                SetupUtils.SpriteChild(f.transform, "Contorno", PlaceholderArt.Circle, new Color(0.3f, 0.22f, 0.05f), Vector3.zero, new Vector2(0.78f, 0.9f), 3);
                SetupUtils.SpriteChild(f.transform, "Cuerpo", PlaceholderArt.Circle, enemyData.tint, Vector3.zero, new Vector2(0.68f, 0.8f), 4);
                SetupUtils.SpriteChild(f.transform, "Ojo", PlaceholderArt.Circle, Color.black, new Vector3(-0.12f, 0.12f, 0f), new Vector2(0.1f, 0.1f), 5);
                SetupUtils.SpriteChild(f.transform, "Ojo", PlaceholderArt.Circle, Color.black, new Vector3(0.12f, 0.12f, 0f), new Vector2(0.1f, 0.1f), 5);
                figure = f.transform;
            }
            go.AddComponent<AmbushSpawner>().Configure(prefab, enemyData, count, range, ahead, velocity, figure, message);
        }

        /// <summary>Suero oral: cura (sobre azul con gota).</summary>
        private void OralSerum(float x, float y, float heal = 35f)
        {
            var go = new GameObject("SueroOral");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(x, y, 0f);
            GameObject art = SetupUtils.Child(go.transform, "Dibujo", Vector3.zero);
            Sprite packet = PrefabSetup.ArtByWidth($"{SmallIntestineArt}/SueroOral.png", 0.85f);
            if (packet != null)
                SetupUtils.SpriteChild(art.transform, "Sobre", packet, Color.white, Vector3.zero, Vector2.one, 21);
            else
            {
                SetupUtils.SpriteChild(art.transform, "Contorno", PlaceholderArt.SquareCentered, new Color(0.08f, 0.15f, 0.3f), Vector3.zero, new Vector2(0.72f, 0.92f), 20);
                SetupUtils.SpriteChild(art.transform, "Sobre", PlaceholderArt.SquareCentered, new Color(0.45f, 0.75f, 1f), Vector3.zero, new Vector2(0.62f, 0.82f), 21);
                SetupUtils.SpriteChild(art.transform, "Gota", PlaceholderArt.Circle, Color.white, new Vector3(0f, -0.05f, 0f), new Vector2(0.3f, 0.36f), 22);
            }
            SetupUtils.SpriteChild(go.transform, "Aura", PlaceholderArt.Circle, new Color(0.5f, 0.85f, 1f, 0.25f), Vector3.zero, new Vector2(1.5f, 1.5f), 19);
            go.AddComponent<HealPickup>().Configure(art.transform, heal);
        }

        /// <summary>Placa de Peyer: punto de guardado (y cura un poco).</summary>
        private void Peyer(float x)
        {
            var go = new GameObject("PlacaDePeyer");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(x, FloorY, 0f);
            go.AddComponent<PeyerPatch>().SetArt(
                PrefabSetup.ArtByWidth($"{SmallIntestineArt}/Peyer_Apagada.png", 2.8f, new Vector2(0.5f, 0.02f)),
                PrefabSetup.ArtByWidth($"{SmallIntestineArt}/Peyer_Encendida.png", 2.8f, new Vector2(0.5f, 0.02f)));
        }

        /// <summary>Persecución de la lombriz: empieza al pasar 'startX'; sale en cada punto; el último es la válvula ileocecal.</summary>
        private void WormChaseAt(float startX, params float[] points)
        {
            var worm = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.LombrizHuye.gameObject);
            worm.transform.SetParent(environment, true);
            worm.transform.position = new Vector3(points.Length > 0 ? points[0] : startX, FloorY, 0f);
            var chase = worm.GetComponent<WormChase>();
            chase.Configure(startX, points);
            // Es una instancia de prefab: hay que registrar el cambio para que se guarde con la escena.
            PrefabUtility.RecordPrefabInstancePropertyModifications(chase);
        }

        /// <summary>Columna de tejido de adorno (sin colisión), pegada al piso: la pared donde se agarra la tenia.</summary>
        private void TissueColumn(float x, float topY, float width)
        {
            var go = new GameObject("ColumnaTejido");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(x, topY, 0f);
            if (tissueFloor != null) TissueGround(go.transform, width, topY - FloorY, new Color(0.82f, 0.74f, 0.74f, 1f), true);
            else SetupUtils.SpriteChild(go.transform, "Columna", PlaceholderArt.Square, new Color(0.6f, 0.25f, 0.3f), new Vector3(0f, FloorY - topY, 0f), new Vector2(width, topY - FloorY), -99);
        }

        /// <summary>Válvula ileocecal (la puerta al intestino grueso): solo de adorno, al final del nivel.</summary>
        private void IleocecalValve(float x)
        {
            var go = new GameObject("ValvulaIleocecal");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(x, FloorY, 0f);
            // Dibujo: abierta (la lombriz se escapa por ahí). Mide 5 de ancho.
            Sprite valve = PrefabSetup.ArtByWidth($"{SmallIntestineArt}/Valvula_Abierta.png", 5f, new Vector2(0.5f, 0.03f));
            if (valve != null)
            {
                SetupUtils.SpriteChild(go.transform, "Dibujo", valve, Color.white, Vector3.zero, Vector2.one, -18);
                return;
            }
            SetupUtils.SpriteChild(go.transform, "Contorno", PlaceholderArt.Circle, new Color(0.3f, 0.07f, 0.1f), new Vector3(0f, 2.3f, 0f), new Vector2(3.4f, 4.6f), -20);
            SetupUtils.SpriteChild(go.transform, "Labios", PlaceholderArt.Circle, new Color(0.95f, 0.55f, 0.5f), new Vector3(0f, 2.3f, 0f), new Vector2(3.2f, 4.4f), -19);
            SetupUtils.SpriteChild(go.transform, "Paso", PlaceholderArt.Circle, new Color(0.55f, 0.32f, 0.38f), new Vector3(0f, 2.3f, 0f), new Vector2(1.4f, 2.8f), -18);
        }

        /// <summary>Burbujas de gas que suben desde el ácido y te llevan hacia arriba.</summary>
        private void GasVent(float x, float height, float interval = 2.6f)
        {
            var go = new GameObject("BurbujasGas");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(x, FloorY - 0.35f, 0f);
            go.AddComponent<GasBubbleVent>().Configure(interval, height);
        }

        private const string StomachArtFolder = "Assets/_Project/Art/Estomago";

        /// <summary>Cápsula de omeprazol (Art/Estomago/Omeprazol.png; sin PNG, cápsula de figuras) que baja el ácido unos segundos.</summary>
        private void Omeprazol(float x, float y)
        {
            var go = new GameObject("Omeprazol");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(x, y, 0f);
            GameObject art = SetupUtils.Child(go.transform, "Dibujo", Vector3.zero);
            Sprite pill = PrefabSetup.ArtSprite($"{StomachArtFolder}/Omeprazol.png", 200f, new Vector2(0.5f, 0.5f));
            if (pill != null)
            {
                SetupUtils.SpriteChild(art.transform, "Capsula", pill, Color.white, Vector3.zero, Vector2.one, 21);
            }
            else
            {
                SetupUtils.SpriteChild(art.transform, "Contorno", PlaceholderArt.Capsule, new Color(0.24f, 0.04f, 0.15f), Vector3.zero, new Vector2(0.95f, 0.5f), 20);
                SetupUtils.SpriteChild(art.transform, "Mitad1", PlaceholderArt.Capsule, new Color(0.56f, 0.36f, 0.85f), new Vector3(-0.18f, 0f, 0f), new Vector2(0.5f, 0.4f), 21);
                SetupUtils.SpriteChild(art.transform, "Mitad2", PlaceholderArt.Capsule, new Color(0.98f, 0.96f, 1f), new Vector3(0.18f, 0f, 0f), new Vector2(0.5f, 0.4f), 21);
                SetupUtils.SpriteChild(art.transform, "Brillo", PlaceholderArt.Circle, new Color(1f, 1f, 1f, 0.8f), new Vector3(-0.2f, 0.08f, 0f), new Vector2(0.2f, 0.08f), 22);
            }
            SetupUtils.SpriteChild(go.transform, "Aura", PlaceholderArt.Circle, new Color(0.7f, 0.55f, 1f, 0.25f), Vector3.zero, new Vector2(1.5f, 1.5f), 19);
            go.AddComponent<AntacidPickup>().Configure(art.transform);
        }

        /// <summary>La contracción que te persigue de 'fromX' a 'toX'.</summary>
        private void Peristalsis(float fromX, float toX, float speed = 4.1f)
        {
            var go = new GameObject("Contraccion");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(fromX, FloorY, 0f);
            var wave = go.AddComponent<PeristalsisWave>();
            wave.Configure(fromX, toX, speed);
            // Dibujo de la ola de músculo: 8 de alto, con el pivote en el frente (la cara) y en el piso.
            const float waveHeight = 8f;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{StomachArtFolder}/Contraccion.png");
            Sprite waveArt = tex != null ? PrefabSetup.ArtSprite($"{StomachArtFolder}/Contraccion.png", tex.height / waveHeight, new Vector2(0.96f, 0.02f)) : null;
            if (waveArt != null)
            {
                GameObject visual = SetupUtils.Child(go.transform, "Ola", Vector3.zero);
                SetupUtils.SpriteChild(visual.transform, "Dibujo", waveArt, Color.white, Vector3.zero, Vector2.one, 25);
                SetupUtils.SetRef(wave, "visual", visual.transform);
            }
        }

        /// <summary>El píloro (salida del estómago): cerrado mientras la arena del jefe no esté limpia.</summary>
        private void Pylorus(float x, ArenaZone bossArena)
        {
            var go = new GameObject("Piloro");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(x, FloorY, 0f);
            GameObject closed = SetupUtils.Child(go.transform, "Cerrado", Vector3.zero);
            GameObject open = SetupUtils.Child(go.transform, "Abierto", Vector3.zero);
            // Dibujos Art/Estomago/Piloro_Cerrado y Piloro_Abierto (4.8 de alto, pivote abajo al centro).
            const float pylorusHeight = 4.8f;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{StomachArtFolder}/Piloro_Cerrado.png");
            float ppu = tex != null ? tex.height / pylorusHeight : 100f;
            Sprite closedArt = tex != null ? PrefabSetup.ArtSprite($"{StomachArtFolder}/Piloro_Cerrado.png", ppu, new Vector2(0.5f, 0f)) : null;
            Sprite openArt = tex != null ? PrefabSetup.ArtSprite($"{StomachArtFolder}/Piloro_Abierto.png", ppu, new Vector2(0.5f, 0f)) : null;
            foreach (GameObject state in new[] { closed, open })
            {
                bool isOpen = state == open;
                Sprite art = isOpen ? openArt : closedArt;
                if (art != null)
                {
                    SetupUtils.SpriteChild(state.transform, "Dibujo", art, Color.white, new Vector3(0f, -0.2f, 0f), Vector2.one, -18);
                }
                else
                {
                    SetupUtils.SpriteChild(state.transform, "Contorno", PlaceholderArt.Circle, new Color(0.24f, 0.04f, 0.15f), new Vector3(0f, 2.2f, 0f), new Vector2(3.6f, 4.6f), -20);
                    SetupUtils.SpriteChild(state.transform, "Anillo", PlaceholderArt.Circle, new Color(0.88f, 0.38f, 0.5f), new Vector3(0f, 2.2f, 0f), new Vector2(3.4f, 4.4f), -19);
                    SetupUtils.SpriteChild(state.transform, "Centro", PlaceholderArt.Circle,
                        isOpen ? new Color(1f, 0.78f, 0.6f) : new Color(0.66f, 0.2f, 0.33f), new Vector3(0f, 2.2f, 0f),
                        isOpen ? new Vector2(2.2f, 3.2f) : new Vector2(1f, 1.2f), -18);
                }
                state.SetActive(!isOpen);
            }
            go.AddComponent<PyloricDoor>().Configure(bossArena, closed, open);
        }

        private void Basin(float fromX, float width)
        {
            BasinFloor(fromX, width, BasinDepth);
            currentLakes.Add(Lake(fromX, fromX + width, BasinDepth - 0.15f, FloorY - BasinDepth));
        }

        /// <summary>Piso hundido (fondo de un lago o de la alberca de marea).</summary>
        private void BasinFloor(float fromX, float width, float depth)
        {
            var go = new GameObject("FondoLago");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(fromX + width * 0.5f, FloorY - depth, 0f);
            go.layer = PhysicsLayers.Ground;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, 4f);
            box.offset = new Vector2(0f, -2f);
            if (tissueFloor != null) TissueGround(go.transform, width, 4f, new Color(0.7f, 0.7f, 0.7f, 1f), false);
            else
                SetupUtils.SpriteChild(go.transform, "Fill", PlaceholderArt.Square, Color.Lerp(currentStyle.floor, Color.black, 0.35f),
                    new Vector3(0f, -4f, 0f), new Vector2(width, 4f), -100);
        }

        /// <summary>Hueco lleno de ácido casi hasta el borde. No daña aparte: caer al hueco ya cuesta vida.</summary>
        private void PitAcid(float fromX, float width)
        {
            const float depth = 3f;
            Lake(fromX, fromX + width, depth - 0.18f, FloorY - depth).SetDamage(0f);
        }

        /// <summary>Lago de ácido de 'fromX' a 'toX' (animado por código, ver AcidLake). 'bottomY' = fondo del lago.</summary>
        private AcidLake Lake(float fromX, float toX, float level = 0.22f, float bottomY = FloorY)
        {
            var go = new GameObject("LagoAcido");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3((fromX + toX) * 0.5f, bottomY, 0f);
            var lake = go.AddComponent<AcidLake>();
            lake.Configure(toX - fromX, level, false, 0f);
            if (currentStyle.liquidSurface.a > 0f)
                lake.SetColors(currentStyle.liquidDeep, currentStyle.liquidSurface, currentStyle.liquidOutline, currentStyle.liquidCrest);
            return lake;
        }

        /// <summary>Marea de ácido: seco, aviso (retumbo), sube hasta 'high', baja. Pon plataformas arriba de 'high'.</summary>
        private AcidLake Tide(float fromX, float toX, float high)
        {
            AcidLake lake = Lake(fromX, toX, 0f);
            lake.name = "MareaAcido";
            lake.Configure(toX - fromX, 0f, true, high);
            currentLakes.Add(lake);
            return lake;
        }

        /// <summary>Capa de moco sobre el piso (zona segura: el moco protege al estómago de su propio ácido).</summary>
        private void MucusLayer(float fromX, float toX)
        {
            // Solo sobre el piso seco: se parte en tramos alrededor de los huecos y lagos.
            float start = fromX;
            foreach (Vector2 cut in currentCuts)
            {
                if (cut.x + cut.y <= start || cut.x >= toX) continue;
                if (cut.x > start) MucusStrip(start, cut.x);
                start = Mathf.Max(start, cut.x + cut.y);
            }
            if (start < toX) MucusStrip(start, toX);
        }

        private void MucusStrip(float fromX, float toX)
        {
            if (toX - fromX < 0.2f) return;
            // Capa brillante y húmeda, del mismo tono que la piel pero más clara (no gris).
            Color tone = Color.Lerp(currentStyle.floorEdge, Color.white, 0.35f);
            SetupUtils.SpriteChild(environment, "Moco", PlaceholderArt.Square, new Color(tone.r, tone.g, tone.b, 0.45f),
                new Vector3((fromX + toX) * 0.5f, FloorY + 0.03f, 0f), new Vector2(toX - fromX, 0.08f), -98);
            var random = new System.Random((int)(fromX * 10f));
            for (float x = fromX + 0.5f; x < toX - 0.5f; x += 1.2f + (float)random.NextDouble() * 1.8f)
            {
                float w = 0.4f + (float)random.NextDouble() * 0.6f;
                SetupUtils.SpriteChild(environment, "MocoBrillo", PlaceholderArt.Capsule, new Color(1f, 1f, 1f, 0.3f),
                    new Vector3(x, FloorY + 0.05f, 0f), new Vector2(w, 0.06f), -97);
            }
        }

        /// <summary>Pasaje secreto; 'groundY' = piso o plataforma donde está.</summary>
        private void Tooth(float x, float groundY)
        {
            var tooth = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Tooth.gameObject);
            tooth.transform.position = new Vector3(x, groundY + 1.1f, 0f);
        }

        private void Secret(float x, float groundY)
        {
            var secret = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Secret.gameObject);
            secret.transform.position = new Vector3(x, groundY + 0.7f, 0f);
        }

        private void BuildEstomago()
        {
            BuildLevelBase(160f, new LevelStyle
            {
                // Estómago: tejido carmesí, plataformas de carne rosada que escurren ácido verde brillante.
                background = new Color(0.1f, 0.02f, 0.04f),
                wall = new Color(0.32f, 0.06f, 0.1f),
                floor = new Color(0.6f, 0.2f, 0.28f),
                floorEdge = new Color(0.95f, 0.45f, 0.55f),
                blob = new Color(0.55f, 0.15f, 0.22f, 0.6f),
                decor = Decor.Veins,
                backdrop = "Fondo_Estomago", // pintura estilo caricatura; si se borra el PNG vuelven las venas de placeholder
                tissue = "Estomago",         // piso y plataformas de tejido estilo caricatura (Art/Tejidos)
                // Lagos de ácido hundidos (x = inicio, ancho). Se cruzan sobre la comida que flota.
                basins = new[]
                {
                    new Vector2(21f, 15f),    // 2) lago A: comida que rebota, rueda y se hunde + chorros
                    new Vector2(39f, 12f),    //    lago B: con omeprazol se cruza caminando por el fondo
                    new Vector2(57f, 10f),    // 3) lago de la arena de la colonia (ureasa = burbuja segura)
                    new Vector2(72f, 12f),    // 4) lago de las burbujas de gas
                    new Vector2(112f, 2.5f),  // 6) charcos de la huida (se brincan)
                    new Vector2(118f, 2.5f)
                },
                // 5) Alberca de marea de 86 a 106, siempre con ácido; sube hasta 1.5 sobre el piso.
                tidePool = new Vector4(86f, 20f, 1.2f, 1.5f),
                detail = new Color(0.15f, 0.55f, 0.6f, 0.7f),
                pitLiquid = new Color(0.5f, 1f, 0.15f, 1f),
                drip = new Color(0.55f, 1f, 0.2f, 0.9f),
                glow = new Color(0.45f, 1f, 0.2f, 1f)
            }, "NODO: Estás en el estómago. Muévete con {mover}, salta con {saltar} y dispara con {disparar}. Mantén {fijar} para quedarte quieto y apuntar a donde quieras; {dash} hace un dash.");

            // Recorrido (cada parte agrega algo nuevo):
            //  1) Entrada seca (tutorial).
            //  2) Lagos con comida que rebota/rueda/se hunde, chorros de ácido y omeprazol.
            //  3) Arena de la colonia madre y la H. pylori con ureasa.
            //  4) Subida con burbujas de gas y el sushi con anisakis.
            //  5) Alberca de marea.
            //  6) Huida de la contracción.
            //  7) Jefe Bezoar tapando el píloro.
            MucusLayer(19f, 150f);

            // ----- 1) Entrada seca -----
            Platform(0f, 2.2f, 3f);
            Platform(4.5f, 3.6f, 2.5f);
            Arena("Arena_1_Tutorial", 8f, 14f,
                "H. pylori: una bacteria que se entierra en el moco y sale debajo de ti (enterrada no le haces daño). Cambia de cápsula con {medicamento} hasta encontrar la que funciona.",
                W(0.8f, E(prefabs.HPylori, data.HPylori, 1)),
                W(0.8f, E(prefabs.HPylori, data.HPylori, 2)));

            // ----- 2) Lagos con comida "viva" y chorros de ácido -----
            Tip(18.6f, "¡ÁCIDO!", "Lago de ÁCIDO: no lo toques. Brinca en la comida que flota: la DONA rebota, la SALCHICHA rueda y la HAMBURGUESA se hunde si te quedas encima.", FloorY, Signpost.Kind.Warning);
            Sign(Signpost.Kind.Info, 36.7f, FloorY, "OMEPRAZOL");
            Food("Dona", 23.4f, 0.95f, FloatingFood.FoodKind.Bouncy);          // rebota: trampolín
            Food("Salchicha", 28.2f, 0.79f, FloatingFood.FoodKind.Rolling);    // rueda hacia donde caminas
            Food("Hamburguesa", 33.2f, 0.95f, FloatingFood.FoodKind.Sinking);  // se hunde si te quedas
            Geyser(25.8f, FloorY - BasinDepth, 4.5f, 3.4f, 0f);
            Geyser(30.7f, FloorY - BasinDepth, 4.5f, 3.4f, 1.7f);
            Omeprazol(37.5f, 1.3f);                                             // banco entre los lagos
            Food("Manzana", 42f, 0.89f);
            Food("Dona", 47.3f, 0.95f, FloatingFood.FoodKind.Bouncy);
            Geyser(44.6f, FloorY - BasinDepth, 4.5f, 3f, 0.5f);
            Geyser(49.8f, FloorY - BasinDepth, 4.5f, 3f, 2f);
            // Easter egg: muy alto, solo se alcanza rebotando en la dona + doble salto.
            Platform(45.5f, 5.4f, 2.5f);
            Tooth(45.8f, 5.4f);

            // ----- 3) Arena de la colonia -----
            Arena("Arena_2_Colonia", 56f, 18f,
                "NODO: ¡Una colonia de H. pylori! Destruye a la MADRE o no dejará de soltar crías. La H. pylori turquesa neutraliza el ácido: cerca de ella no te quema. Escudo: {escudo}; justo antes del golpe = PARRY.",
                W(0.6f, E(prefabs.HPyloriUreasa, data.HPyloriUreasa, 1), E(prefabs.HPylori, data.HPylori, 2, SpawnSide.Left)),
                W(1.0f, E(prefabs.HPyloriMadre, data.HPyloriMadre, 1), E(prefabs.HPyloriUreasa, data.HPyloriUreasa, 1, SpawnSide.Left)));
            Food("Hamburguesa", 59.6f, 0.95f, FloatingFood.FoodKind.Sinking);
            Food("Salchicha", 64.2f, 0.79f, FloatingFood.FoodKind.Rolling);
            Platform(62f, 3.4f, 3f);

            // ----- 4) Burbujas de gas y el sushi -----
            Tip(69.6f, "GAS", "Burbujas de GAS: es el aire que tragas al comer (por eso eructas). Párate encima y te suben; arriba revientan, así que brinca a tiempo.");
            GasVent(74.6f, 6.6f);
            GasVent(80.2f, 7.4f, 3f);
            Platform(77.4f, 5.6f, 2.6f);
            Platform(83.6f, 6.6f, 2.4f);
            Omeprazol(83.6f, 7.8f);                                             // premio arriba
            Food("Sushi", 78f, 0.92f, FloatingFood.FoodKind.Normal, true);      // ¡anisakis!

            // ----- 5) Alberca de marea -----
            Arena("Arena_3_Marea", 90f, 16f,
                "Contracción del estómago: el ácido sube y baja. La comida flota contigo... pero la hamburguesa se hunde. La H. pylori turquesa te da un refugio en el ácido.",
                W(0.6f, E(prefabs.HPylori, data.HPylori, 2), E(prefabs.HPyloriUreasa, data.HPyloriUreasa, 1)),
                W(1.0f, E(prefabs.HPylori, data.HPylori, 2, SpawnSide.Left), E(prefabs.Anisakis, data.Anisakis, 1)));
            Food("Dona", 88.2f, 0.95f, FloatingFood.FoodKind.Bouncy);
            Food("Hamburguesa", 91.8f, 0.95f, FloatingFood.FoodKind.Sinking);
            Food("Manzana", 95.3f, 0.89f);
            Food("Sushi", 98.6f, 0.92f, FloatingFood.FoodKind.Normal, true);
            Food("Salchicha", 102.4f, 0.79f, FloatingFood.FoodKind.Rolling);

            // ----- 6) Huida: la contracción te persigue hasta la arena del jefe -----
            Sign(Signpost.Kind.Warning, 107.6f, FloorY, "¡CORRE!");
            Peristalsis(109f, 125f);
            Geyser(115.5f, FloorY, 3.5f, 2.2f, 0f);
            Geyser(121.5f, FloorY, 3.5f, 2.2f, 1.1f);
            Platform(115.5f, 2.6f, 2f);

            // ----- 7) Jefe: el Bezoar tapa el píloro -----
            ArenaZone boss = Arena("Arena_4_Bezoar", 129f, 18f,
                "NODO: Algo enorme tapa la salida del estómago...",
                W(1.2f, E(prefabs.Bezoar, data.Bezoar, 1)));
            Platform(131.5f, 2.8f, 2.4f);
            Platform(138.5f, 2.8f, 2.4f);
            Pylorus(147f, boss);
        }

        private void BuildDelgado()
        {
            // Rediseño (205 de largo): 1) duodeno (bilis y grasa) · 2) yeyuno (vellosidades y rotavirus) ·
            // 3) tenia · 4) íleon (río de quimo en balsa) · 5) persecución de la lombriz hasta la válvula ileocecal.
            BuildLevelBase(205f, new LevelStyle
            {
                // Intestino delgado: tejido rosa coral con vellosidades y bilis amarilla verdosa.
                background = new Color(0.11f, 0.03f, 0.05f),
                wall = new Color(0.36f, 0.1f, 0.16f),
                floor = new Color(0.72f, 0.3f, 0.36f),
                floorEdge = new Color(1f, 0.6f, 0.62f),
                blob = new Color(0.8f, 0.38f, 0.45f, 0.75f),
                decor = Decor.Villi,
                backdrop = "Fondo_Delgado",  // pintura estilo caricatura (vellosidades, durazno)
                tissue = "Delgado",          // tejido durazno-coral (Tools/Art/generar_tejido.py)
                // Bilis: amarillo mostaza verdoso.
                liquidDeep = new Color(0.5f, 0.44f, 0.06f, 0.95f),
                liquidSurface = new Color(0.9f, 0.8f, 0.22f, 0.85f),
                liquidOutline = new Color(0.3f, 0.22f, 0.04f, 1f),
                liquidCrest = new Color(1f, 0.96f, 0.6f, 0.95f),
                detail = new Color(0.2f, 0.6f, 0.65f, 0.55f),
                pitLiquid = new Color(0.8f, 0.95f, 0.2f, 1f),
                drip = new Color(0.8f, 0.95f, 0.25f, 0.85f),
                glow = new Color(0.75f, 1f, 0.3f, 1f),
                // Lagos de bilis hundidos (x = inicio, ancho).
                basins = new[]
                {
                    new Vector2(28f, 19f),    // 1) duodeno: gotas de grasa y chorros de bilis
                    new Vector2(52f, 17f),    // 2) yeyuno: bosque de vellosidades sobre la bilis
                    new Vector2(128f, 34f)    // 4) íleon: el río de quimo (balsa)
                }
            }, "NODO: Intestino delgado. Aquí se absorbe la comida... y viven los parásitos. Usa la cápsula Antiparasitario.");

            const float bottom = FloorY - BasinDepth;   // fondo de los lagos de bilis

            // ---------- 1) Duodeno ----------
            Platform(3f, 2.2f, 3f);
            Arena("Arena_1_Giardia", 10f, 18f, "Giardia: salta y se te pega (te frena y te daña). Dispárale para despegarla. Antiparasitario (el antibiótico también sirve).",
                W(0.6f, E(prefabs.Giardia, data.Giardia, 2)),
                W(1.0f, E(prefabs.Giardia, data.Giardia, 2, SpawnSide.Both)));

            Tip(26f, "DUODENO", "NODO: Duodeno. La bilis rompe la grasa en gotitas (la emulsiona). Cruza las gotas ANTES de que llegue el chorro.");
            AcidGeyser g1 = BileGeyser(32.8f, bottom, 4.5f, 3.2f, 0f);
            AcidGeyser g2 = BileGeyser(38.4f, bottom, 4.5f, 3.2f, 1.1f);
            AcidGeyser g3 = BileGeyser(44f, bottom, 4.5f, 3.2f, 2.2f);
            FatDrop(30f, 1.4f);
            FatDrop(32.8f, 1.9f, 1.8f, g1);
            FatDrop(35.6f, 1.5f);
            FatDrop(38.4f, 2f, 1.8f, g2);
            FatDrop(41.2f, 1.6f);
            FatDrop(44f, 1.9f, 1.8f, g3);

            // ---------- 2) Yeyuno: vellosidades ----------
            Tip(50f, "YEYUNO", "NODO: Yeyuno. Las vellosidades absorben la comida. Brinca de punta en punta... y cuidado: hay Giardias escondidas en la bilis.");
            Villus(54f, bottom, 3f);
            Villus(57.2f, bottom, 3.8f);
            Villus(60.4f, bottom, 3.2f);
            Villus(63.6f, bottom, 4f);
            Villus(66.8f, bottom, 3.4f);
            // Giardias escondidas en la bilis: brincan fuera cuando pasas por encima.
            Ambush(61.8f, FloorY - 0.5f, prefabs.Giardia, data.Giardia, 1, 1.8f, 0f, new Vector2(-2f, 11f), false);
            Ambush(68.2f, FloorY - 0.5f, prefabs.Giardia, data.Giardia, 1, 1.8f, 0f, new Vector2(-2f, 11f), false);

            Villus(75.5f, FloorY, 2.6f);
            Villus(79.5f, FloorY, 3.4f);
            Villus(83.5f, FloorY, 2.8f);
            Villus(87.5f, FloorY, 3.6f);
            Villus(91f, FloorY, 2.6f);
            Arena("Arena_2_Rotavirus", 74f, 22f, "Rotavirus (rota = rueda): APLANA las vellosidades (por eso da diarrea). Mátalo rápido o te quedas sin plataformas. Antiviral.",
                W(0.6f, E(prefabs.Rotavirus, data.Rotavirus, 2)),
                W(1.0f, E(prefabs.Rotavirus, data.Rotavirus, 1), E(prefabs.Giardia, data.Giardia, 2, SpawnSide.Left)),
                W(1.0f, E(prefabs.Rotavirus, data.Rotavirus, 2, SpawnSide.Both)));

            Peyer(97f);
            OralSerum(99.5f, 1.3f);

            // ---------- 3) Tenia ----------
            Platform(109.5f, 2.4f, 2.6f);
            Platform(115f, 3.6f, 2.6f);
            Arena("Arena_3_Tenia", 106f, 20f, null,
                W(1.2f, E(prefabs.Tenia, data.Tenia, 1)));
            TissueColumn(123.6f, 8f, 3f);   // pared de intestino donde se agarra la cabeza de la tenia

            // ---------- 4) Íleon: el río de quimo ----------
            Peyer(124.5f);
            Tip(126f, "ÍLEON", "NODO: Íleon. Súbete a la salchicha: el quimo te lleva río abajo. Brinca los chorros de bilis.");
            Food("Salchicha", 130f, 0.79f, FloatingFood.FoodKind.Raft);
            BileGeyser(138f, bottom, 3.2f, 3.6f, 0f);
            BileGeyser(146.5f, bottom, 3.2f, 3.6f, 1.2f);
            BileGeyser(155f, bottom, 3.2f, 3.6f, 2.4f);
            Ambush(134f, 4.2f, prefabs.Rotavirus, data.Rotavirus, 2, 0.5f, 9f, Vector2.zero, false);
            Ambush(142f, 4.6f, prefabs.Rotavirus, data.Rotavirus, 1, 0.5f, 9f, Vector2.zero, false);
            Ambush(150f, 4.2f, prefabs.Rotavirus, data.Rotavirus, 2, 0.5f, 9f, Vector2.zero, false);

            // ---------- 5) Persecución de la lombriz ----------
            Peyer(163.5f);
            // Zona de pelea más larga: sale en 4 puntos separados y la pantalla se cierra alrededor de cada uno.
            Sign(Signpost.Kind.Warning, 163f, FloorY, "¡CORRE!");
            WormChaseAt(164f, 170f, 178f, 186f, 194f);
            IleocecalValve(199f);
        }

        private void BuildGrueso()
        {
            // Rediseño (215 de largo), siguiendo el colon de verdad:
            // 1) ciego y apéndice · 2) colon ascendente (subida con el líquido detrás) · 3) colon transverso, ARRIBA
            // (flora buena y C. difficile) · 4) colon descendente (bajada, lodo y úlceras) · 5) sigmoide y recto (nidos y jefe).
            BuildLevelBase(215f, new LevelStyle
            {
                // Intestino grueso: más oscuro y pesado, pliegues en arco y líquido verde olivo turbio.
                background = new Color(0.07f, 0.03f, 0.03f),
                wall = new Color(0.26f, 0.08f, 0.09f),
                floor = new Color(0.5f, 0.2f, 0.2f),
                floorEdge = new Color(0.85f, 0.42f, 0.4f),
                blob = new Color(0.42f, 0.13f, 0.15f, 0.85f),
                decor = Decor.Folds,
                backdrop = "Fondo_Grueso",   // pintura estilo caricatura (haustras, malva)
                tissue = "Grueso",           // tejido malva con moco dorado
                // Líquido turbio verde olivo.
                liquidDeep = new Color(0.28f, 0.32f, 0.1f, 0.95f),
                liquidSurface = new Color(0.56f, 0.6f, 0.22f, 0.85f),
                liquidOutline = new Color(0.16f, 0.18f, 0.06f, 1f),
                liquidCrest = new Color(0.84f, 0.86f, 0.52f, 0.95f),
                detail = new Color(0.15f, 0.45f, 0.45f, 0.6f),
                pitLiquid = new Color(0.55f, 0.7f, 0.15f, 1f),
                drip = new Color(0.6f, 0.75f, 0.2f, 0.8f),
                glow = new Color(0.55f, 0.85f, 0.2f, 1f)
            }, "NODO: Intestino grueso. Aquí se absorbe el agua y viven billones de bacterias... buenas y malas.");

            const float top = 16f;   // altura del colon transverso

            // ---------- 1) Ciego y apéndice ----------
            Platform(3f, 2.2f, 3f);
            Arena("Arena_1_Amebas", 10f, 18f, "Entamoeba: ameba lenta; su seudópodo llega lejos y te jala. No te quedes enfrente. Antiparasitario.",
                W(0.6f, E(prefabs.Entamoeba, data.Entamoeba, 1), E(prefabs.Oxiuro, data.Oxiuro, 2, SpawnSide.Left)),
                W(1.0f, E(prefabs.Entamoeba, data.Entamoeba, 1), E(prefabs.Oxiuro, data.Oxiuro, 2, SpawnSide.Both)));
            Tip(26f, "HAUSTRAS", "NODO: Haustras. Las bolsas del colon se aprietan y te lanzan: úsalas para subir.");
            Haustra(28f, 0f);
            Haustra(31.5f, 0.9f);
            Haustra(35f, 1.8f);
            // Apéndice secreto: arriba, se llega con la bolsa de en medio + doble salto.
            Platform(31.5f, 5.6f, 3.2f);
            AppendixReward(31.5f, 6.8f);

            // ---------- 2) Colon ascendente: la subida ----------
            Tip(40f, "¡SUBE!", "NODO: Colon ascendente. Sube... ¡y no dejes que te alcance lo que viene de abajo!", FloorY, Signpost.Kind.ArrowUp);
            Flood(41.5f, 57f, top - 0.5f, 0.5f);
            Platform(45f, 2.4f, 2.8f);
            Platform(49.5f, 4.7f, 2.8f);
            Platform(45.5f, 7f, 2.8f);
            Platform(50f, 9.3f, 2.8f);
            Platform(46f, 11.6f, 2.8f);
            Platform(50.5f, 13.9f, 2.8f);
            Platform(54.2f, 15.2f, 2.4f);
            GasVent(53.5f, 9f, 2.8f);
            Ambush(49.5f, 4.7f + 0.3f, prefabs.Oxiuro, data.Oxiuro, 2, 1.2f, 0f, new Vector2(0f, 3f), false);
            Ambush(50.5f, 13.9f + 0.3f, prefabs.Oxiuro, data.Oxiuro, 2, 1.2f, 0f, new Vector2(0f, 3f), false);

            // ---------- 3) Colon transverso (arriba): la microbiota ----------
            SolidBlock(57f, 122f, top);
            Tip(58.6f, "MICROBIOTA", "NODO: Microbiota. Las bacterias verdes son BUENAS: no les dispares antibiótico o crecerá C. difficile.", top);
            foreach (float fx in new[] { 60.5f, 64f, 67.5f, 71f, 74.5f }) FloraAt(fx, top);
            Probiotic(66f, top + 1.6f);
            ArenaZone cdiff = Arena("Arena_2_CDifficile", 80f, 22f, "C. difficile: herida se vuelve espora y todo le rebota. Solo el antibiótico... ¡y cuidado con la flora buena!",
                W(0.6f, E(prefabs.CDifficile, data.CDifficile, 2), E(prefabs.Oxiuro, data.Oxiuro, 1, SpawnSide.Left)),
                W(1.0f, E(prefabs.CDifficile, data.CDifficile, 2), E(prefabs.Entamoeba, data.Entamoeba, 1, SpawnSide.Left)));
            cdiff.floorY = top;
            cdiff.transform.position = new Vector3(80f, top, 0f);
            foreach (float fx in new[] { 83f, 88.5f, 94f }) FloraAt(fx, top);
            Platform(86f, top + 2.6f, 3f);
            Platform(93f, top + 2.6f, 3f);
            foreach (float fx in new[] { 103.5f, 107.5f, 111.5f }) FloraAt(fx, top);
            Probiotic(108.5f, top + 1.6f);
            Ambush(110f, top + 3f, prefabs.CDifficile, data.CDifficile, 2, 0.5f, 7f, new Vector2(0f, 2f), false);
            Ambush(116f, top + 3f, prefabs.Oxiuro, data.Oxiuro, 3, 0.5f, 7f, new Vector2(0f, 2f), false);

            // ---------- 4) Colon descendente: la bajada ----------
            Tip(120f, "¡BAJA!", "NODO: Colon descendente. El colon absorbe el agua: todo se vuelve LODO espeso. No te quedes parado.", top, Signpost.Kind.ArrowDown);
            Bolus(new Vector2(124.5f, top - 0.6f), new Vector2(124.5f, 1.2f), 2.2f, 0f);
            Bolus(new Vector2(130f, 1.2f), new Vector2(130f, top - 2f), 2.2f, 0.5f);
            Platform(127.5f, 11f, 2.4f);
            Platform(133f, 7f, 2.4f);
            Platform(137f, 3.5f, 2.4f);
            Mud(126f, 152f);
            Ulcer(135.5f);
            Ulcer(145f);

            // ---------- 5) Sigmoide y recto ----------
            Tip(152.6f, "RECTO", "NODO: Recto. Los oxiuros ponen sus huevos aquí: rómpelos antes de que salgan más.");
            foreach (float ex in new[] { 154f, 156.5f, 159f }) EnemyAt(prefabs.OxiuroHuevo, ex, FloorY);
            Arena("Arena_3_Nidos", 165f, 18f, "Nidos de oxiuros: los huevos se abren si los dejas. Antiparasitario.",
                W(0.6f, E(prefabs.Oxiuro, data.Oxiuro, 3)),
                W(1.0f, E(prefabs.Entamoeba, data.Entamoeba, 1), E(prefabs.Oxiuro, data.Oxiuro, 2, SpawnSide.Both)),
                W(1.0f, E(prefabs.OxiuroHuevo, data.OxiuroHuevo, 2), E(prefabs.Oxiuro, data.Oxiuro, 2, SpawnSide.Left)));
            Haustra(183f, 0f);

            // Pelea final: la lombriz. Abajo de la mitad de vida, oleadas de "movimiento de masa": hay que subirse.
            Arena("Arena_4_LombrizFinal", 189f, 24f, null,
                W(1.0f, E(prefabs.LombrizGrueso, data.Lombriz, 1)));
            Platform(191.5f, 2.8f, 3f);
            Platform(198f, 3.4f, 3f);
            Platform(204.5f, 2.8f, 3f);
            Haustra(195f, 0.4f);
            Haustra(201.5f, 1.4f);
            MassWaveIn(186f, 210f);
        }

        // ---------- Elementos del Intestino grueso (rediseño) ----------

        private const string LargeIntestineArt = "Assets/_Project/Art/Grueso";

        /// <summary>Bloque de tejido sólido (con colisión) de 'fromX' a 'toX' con el piso arriba en 'topY' (el colon transverso).</summary>
        private void SolidBlock(float fromX, float toX, float topY)
        {
            float width = toX - fromX;
            float height = topY - FloorY + 6f;
            var go = new GameObject("BloqueTejido");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(fromX + width * 0.5f, topY, 0f);
            go.layer = PhysicsLayers.Ground;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, height);
            box.offset = new Vector2(0f, -height * 0.5f);
            if (tissueFloor != null) TissueGround(go.transform, width, height, Color.white, true);
            else SetupUtils.SpriteChild(go.transform, "Bloque", PlaceholderArt.Square, new Color(0.5f, 0.2f, 0.2f), new Vector3(0f, -height, 0f), new Vector2(width, height), -99);
        }

        private void Haustra(float x, float offset)
        {
            var go = new GameObject("Haustra");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(x, FloorY, 0f);
            go.AddComponent<HaustraPouch>().Configure(2.6f, 2.8f, offset, PrefabSetup.ArtByWidth($"{LargeIntestineArt}/Haustra.png", 2.9f, new Vector2(0.5f, 0.05f)), 1.2f);
        }

        private void Flood(float fromX, float toX, float topY, float speed)
        {
            var go = new GameObject("LiquidoQueSube");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3((fromX + toX) * 0.5f, FloorY - 0.3f, 0f);
            go.AddComponent<RisingFlood>().Configure(fromX, toX, topY, speed);
        }

        private void Mud(float fromX, float toX)
        {
            var go = new GameObject("Lodo");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3((fromX + toX) * 0.5f, FloorY, 0f);
            go.AddComponent<MudZone>().Configure(toX - fromX);
        }

        private void Bolus(Vector2 from, Vector2 to, float speed, float phase)
        {
            var go = new GameObject("Bolo");
            go.transform.SetParent(environment, false);
            go.transform.position = from;
            go.AddComponent<MovingPlatform>().Configure(from, to, speed, phase, 2.6f, PrefabSetup.ArtByWidth($"{LargeIntestineArt}/Bolo.png", 2.8f));
        }

        private void Ulcer(float x)
        {
            var go = new GameObject("Ulcera");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(x, FloorY, 0f);
            go.AddComponent<EnemyNest>().Configure(prefabs.Entamoeba, data.Entamoeba, 7f, 1, PrefabSetup.ArtByWidth($"{LargeIntestineArt}/Ulcera.png", 2.2f, new Vector2(0.5f, 0.2f)));
        }

        private void FloraAt(float x, float groundY) => EnemyAt(prefabs.Flora, x, groundY);

        /// <summary>Pone un enemigo (o la flora) fijo en la escena, fuera de las arenas.</summary>
        private void EnemyAt(EnemyHealth prefab, float x, float groundY)
        {
            if (prefab == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject);
            go.transform.SetParent(environment, true);
            go.transform.position = new Vector3(x, groundY, 0f);
        }

        private void Probiotic(float x, float y)
        {
            var go = new GameObject("Probiotico");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(x, y, 0f);
            GameObject art = SetupUtils.Child(go.transform, "Dibujo", Vector3.zero);
            Sprite pic = PrefabSetup.ArtByWidth($"{LargeIntestineArt}/Probiotico.png", 0.7f);
            if (pic != null) SetupUtils.SpriteChild(art.transform, "Frasco", pic, Color.white, Vector3.zero, Vector2.one, 21);
            else
            {
                SetupUtils.SpriteChild(art.transform, "Contorno", PlaceholderArt.SquareCentered, new Color(0.1f, 0.2f, 0.1f), Vector3.zero, new Vector2(0.62f, 0.86f), 20);
                SetupUtils.SpriteChild(art.transform, "Frasco", PlaceholderArt.SquareCentered, new Color(0.95f, 0.97f, 1f), Vector3.zero, new Vector2(0.52f, 0.76f), 21);
                SetupUtils.SpriteChild(art.transform, "Etiqueta", PlaceholderArt.SquareCentered, new Color(0.45f, 0.85f, 0.5f), new Vector3(0f, -0.05f, 0f), new Vector2(0.52f, 0.3f), 22);
                SetupUtils.SpriteChild(art.transform, "Tapa", PlaceholderArt.SquareCentered, new Color(0.3f, 0.65f, 0.35f), new Vector3(0f, 0.42f, 0f), new Vector2(0.56f, 0.14f), 22);
            }
            SetupUtils.SpriteChild(go.transform, "Aura", PlaceholderArt.Circle, new Color(0.5f, 1f, 0.55f, 0.25f), Vector3.zero, new Vector2(1.5f, 1.5f), 19);
            go.AddComponent<ProbioticPickup>().Configure(art.transform);
        }

        /// <summary>Premio del apéndice secreto: vida y bombas (el apéndice tiene tejido de defensa).</summary>
        private void AppendixReward(float x, float y)
        {
            var go = new GameObject("Apendice");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(x, y, 0f);
            GameObject art = SetupUtils.Child(go.transform, "Dibujo", Vector3.zero);
            Sprite pic = PrefabSetup.ArtByWidth($"{LargeIntestineArt}/Apendice.png", 1.1f);
            if (pic != null) SetupUtils.SpriteChild(art.transform, "Ganglio", pic, Color.white, Vector3.zero, Vector2.one, 21);
            else
            {
                SetupUtils.SpriteChild(art.transform, "Contorno", PlaceholderArt.Circle, new Color(0.2f, 0.08f, 0.25f), Vector3.zero, new Vector2(0.9f, 0.9f), 20);
                SetupUtils.SpriteChild(art.transform, "Ganglio", PlaceholderArt.Circle, new Color(0.8f, 0.65f, 1f), Vector3.zero, new Vector2(0.78f, 0.78f), 21);
                SetupUtils.SpriteChild(art.transform, "Brillo", PlaceholderArt.Circle, Color.white, new Vector3(-0.15f, 0.15f, 0f), new Vector2(0.2f, 0.12f), 22);
            }
            SetupUtils.SpriteChild(go.transform, "Aura", PlaceholderArt.Circle, new Color(0.8f, 0.6f, 1f, 0.3f), Vector3.zero, new Vector2(1.6f, 1.6f), 19);
            go.AddComponent<HealPickup>().Configure(art.transform, 25f, 3,
                "NODO: ¡Encontraste el APÉNDICE! Tiene tejido de defensa (linfocitos). +Vida y +3 bombas.");
        }

        private void MassWaveIn(float fromX, float toX)
        {
            var go = new GameObject("MovimientoDeMasa");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(fromX, FloorY, 0f);
            go.AddComponent<MassWave>().Configure(fromX, toX);
        }

        /// <summary>
        /// Mundo secreto: adelanto de una idea para después (un planeta vivo invadido por patógenos alienígenas).
        /// NO está en el juego: no aparece en el mapa ni en la compilación. Solo se abre desde el editor:
        /// Healing Knight → Abrir escena → Extra · Mundo secreto (prueba). Es una arena corta con enemigos que ya existen.
        /// </summary>
        private void BuildMundoSecreto()
        {
            BuildLevelBase(60f, new LevelStyle
            {
                // Planeta vivo: cielo verde azulado profundo, suelo de cristal lila que brilla en turquesa.
                background = new Color(0.02f, 0.08f, 0.09f),
                wall = new Color(0.16f, 0.1f, 0.3f),
                floor = new Color(0.3f, 0.2f, 0.5f),
                floorEdge = new Color(0.35f, 1f, 0.85f),
                blob = new Color(0.25f, 0.9f, 0.7f, 0.3f),
                decor = Decor.Rings,
                glow = new Color(0.35f, 1f, 0.85f, 1f)
            }, "¿?¿?¿? Esto no es un cuerpo... es un PLANETA VIVO. Unos patógenos que no son de aquí lo están invadiendo. (Próximamente.)");

            Platform(6f, 2.4f, 2.6f);
            Arena("Arena_Invasores", 12f, 20f, "Patógenos alienígenas: se parecen a los que ya conoces... pero vienen de otro mundo.",
                W(0.8f, E(prefabs.Rotavirus, data.Rotavirus, 2), E(prefabs.Polen, data.Polen, 2, SpawnSide.Left)),
                W(1.0f, E(prefabs.Herpes, data.Herpes, 1), E(prefabs.Neumococo, data.Neumococo, 3, SpawnSide.Both)));
            Platform(16f, 3.6f, 2.6f);
            Platform(24f, 2.4f, 2.6f);
            SetMessage("Fin del adelanto. Gracias por encontrar el diente.", 40f);
        }

        // ---------- Niveles del Sistema Respiratorio (GDD: avance por bronquiolos, mucosidad e inflamación) ----------
        // Paleta del arte de referencia: tejido violeta oscuro, bordes cian que brillan, alvéolos lila translúcidos,
        // mucosidad amarillo verdosa e inflamación roja.

        private static readonly Color RespGlow = new Color(0.45f, 0.95f, 1f, 1f);

        private void BuildNariz()
        {
            // Rediseño (170 u): cañones de moco de la mucosa, el ESTORNUDO (barra de comezón + pelos nasales)
            // y dos arenas. Ver LEEME "Nariz".
            BuildLevelBase(170f, new LevelStyle
            {
                background = new Color(0.09f, 0.03f, 0.11f),
                wall = new Color(0.36f, 0.14f, 0.34f),
                floor = new Color(0.48f, 0.2f, 0.42f),
                floorEdge = RespGlow,
                blob = new Color(0.7f, 0.32f, 0.58f, 0.35f),
                decor = Decor.Blobs,
                glow = RespGlow,
                cilia = true,
                backdrop = "Fondo_Nariz",    // pintura estilo caricatura (cornetes, pelos y moco)
                tissue = "Nariz",            // tejido ciruela-magenta
                pitLiquid = new Color(1f, 1f, 1f, 1f), // los huecos llevan líquido animado: moco
                liquidDeep = new Color(0.42f, 0.5f, 0.12f, 0.95f),
                liquidSurface = new Color(0.8f, 0.88f, 0.36f, 0.85f),
                liquidOutline = new Color(0.22f, 0.26f, 0.06f, 1f),
                liquidCrest = new Color(0.95f, 1f, 0.7f, 0.95f)
            }, "NODO: Sistema respiratorio. Entraste por la nariz. Tu cápsula regional ahora es el MUCOLÍTICO.",
                new Vector2(33f, 3f), new Vector2(63f, 3f), new Vector2(100f, 3.5f), new Vector2(112f, 4f), new Vector2(152f, 3f));

            Platform(1f, 2.2f, 3f);

            // Arena 1: virus del resfriado y polen.
            Arena("Arena_1_Resfriado", 8f, 18f,
                "Rinovirus (resfriado): no hay antiviral para él, los medicamentos le hacen poco y se cura solo con el tiempo. El polen te persigue con el aire: Antihistamínico.",
                W(0.8f, E(prefabs.Rinovirus, data.Rinovirus, 1), E(prefabs.Polen, data.Polen, 2)),
                W(1.0f, E(prefabs.Rinovirus, data.Rinovirus, 2), E(prefabs.Polen, data.Polen, 2, SpawnSide.Left)));
            Platform(10f, 2.4f, 2.6f);
            Platform(15.5f, 3.8f, 2.6f);
            Platform(21f, 2.4f, 2.6f);

            // Cañones de moco: la mucosa te lanza bolas. Te frenan, pero las que caen se pegan y sirven de escalón.
            Tip(24f, "MOCO", "La mucosa lanza bolas de MOCO: si te pegan te frenan, pero las que caen se quedan pegadas un rato y sirven de plataforma. El mucolítico las disuelve.");
            MucusGland(30f, 2.6f, 0f);
            Platform(28f, 2.4f, 2.2f);
            MucusGland(38f, 2.8f, 1.2f);
            Platform(38.5f, 3.4f, 2.6f);
            Tip(41f, "MUCOSIDAD", "Mucosidad: tapa el paso y te frena si la tocas. Dispárale MUCOLÍTICO (el antihistamínico también sirve, pero menos).");
            Block(44f, true);
            Checkpoint(46f);

            // EL ESTORNUDO: el polen irrita la nariz y llena la barra; al llenarse, ¡ACHÚ! y el aire te avienta atrás.
            Tip(47f, "¡COMEZÓN!", "¡El polen irrita la nariz! Mira la barra de COMEZÓN: cuando se llene vas a estornudar y el aire te avienta hacia atrás. Agárrate de los PELOS NASALES o escóndete tras un cornete. Matar polen la llena más lento.", FloorY, Signpost.Kind.Warning);
            Sneeze(47f, 94f);
            NasalHairAt(52f);
            EnemyAt(prefabs.Polen, 56f, FloorY + 2.5f);
            NasalHairAt(60f);
            Platform(64.5f, 2.6f, 1.6f);          // escalón sobre el hueco: si estornuda aquí... ¡cuidado!
            NasalHairAt(68.5f);
            Turbinate(73.5f, 2.6f, 1.4f);         // cornete: escóndete a su izquierda
            EnemyAt(prefabs.Polen, 70f, FloorY + 3f);

            // Arena 2 dentro del estornudo: mucho polen = mucha comezón.
            Arena("Arena_2_Polen", 79f, 15f, "¡Nube de polen! Cada uno vivo llena más rápido la comezón. Antihistamínico y agárrate de los pelos.",
                W(0.6f, E(prefabs.Polen, data.Polen, 3), E(prefabs.Rinovirus, data.Rinovirus, 1)),
                W(1.0f, E(prefabs.Polen, data.Polen, 3, SpawnSide.Both)));
            NasalHairAt(80f);
            NasalHairAt(87.5f);
            Platform(83.5f, 3f, 2.6f);
            Checkpoint(95f);

            // Moco en el camino: bolas que caen y huecos (úsalas de escalón si te sirven).
            MucusGland(104f, 2.4f, 0.4f);
            Platform(106.5f, 2.6f, 2.2f);
            MucusGland(110f, 2.6f, 1.6f, 1);
            Platform(114f, 2.6f, 1.6f);
            O2(108f, 5f);

            // Arena 3: estafilococos con cañones de moco disparando a todos lados.
            BlockedArena("Arena_3_Estafilococo", 120f, 24f,
                "S. aureus: racimo de cocos; al romperlo se separa en cocos sueltos. Antibiótico. Cuidado con el moco de las glándulas.", true,
                W(0.6f, E(prefabs.SAureus, data.SAureus, 3)),
                W(1.0f, E(prefabs.SAureus, data.SAureus, 2, SpawnSide.Left), E(prefabs.Polen, data.Polen, 2), E(prefabs.Rinovirus, data.Rinovirus, 1)),
                W(1.0f, E(prefabs.SAureus, data.SAureus, 2, SpawnSide.Both), E(prefabs.Polen, data.Polen, 2)));
            MucusGland(125f, 3f, 0f, 1);
            MucusGland(134f, 3f, 1.5f);
            Platform(123f, 2.4f, 2.6f);
            Platform(129.5f, 3.8f, 2.6f);
            Platform(136f, 2.4f, 2.6f);
            Checkpoint(143f);

            // Recta final: el último estornudo, más polen.
            Sign(Signpost.Kind.Warning, 144.6f, FloorY, "¡ESTORNUDO!");
            Sneeze(145f, 166f);
            NasalHairAt(148f);
            EnemyAt(prefabs.Polen, 151f, FloorY + 2.5f);
            Platform(153.5f, 2.6f, 1.6f);
            NasalHairAt(157f);
            EnemyAt(prefabs.Polen, 160f, FloorY + 3f);
            NasalHairAt(162.5f);
        }

        private void BuildTraquea()
        {
            // Rediseño (170 u): epiglotis, escalera de cilios, la TOS (anillos de cartílago), corrientes de aire
            // y el jefe: la reina de la tos ferina. Ver LEEME "Tráquea".
            BuildLevelBase(170f, new LevelStyle
            {
                background = new Color(0.06f, 0.04f, 0.13f),
                wall = new Color(0.28f, 0.2f, 0.46f),
                floor = new Color(0.4f, 0.28f, 0.58f),
                floorEdge = RespGlow,
                blob = new Color(0.6f, 0.52f, 0.85f, 0.3f), // anillos de cartílago
                decor = Decor.Rings,
                glow = RespGlow,
                cilia = true,
                backdrop = "Fondo_Traquea",  // pintura estilo caricatura (anillos de cartílago y cilios)
                tissue = "Traquea",          // tejido índigo-violeta
                pitLiquid = new Color(1f, 1f, 1f, 1f), // huecos con moco claro azulado
                liquidDeep = new Color(0.2f, 0.32f, 0.55f, 0.95f),
                liquidSurface = new Color(0.55f, 0.82f, 0.95f, 0.8f),
                liquidOutline = new Color(0.1f, 0.14f, 0.32f, 1f),
                liquidCrest = new Color(0.85f, 0.97f, 1f, 0.95f)
            }, "NODO: Tráquea. Detectamos tos ferina: Bordetella pertussis. Antibiótico.",
                new Vector2(66f, 3f), new Vector2(81f, 3.5f), new Vector2(100f, 4.5f));

            Arena("Arena_1_TosFerina", 8f, 18f, "B. pertussis (tos ferina): se infla y tose tres veces; cada tos te empuja. Antibiótico.",
                W(0.6f, E(prefabs.Pertussis, data.Pertussis, 2)),
                W(1.0f, E(prefabs.Pertussis, data.Pertussis, 2, SpawnSide.Both), E(prefabs.Rinovirus, data.Rinovirus, 1)));
            Platform(11f, 2.4f, 2.6f);
            Platform(18f, 2.4f, 2.6f);

            // Epiglotis: se cierra cada vez que tragas.
            Tip(22f, "EPIGLOTIS", "La EPIGLOTIS es una tapita que cierra la tráquea cada vez que tragas, para que la comida no se vaya a los pulmones. Pasa cuando esté abierta.");
            Epiglottis(25.5f, 2.4f, 2f, 0f);

            // Escalera de cilios: el piso te empuja hacia atrás y rueda moco hacia ti.
            Tip(27f, "CILIOS", "Los CILIOS barren el moco hacia la garganta: en el piso te empujan HACIA ATRÁS y te mandan bolas de moco. Sáltalas, ve por arriba o disuélvelas con Mucolítico.");
            Cilia(28f, 52f, 3.2f, 2.2f);
            Platform(31f, 2.2f, 2.2f);
            Platform(36.5f, 3.6f, 2.2f);
            Platform(42f, 2.2f, 2.2f);
            Platform(47.5f, 3.4f, 2.2f);
            Checkpoint(53f);

            Tip(53f, "INFLAMACIÓN", "Inflamación: el tejido hinchado cierra el paso. Dispárale ANTIINFLAMATORIO hasta que se contraiga.");
            Block(56f, false);

            // LA TOS: cada cierto tiempo te avienta hacia atrás; cúbrete bajo los anillos de cartílago.
            Cough(58f, 92f, 6f);
            Sign(Signpost.Kind.Warning, 58.2f, FloorY, "¡TOS!");
            CartilageRing(61f);
            CartilageRing(70f);
            EnemyAt(prefabs.Pertussis, 75f, FloorY + 2f);
            CartilageRing(78.5f);
            Platform(82.75f, 2.4f, 1.4f);
            CartilageRing(88f);
            // Secreto: una corriente escondida junto al anillo te sube a la GARGANTA (entrada a las Amígdalas).
            Vent(89.8f, 8.2f, 3.2f, 0.8f);
            Platform(90.6f, 7.4f, 2.2f);
            Sign(Signpost.Kind.Secret, 88.6f, FloorY, "?");   // pista: algo hay arriba
            SecretEntrance(90.6f, 8.5f, data.RespiratorioAmigdalas, "respiratorio_amigdalas",
                "¡Un pasaje hacia arriba, a la GARGANTA! Ahí están las amígdalas.",
                "De vuelta a las amígdalas: trae antígenos.", false);
            Checkpoint(92f);

            // Corrientes de aire hacia arriba: te lanzan a la ruta alta sobre el hueco grande.
            Tip(93f, "¡AIRE!", "Corrientes de AIRE: cuando soplen, párate encima y te lanzan muy alto.", FloorY, Signpost.Kind.ArrowUp);
            Vent(95.5f, 6.5f, 3f, 0f);
            Platform(98.5f, 6.2f, 3f);
            Platform(103.5f, 4.6f, 2.4f);
            Vent(108f, 6.5f, 3f, 1.5f);
            O2(108f, 8f);

            // Arena 2: influenza y tos ferina.
            BlockedArena("Arena_2_Bronquio", 115f, 22f,
                "Influenza: flota alto y se lanza en picada con sus espículas. Antiviral. Al final, mucosidad: Mucolítico.", true,
                W(0.6f, E(prefabs.Pertussis, data.Pertussis, 3)),
                W(1.0f, E(prefabs.Influenza, data.Influenza, 1), E(prefabs.Pertussis, data.Pertussis, 2, SpawnSide.Left)),
                W(1.0f, E(prefabs.Influenza, data.Influenza, 2), E(prefabs.Polen, data.Polen, 2, SpawnSide.Both)));
            Platform(118f, 2.4f, 2.6f);
            Platform(123f, 3.8f, 2.6f);
            Platform(128f, 2.4f, 2.6f);
            Checkpoint(135f);

            // Jefe: la reina de la tos ferina. Sus toses mandan ondas por el piso: salta.
            Arena("Arena_Reina", 140f, 26f, null,
                W(1.2f, E(prefabs.TosFerinaReina, data.TosFerinaReina, 1)));
            Platform(143f, 2.4f, 2.6f);
            Platform(150f, 3.8f, 2.6f);
            Platform(157f, 2.4f, 2.6f);
        }

        private void BuildPulmones()
        {
            // Rediseño (202 u): todo el pulmón RESPIRA (inhala te jala, exhala te empuja), O₂ que cura y CO₂ que asfixia,
            // bronquiolos con dos rutas, la colmena y al final el viaje en glóbulo rojo por un capilar. Ver LEEME "Pulmones".
            LevelManager manager = BuildLevelBase(202f, new LevelStyle
            {
                background = new Color(0.05f, 0.05f, 0.15f),
                wall = new Color(0.26f, 0.19f, 0.44f),
                floor = new Color(0.36f, 0.25f, 0.54f),
                floorEdge = RespGlow,
                blob = new Color(0.85f, 0.7f, 0.95f, 0.28f), // alvéolos
                decor = Decor.Alveoli,
                glow = new Color(0.55f, 1f, 0.95f, 1f),
                cilia = true,
                backdrop = "Fondo_Pulmones", // pintura estilo caricatura (bronquiolos y alvéolos de noche)
                tissue = "Pulmones",         // tejido índigo con lavanda
                pitLiquid = new Color(1f, 1f, 1f, 1f), // huecos con líquido turquesa (edema)
                liquidDeep = new Color(0.08f, 0.32f, 0.4f, 0.95f),
                liquidSurface = new Color(0.4f, 0.9f, 0.9f, 0.8f),
                liquidOutline = new Color(0.04f, 0.14f, 0.2f, 1f),
                liquidCrest = new Color(0.8f, 1f, 0.98f, 0.95f)
            }, "NODO: Pulmones. La infección se asentó en los bronquiolos. Cada tramo está bloqueado: elimina al grupo y despeja el paso.",
                new Vector2(30f, 4f), new Vector2(45f, 4.5f), new Vector2(138f, 4.5f), new Vector2(147f, 4f), new Vector2(187.5f, 8.5f));

            // Tramo 1
            BlockedArena("Tramo_1_Neumonia", 8f, 20f,
                "S. pneumoniae (neumonía): avanzan a saltos y te caen encima. Antibiótico.", true,
                W(0.6f, E(prefabs.Neumococo, data.Neumococo, 3)),
                W(1.0f, E(prefabs.Neumococo, data.Neumococo, 2, SpawnSide.Left), E(prefabs.Influenza, data.Influenza, 1)));
            Platform(10f, 2.4f, 2.6f);
            Platform(15.5f, 3.8f, 2.6f);
            Platform(20.5f, 2.4f, 2.6f);

            // Desde aquí el pulmón respira (sincronizado con los alvéolos).
            Tip(27f, "RESPIRA", "NODO: Todo el pulmón RESPIRA. Al INHALAR el aire te jala hacia adelante y los alvéolos se inflan (trampolines); al EXHALAR te empuja hacia atrás y se desinflan.");
            Breathing(26f, 186f);
            Alveolus(32f, 1.2f, 2f, 0f);
            Platform(35.8f, 4.4f, 2.4f);
            Alveolus(39.5f, 1.2f, 2f, 1.4f);
            Platform(43f, 4.6f, 2.4f);
            Alveolus(47.2f, 1.2f, 1.8f, 2.75f);
            Platform(51.2f, 4.2f, 2f);

            // Oxígeno y dióxido de carbono.
            Tip(55f, "O<sub>2</sub> y CO<sub>2</sub>", "Burbujas de O<sub>2</sub> (oxígeno): te curan. Nubes de CO<sub>2</sub> (el gas de desecho que exhalas): te asfixian si te quedas dentro.");
            O2(58f, 1.4f);
            CO2(62f, 1.5f);
            Platform(65f, 3.4f, 2.4f);
            O2(65f, 4.8f);
            CO2(69.5f, 3f);
            O2(73f, 1.4f);
            Checkpoint(75f);

            // Bronquiolos: el camino se divide. Arriba (difícil): oxígeno y una bomba. Abajo (fácil): CO₂ y bacterias.
            Tip(76.4f, "¿ARRIBA O ABAJO?", "Los bronquiolos se dividen: ARRIBA hay oxígeno y una bomba, pero hay que saltar bien; ABAJO es más fácil pero está lleno de CO<sub>2</sub>.");
            Alveolus(78f, 1.2f, 2f, 0f);
            Platform(82f, 4.6f, 2.6f);
            Platform(86.5f, 6.2f, 3f);
            Platform(91.5f, 6.6f, 3f);
            O2(91.5f, 8f);
            Platform(96.5f, 6.2f, 3f);
            Platform(101.5f, 6.6f, 3f);
            BombGift(101.5f, 7.6f, "+1 bomba: tomaste el bronquiolo de arriba.");
            Platform(106.5f, 5.4f, 3f);
            Platform(111f, 4f, 2.4f);
            CO2(89f, 1.3f);
            EnemyAt(prefabs.Neumococo, 93f, FloorY + 1f);
            CO2(98f, 1.3f);
            EnemyAt(prefabs.Neumococo, 102f, FloorY + 1f);
            CO2(105.5f, 1.3f);

            // Tramo 2
            BlockedArena("Tramo_2_Influenza", 118f, 20f,
                "Influenza en el aire y neumococos en el piso. Al final, inflamación: Antiinflamatorio.", false,
                W(0.6f, E(prefabs.Influenza, data.Influenza, 2)),
                W(1.0f, E(prefabs.Neumococo, data.Neumococo, 3, SpawnSide.Both), E(prefabs.Influenza, data.Influenza, 1)));
            Alveolus(120f, 2.4f, 2.4f);
            Alveolus(125f, 3.8f, 2.4f);
            Alveolus(130f, 2.4f, 2.4f);
            Checkpoint(136f);

            // Secuencia de timing sobre dos huecos.
            Alveolus(139.2f, 1.2f, 1.6f, 0f);
            Alveolus(141.6f, 1.2f, 1.6f, 2.75f);
            Platform(144.5f, 2.4f, 1.6f);
            Alveolus(149f, 1.2f, 2f, 1.4f);
            Platform(152.5f, 4.4f, 2.4f);
            O2(152.5f, 5.8f);

            // Tramo 3: arena de la colmena.
            BlockedArena("Tramo_3_Colmena", 160f, 28f, "NODO: Algo enorme se mueve arriba...", true,
                W(0.6f, E(prefabs.Neumococo, data.Neumococo, 4, SpawnSide.Both)),
                W(1.5f, E(prefabs.Colmena, data.Colmena, 1)));
            Alveolus(164f, 2.6f, 3f);
            Platform(169.5f, 4.2f, 3.4f);
            Alveolus(174f, 1.2f, 3f, 1f);
            Platform(179f, 4.2f, 3f);
            Checkpoint(185f);

            // Capilar: el glóbulo rojo se lleva el oxígeno a la sangre... y a ti.
            BloodPit(187.5f, 8.5f);
            Ride("GloboRojo", new Vector2(186.2f, FloorY + 0.5f), new Vector2(197.2f, FloorY + 0.5f), 2.6f, 2.4f,
                MovingPlatform.Style.BloodCell, 0.6f,
                "¡Súbete al GLÓBULO ROJO! En los alvéolos recoge el oxígeno y lo lleva por los capilares a todo el cuerpo.",
                PrefabSetup.ArtByWidth($"{RespArt}/GloboRojo.png", 2.6f));

            SetupUtils.SetString(manager, "completeMessage",
                "NODO: Fórmula 2 enviada a Aurelis. Alerta: la infección avanza hacia el cerebro...");
            SetupUtils.SetFloat(manager, "completeDelay", 4.5f);
        }

        /// <summary>Alvéolo: plataforma de un solo sentido que se ve como racimo de burbujas.</summary>
        // ---------- Boca (rediseño) y niveles secretos: Amígdalas y Ojo ----------

        private const string MouthArt = "Assets/_Project/Art/Boca";
        private const string ThroatArt = "Assets/_Project/Art/Amigdalas";
        private const string EyeArt = "Assets/_Project/Art/Ojo";

        private void Floss() => Node("HiloDental", 0f, FloorY).AddComponent<FlossGrapple>();

        /// <summary>Gancho para el hilo dental colgado de un diente de arriba (y el diente de adorno).</summary>
        private void Hook(float x, float y)
        {
            GameObject go = Node("GanchoDiente", x, FloorY + y);
            go.AddComponent<FlossHook>().Configure(0f, true, 3.8f);
            go.AddComponent<ButtonPrompt>().Configure("herramienta", "Hilo dental", new Vector2(0f, -1.1f), 4.6f, ButtonPrompt.HideWhen.Swinging, signFont);
            Sprite tooth = PrefabSetup.ArtByWidth($"{MouthArt}/DienteArriba.png", 1.6f, new Vector2(0.5f, 0.05f));
            if (tooth != null) { SetupUtils.SpriteChild(go.transform, "Diente", tooth, Color.white, new Vector3(0f, -0.2f, 0f), Vector2.one, -8); return; }   // el aro del dibujo queda en el gancho
            Color line = new Color(0.3f, 0.2f, 0.22f), enamel = new Color(0.98f, 0.96f, 0.9f);
            SetupUtils.SpriteChild(go.transform, "Contorno", PlaceholderArt.SquareCentered, line, new Vector3(0f, 1.6f, 0f), new Vector2(1.5f, 2.6f), -9);
            SetupUtils.SpriteChild(go.transform, "Diente", PlaceholderArt.SquareCentered, enamel, new Vector3(0f, 1.6f, 0f), new Vector2(1.36f, 2.5f), -8);
            SetupUtils.SpriteChild(go.transform, "Punta", PlaceholderArt.Circle, enamel, new Vector3(0f, 0.35f, 0f), new Vector2(1.36f, 0.8f), -8);
            SetupUtils.SpriteChild(go.transform, "Brillo", PlaceholderArt.Circle, Color.white, new Vector3(-0.35f, 1.8f, 0f), new Vector2(0.25f, 0.9f), -7);
        }

        private void Molar(float x, float phase) =>
            Node("MuelaQueMastica", x, FloorY).AddComponent<ChewingMolar>().Configure(3f, 8f, 2.6f, phase,
                PrefabSetup.ArtByWidth($"{MouthArt}/Muela.png", 3.3f, new Vector2(0.5f, 0.02f)));

        private void Tongue(float fromX, float toX, int segments)
        {
            float step = (toX - fromX) / segments;
            for (int i = 0; i < segments; i++)
            {
                float x = fromX + step * (i + 0.5f);
                GameObject go = Node("Lengua", x, FloorY + 0.6f);
                var p = go.AddComponent<MovingPlatform>();
                p.Configure(new Vector2(x, FloorY + 0.4f), new Vector2(x, FloorY + 2.6f), 1.6f, (i * 0.25f) % 1f, step - 0.4f, null);
                p.SetStyle(MovingPlatform.Style.Tongue);
            }
        }

        private void Plaque(float x) =>
            Node("PlacaEntreDientes", x, FloorY).AddComponent<PlaqueWall>().Configure(9f, PrefabSetup.ArtByWidth($"{MouthArt}/Placa.png", 2.5f));

        private void Sugar(float fromX, float toX) =>
            Node("LluviaDeAzucar", fromX, FloorY).AddComponent<SugarRain>().Configure(fromX, toX, FloorY + 9f, 1.4f, data.SMutans,
                PrefabSetup.ArtByWidth($"{MouthArt}/Dulce.png", 0.7f));

        private void Saliva(float fromX, float toX, float period) =>
            Node("OlaDeSaliva", fromX, FloorY).AddComponent<SalivaWave>().Configure(fromX, toX, period);

        /// <summary>Entrada a un nivel secreto (como el diente del Estómago): al tocarla te lleva y lo agrega al mapa.</summary>
        private void SecretEntrance(float x, float y, LevelData target, string secretId, string first, string again, bool eye)
        {
            GameObject go = Node(eye ? "NervioOptico" : "HaciaLasAmigdalas", x, FloorY + y);
            var entrance = go.AddComponent<SecretTooth>();
            GameObject visual = SetupUtils.Child(go.transform, "Dibujo", Vector3.zero);
            Color line = new Color(0.12f, 0.05f, 0.15f);
            if (eye)
            {
                SetupUtils.SpriteChild(visual.transform, "Aura", PlaceholderArt.Circle, new Color(1f, 0.95f, 0.6f, 0.35f), Vector3.zero, new Vector2(1.8f, 1.8f), 19);
                SetupUtils.SpriteChild(visual.transform, "Contorno", PlaceholderArt.Circle, line, Vector3.zero, new Vector2(1.2f, 0.85f), 20);
                SetupUtils.SpriteChild(visual.transform, "Blanco", PlaceholderArt.Circle, Color.white, Vector3.zero, new Vector2(1.08f, 0.74f), 21);
                SetupUtils.SpriteChild(visual.transform, "Iris", PlaceholderArt.Circle, new Color(0.3f, 0.65f, 1f), Vector3.zero, new Vector2(0.5f, 0.5f), 22);
                SetupUtils.SpriteChild(visual.transform, "Pupila", PlaceholderArt.Circle, line, Vector3.zero, new Vector2(0.24f, 0.24f), 23);
                SetupUtils.SpriteChild(visual.transform, "Brillo", PlaceholderArt.Circle, Color.white, new Vector3(-0.1f, 0.1f, 0f), new Vector2(0.1f, 0.1f), 24);
            }
            else
            {
                SetupUtils.SpriteChild(visual.transform, "Aura", PlaceholderArt.Circle, new Color(1f, 0.6f, 0.7f, 0.35f), Vector3.zero, new Vector2(1.8f, 1.8f), 19);
                SetupUtils.SpriteChild(visual.transform, "Contorno", PlaceholderArt.Circle, line, Vector3.zero, new Vector2(1.1f, 1.2f), 20);
                SetupUtils.SpriteChild(visual.transform, "Amigdala", PlaceholderArt.Circle, new Color(1f, 0.55f, 0.6f), Vector3.zero, new Vector2(0.96f, 1.06f), 21);
                for (int i = 0; i < 3; i++)
                    SetupUtils.SpriteChild(visual.transform, "Cripta", PlaceholderArt.Circle, new Color(0.75f, 0.3f, 0.4f), new Vector3(-0.2f + i * 0.2f, 0.2f - (i % 2) * 0.35f, 0f), new Vector2(0.16f, 0.12f), 22);
                SetupUtils.SpriteChild(visual.transform, "Brillo", PlaceholderArt.Circle, Color.white, new Vector3(-0.2f, 0.3f, 0f), new Vector2(0.2f, 0.12f), 23);
            }
            SetupUtils.SetString(entrance, "secretId", secretId);
            SetupUtils.SetRef(entrance, "targetLevel", target);
            SetupUtils.SetString(entrance, "firstMessage", first);
            SetupUtils.SetString(entrance, "againMessage", again);
            SetupUtils.SetRef(entrance, "visual", visual.transform);
        }

        private void Antigens(params EnemyData[] sources) => Node("Antigenos", 0f, FloorY).AddComponent<AntigenSystem>().Configure(sources);
        private void Germinal(float x) => Node("CentroGerminal", x, FloorY).AddComponent<GerminalCenter>();
        private void Stealth() => Node("Sigilo", 0f, FloorY).AddComponent<StealthMeter>().Configure(prefabs.Herpes, data.Herpes);

        private void WatcherAt(float fromX, float toX, float y = 0f) =>
            Node("Vigilante", fromX, FloorY + y).AddComponent<Watcher>().Configure(fromX, toX, 1.4f,
                PrefabSetup.ArtByWidth($"{EyeArt}/Macrofago.png", 1.9f, new Vector2(0.5f, 0.02f)));

        private void Cover(float x, float y = 0f)
        {
            GameObject go = Node("Escondite", x, FloorY + y);
            go.AddComponent<HidingSpot>().Configure(2.2f, 1.6f, false, Vector2.zero, 6f);
            go.AddComponent<ButtonPrompt>().Configure("abajo", "Esconderse", new Vector2(0f, 2.3f), 2.2f, ButtonPrompt.HideWhen.Crouching, signFont);
        }

        private void Floater(float x, float y, float driftX, float period) =>
            Node("MoscaVolante", x, FloorY + y).AddComponent<HidingSpot>().Configure(2.2f, 2.4f, true, new Vector2(driftX, 0.3f), period);

        private void Retina(float fromX, float toX) => Node("Retina", fromX, FloorY).AddComponent<NoisyFloor>().Configure(fromX, toX);
        private void Beam(float fromX, float toX, float period) => Node("HazDeLuz", fromX, FloorY).AddComponent<LightBeam>().Configure(fromX, toX, period);
        private void BlindSpotAt(float x, float width) => Node("PuntoCiego", x, FloorY).AddComponent<BlindSpot>().Configure(width);

        /// <summary>Cristalino: lente gigante transparente de adorno.</summary>
        private void Lens(float x)
        {
            Sprite pic = PrefabSetup.ArtByWidth($"{EyeArt}/Cristalino.png", 9f);
            if (pic != null) { SetupUtils.SpriteChild(environment, "Cristalino", pic, new Color(1f, 1f, 1f, 0.8f), new Vector3(x, FloorY + 4.5f, 0f), Vector2.one, -60); return; }
            SetupUtils.SpriteChild(environment, "CristalinoBorde", PlaceholderArt.Circle, new Color(0.7f, 0.85f, 1f, 0.35f), new Vector3(x, FloorY + 4.5f, 0f), new Vector2(9f, 8f), -61);
            SetupUtils.SpriteChild(environment, "Cristalino", PlaceholderArt.Circle, new Color(0.85f, 0.95f, 1f, 0.22f), new Vector3(x, FloorY + 4.5f, 0f), new Vector2(8.4f, 7.4f), -60);
            SetupUtils.SpriteChild(environment, "CristalinoBrillo", PlaceholderArt.Circle, new Color(1f, 1f, 1f, 0.35f), new Vector3(x - 2f, FloorY + 6.5f, 0f), new Vector2(2.2f, 1f), -59);
        }

        private void BuildBoca()
        {
            // Rediseño (175 u): mecánica propia, el HILO DENTAL (columpiarse de ganchos y arrancar placa),
            // muelas que mastican, lengua que se ondula, lluvia de azúcar, olas de saliva y el jefe Sarro. Ver LEEME "Boca".
            LevelManager manager = BuildLevelBase(175f, new LevelStyle
            {
                background = new Color(0.14f, 0.03f, 0.06f),
                wall = new Color(0.45f, 0.12f, 0.2f),
                floor = new Color(0.85f, 0.4f, 0.5f),
                floorEdge = new Color(1f, 0.97f, 0.9f),
                blob = new Color(0.97f, 0.95f, 0.88f, 0.85f),
                decor = Decor.Blobs,
                drip = new Color(0.85f, 0.95f, 1f, 0.6f),
                glow = new Color(0.9f, 0.95f, 1f, 1f),
                backdrop = "Fondo_Boca",
                tissue = "Boca",
                pitLiquid = new Color(1f, 1f, 1f, 1f),
                liquidDeep = new Color(0.45f, 0.62f, 0.8f, 0.9f),
                liquidSurface = new Color(0.82f, 0.94f, 1f, 0.75f),
                liquidOutline = new Color(0.2f, 0.28f, 0.42f, 1f),
                liquidCrest = new Color(1f, 1f, 1f, 0.95f)
            }, "NIVEL SECRETO · BOCA. NODO: Placa bacteriana detectada. Te traje HILO DENTAL: botón {herramienta}.",
                new Vector2(24f, 6f), new Vector2(40f, 8f), new Vector2(54f, 9f), new Vector2(97f, 22f));

            Floss();

            Arena("Arena_1_Caries", 8f, 16f, "S. mutans (caries): te lanza ácido en arco desde lejos. Antibiótico.",
                W(0.6f, E(prefabs.SMutans, data.SMutans, 2)),
                W(1.0f, E(prefabs.SMutans, data.SMutans, 2, SpawnSide.Left)));
            Platform(12f, 2.2f, 2f);
            Platform(17f, 2.2f, 2f);

            // Tutorial del hilo: un hueco de saliva que solo se cruza columpiándose.
            Tip(21f, "HILO DENTAL", "Hueco de saliva: lanza el HILO DENTAL ({herramienta}) al gancho del diente, columpiate con {mover} y salta ({saltar}) para soltarte.");
            Hook(27f, 7.2f);

            // Placa entre dientes: solo se quita jalándola con el hilo.
            Plaque(35f);

            // Columpios encadenados.
            Hook(42.5f, 7.4f);
            Hook(46.5f, 7.8f);
            EnemyAt(prefabs.SMutans, 51f, FloorY);
            Hook(56.5f, 7.6f);
            Hook(60.5f, 8.2f);
            Checkpoint(64f);

            // Muelas que mastican: crúzalas entre mordida y mordida.
            Tip(65f, "¡MUELAS!", "¡Muelas! Bajan a ritmo y aplastan: pasa cuando suban. Biología: las muelas muelen la comida.", FloorY, Signpost.Kind.Warning);
            Molar(70f, 0f);
            Molar(75.5f, 0.6f);
            Molar(81f, 1.2f);
            Molar(86.5f, 1.8f);
            EnemyAt(prefabs.SMutans, 92f, FloorY);
            Checkpoint(94f);

            // La lengua: un piso que se ondula sobre un charco de saliva (y ganchos por si te caes).
            Tip(95f, "LENGUA", "La LENGUA se mueve: sus pedazos suben y bajan. Si te resbalas, engánchate a un diente.");
            Tongue(97f, 119f, 6);
            Hook(103f, 8.4f);
            Hook(113f, 8.4f);
            Checkpoint(120f);

            // Arena de placa bajo la lluvia de azúcar, con olas de saliva que ayudan.
            Sugar(121f, 143f);
            Saliva(119f, 146f, 12f);
            Arena("Arena_2_Placa", 124f, 22f, "¡La placa! Rompe los DULCES antes de que el S. mutans se los coma y crezca. La SALIVA te ayuda.",
                W(0.6f, E(prefabs.SMutans, data.SMutans, 3, SpawnSide.Both)),
                W(1.0f, E(prefabs.SMutans, data.SMutans, 3, SpawnSide.Both)),
                W(1.0f, E(prefabs.SMutans, data.SMutans, 4, SpawnSide.Both)));
            Platform(128f, 2.4f, 2.4f);
            Platform(134f, 3.8f, 2.4f);
            Platform(140f, 2.4f, 2.4f);
            Checkpoint(146f);

            // Jefe: el Sarro. Ganchos en el techo para esquivar las ondas columpiándote.
            Arena("Arena_Sarro", 150f, 22f, null,
                W(1.2f, E(prefabs.SarroJefe, data.SarroJefe, 1)));
            Hook(152f, 8f);
            Hook(160f, 8.4f);
            Hook(167f, 8f);

            SetupUtils.SetString(manager, "completeMessage", "NODO: ¡Boca limpia! Recuerda: cepillo, hilo dental y poca azúcar.");
            SetupUtils.SetFloat(manager, "completeDelay", 4f);
        }

        private void BuildAmigdalas()
        {
            // Nivel secreto (150 u): CARGAR ANTÍGENOS al centro germinal para fabricar linfocitos B aliados que disparan
            // anticuerpos. Jefe: el Absceso (solo le dañan los anticuerpos). Premio: linfocito aliado en todos los niveles.
            LevelManager manager = BuildLevelBase(150f, new LevelStyle
            {
                background = new Color(0.16f, 0.03f, 0.07f),
                wall = new Color(0.5f, 0.12f, 0.22f),
                floor = new Color(0.9f, 0.45f, 0.55f),
                floorEdge = new Color(1f, 0.75f, 0.8f),
                blob = new Color(1f, 0.6f, 0.68f, 0.7f),
                decor = Decor.Blobs,
                drip = new Color(1f, 0.9f, 0.95f, 0.6f),
                glow = new Color(1f, 0.8f, 0.85f, 1f),
                backdrop = "Fondo_Amigdalas",
                tissue = "Boca",
                pitLiquid = new Color(1f, 1f, 1f, 1f),
                liquidDeep = new Color(0.55f, 0.4f, 0.6f, 0.9f),
                liquidSurface = new Color(0.9f, 0.8f, 0.95f, 0.75f),
                liquidOutline = new Color(0.25f, 0.15f, 0.3f, 1f),
                liquidCrest = new Color(1f, 1f, 1f, 0.95f)
            }, "NIVEL SECRETO · AMÍGDALAS. NODO: Estreptococos en la garganta. Aquí no basta con disparar: hay que fabricar anticuerpos.",
                new Vector2(30f, 3f), new Vector2(52f, 3.5f));

            Antigens(data.SPyogenes);

            Arena("Arena_1_Garganta", 6f, 14f, "S. pyogenes (faringitis): al morir suelta un ANTÍGENO. Recógelo.",
                W(0.6f, E(prefabs.SPyogenes, data.SPyogenes, 2)));
            Platform(10f, 2.4f, 2.4f);
            Germinal(22f);

            // Amígdala izquierda: criptas (túneles) llenas de estreptococos.
            Tip(26f, "CRIPTAS", "Amígdala izquierda: sus CRIPTAS (pliegues) atrapan gérmenes. Elimínalos y lleva sus antígenos al centro germinal.");
            Platform(31.5f, 2.4f, 1.6f);
            EnemyAt(prefabs.SPyogenes, 35f, FloorY);
            Platform(38f, 3.6f, 3f);
            EnemyAt(prefabs.SPyogenes, 41f, FloorY);
            SolidBlock(43f, 45f, FloorY + 1.4f);
            Germinal(48f);
            Platform(53.75f, 2.4f, 1.6f);
            EnemyAt(prefabs.SPyogenes, 57f, FloorY);
            EnemyAt(prefabs.Rinovirus, 60f, FloorY + 2.5f);
            Checkpoint(63f);

            // Amígdala derecha: oleadas que atacan; el centro germinal en medio de la pelea.
            Arena("Arena_2_AmigdalaDerecha", 70f, 22f, "¡Oleada! Usa los antígenos para fabricar linfocitos B mientras peleas.",
                W(0.6f, E(prefabs.SPyogenes, data.SPyogenes, 3, SpawnSide.Both)),
                W(1.0f, E(prefabs.SPyogenes, data.SPyogenes, 2), E(prefabs.Rinovirus, data.Rinovirus, 2)),
                W(1.0f, E(prefabs.SPyogenes, data.SPyogenes, 4, SpawnSide.Both)));
            Germinal(78f);
            Platform(73f, 2.6f, 2.4f);
            Platform(84f, 2.6f, 2.4f);
            Checkpoint(93f);

            EnemyAt(prefabs.SPyogenes, 100f, FloorY);
            Platform(104f, 2.4f, 2.4f);
            EnemyAt(prefabs.SPyogenes, 108f, FloorY);
            Checkpoint(111f);

            // Jefe: el Absceso. Solo le hacen daño los anticuerpos.
            Arena("Arena_Absceso", 116f, 26f, null,
                W(1.2f, E(prefabs.AbscesoJefe, data.AbscesoJefe, 1)));
            Germinal(118f);
            Platform(123f, 2.6f, 2.4f);
            Platform(129f, 3.8f, 2.4f);

            SetupUtils.SetString(manager, "completeMessage", "NODO: Garganta sana. Tu linfocito B te acompañará en todos los niveles.");
            SetupUtils.SetFloat(manager, "completeDelay", 4f);
        }

        private void BuildOjo()
        {
            // Nivel secreto (165 u): SIGILO. Que el cuerpo no te vea: vigilantes con cono de visión, barra de inflamación
            // (uveítis), escondites, moscas volantes, retina que se enciende y haces de luz. Jefe: Toxoplasma en quistes.
            // Premio: el escáner de NODO (ves la debilidad de cada enemigo en todos los niveles).
            LevelManager manager = BuildLevelBase(165f, new LevelStyle
            {
                background = new Color(0.03f, 0.05f, 0.12f),
                wall = new Color(0.1f, 0.14f, 0.3f),
                floor = new Color(0.75f, 0.35f, 0.3f),
                floorEdge = new Color(1f, 0.6f, 0.45f),
                blob = new Color(0.4f, 0.55f, 0.9f, 0.35f),
                decor = Decor.Blobs,
                drip = new Color(0.7f, 0.85f, 1f, 0.5f),
                glow = new Color(0.7f, 0.85f, 1f, 1f),
                backdrop = "Fondo_Ojo",
                tissue = "Cerebro",
                pitLiquid = new Color(0.7f, 0.85f, 1f, 1f),
                liquidDeep = new Color(0.25f, 0.35f, 0.6f, 0.9f),
                liquidSurface = new Color(0.6f, 0.75f, 1f, 0.75f),
                liquidOutline = new Color(0.1f, 0.15f, 0.35f, 1f),
                liquidCrest = new Color(1f, 1f, 1f, 0.95f)
            }, "NIVEL SECRETO · OJO. NODO: Entramos por el nervio óptico. Aquí las defensas casi no entran... y si nos ven, el ojo se inflama.");

            Stealth();

            // Córnea y humor acuoso: tutorial de esconderse.
            Tip(4f, "SIGILO", "Ese VIGILANTE es un macrófago del ojo. Si te ve en su cono de luz, sube la inflamación. Agáchate ({abajo}) detrás de los pliegues.");
            Cover(11f);
            WatcherAt(14f, 24f);
            Cover(19f);
            Cover(26f);
            Platform(30f, 2.4f, 2.4f);

            // Cristalino: la lente gigante; luz que entra y barre.
            Tip(32f, "CRISTALINO", "El CRISTALINO enfoca la luz. Cuidado con el haz que barre: si te alumbra, te ven.");
            Lens(41f);
            Beam(33f, 49f, 5f);
            Cover(35f);
            WatcherAt(37f, 46f);
            Cover(44f);
            Checkpoint(51f);

            // Humor vítreo: moscas volantes que se mueven y te tapan.
            Tip(52f, "MOSCAS VOLANTES", "Humor vítreo: esas manchas que flotan son MOSCAS VOLANTES (sí, las que a veces ves). Te tapan aunque estés de pie.");
            Floater(56f, 0f, 2f, 7f);
            WatcherAt(54f, 66f);
            Floater(62f, 0f, 2.5f, 8f);
            Floater(69f, 0f, 2f, 6f);
            WatcherAt(66f, 80f);
            Floater(75f, 0f, 2.5f, 7.5f);
            EnemyAt(prefabs.Herpes, 72f, FloorY + 3f);
            Checkpoint(82f);

            // Retina: conos y bastones que se encienden al pisarlos + haces de luz. Ve por arriba.
            Tip(83f, "RETINA", "RETINA: conos y bastones captan la luz y se encienden si los pisas (te delatan). Ve por las plataformas.");
            Retina(84f, 111f);
            Platform(87f, 2.2f, 2.2f);
            Platform(91.5f, 3.2f, 2.2f);
            Platform(96f, 2.2f, 2.2f);
            Platform(100.5f, 3.2f, 2.2f);
            Platform(105f, 2.2f, 2.2f);
            Platform(109.5f, 3f, 2.2f);
            Beam(84f, 97f, 4.5f);
            Beam(98f, 111f, 5.5f);
            WatcherAt(90f, 106f);

            // Punto ciego: zona segura.
            BlindSpotAt(118f, 9f);
            Sign(Signpost.Kind.Info, 117.6f, FloorY, "PUNTO CIEGO");
            Checkpoint(115f);

            // Jefe: Toxoplasma en los quistes de la retina.
            var layout = Node("Quistes", 0f, FloorY).AddComponent<CystLayout>();
            layout.Configure(new[] { new Vector2(133f, FloorY + 1.4f), new Vector2(140f, FloorY + 4.4f), new Vector2(147f, FloorY + 1.4f), new Vector2(152f, FloorY + 4.6f) });
            Arena("Arena_Toxoplasma", 130f, 28f, null,
                W(1.2f, E(prefabs.ToxoplasmaJefe, data.ToxoplasmaJefe, 1)));
            Platform(140f, 3.2f, 2.6f);
            Platform(152f, 3.4f, 2.6f);
            Cover(136f);
            Cover(144f);
            Cover(149f);

            SetupUtils.SetString(manager, "completeMessage", "NODO: Ojo a salvo sin inflamarlo. El escáner queda activo en todos los niveles.");
            SetupUtils.SetFloat(manager, "completeDelay", 4f);
        }

        // ---------- Elementos del rediseño del Respiratorio y del Nervioso ----------

        private const string RespArt = "Assets/_Project/Art/Respiratorio";
        private const string NerveArt = "Assets/_Project/Art/Nervioso";

        private GameObject Node(string name, float x, float y)
        {
            var go = new GameObject(name);
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(x, y, 0f);
            return go;
        }

        /// <summary>Punto de regreso invisible (si mueres o caes después de aquí, regresas aquí).</summary>
        private void Checkpoint(float x) => Node("PuntoDeRegreso", x, FloorY).AddComponent<CheckpointTrigger>();

        /// <summary>Glándula de la mucosa que lanza bolas de moco (te frenan; las que caen sirven de plataforma).</summary>
        private void MucusGland(float x, float period, float offset, int facing = -1)
        {
            Node("GlandulaMoco", x, FloorY).AddComponent<MucusCannon>().Configure(period, offset, facing,
                PrefabSetup.ArtByWidth($"{RespArt}/GlandulaMoco.png", 2.2f, new Vector2(0.5f, 0.03f)),
                PrefabSetup.ArtByWidth($"{RespArt}/BolaMoco.png", 0.8f));
        }

        /// <summary>Zona de estornudo: barra de comezón que se llena (más rápido con polen vivo) y ¡ACHÚ!</summary>
        private void Sneeze(float fromX, float toX)
        {
            Node("Estornudo", (fromX + toX) * 0.5f, FloorY).AddComponent<WindBlast>().Configure(fromX, toX, true, data.Polen, 0f, -1,
                "¡A... a... aaa...!", "¡¡ACHÚ!! El estornudo saca el polen a la fuerza. Agárrate de los pelos de la nariz.");
        }

        /// <summary>Zona de tos (tráquea): cada 'period' segundos tose y te avienta hacia atrás.</summary>
        private void Cough(float fromX, float toX, float period)
        {
            Node("Tos", (fromX + toX) * 0.5f, FloorY).AddComponent<WindBlast>().Configure(fromX, toX, false, null, period, -1,
                "La tráquea se contrae... ¡va a toser!", "¡COF! La tos saca lo que estorba en la tráquea. Cúbrete tras un anillo de cartílago.");
        }

        private void NasalHairAt(float x) =>
            Node("PelosNasales", x, FloorY).AddComponent<NasalHair>().Configure(3.4f, PrefabSetup.ArtByWidth($"{RespArt}/PeloNasal.png", 2.1f, new Vector2(0.5f, 0.02f)));

        /// <summary>Cornete (pliegue de hueso de la nariz): montículo sólido; detrás de él el estornudo no te avienta.</summary>
        private void Turbinate(float x, float width, float height)
        {
            SolidBlock(x - width * 0.5f, x + width * 0.5f, FloorY + height);
            Node("RefugioCornete", x - width * 0.5f - 0.7f, FloorY).AddComponent<WindAnchor>().Configure(1.4f, height + 0.6f);
        }

        /// <summary>Anillo de cartílago de la tráquea: arco de adorno; párate debajo y la tos no te avienta.</summary>
        private void CartilageRing(float x)
        {
            GameObject go = Node("AnilloCartilago", x, FloorY);
            go.AddComponent<WindAnchor>().Configure(1.8f, 4f);
            Sprite ring = PrefabSetup.ArtByWidth($"{RespArt}/AnilloCartilago.png", 3.6f, new Vector2(0.5f, 0.02f));
            if (ring != null) { SetupUtils.SpriteChild(go.transform, "Anillo", ring, Color.white, Vector3.zero, Vector2.one, -40); return; }
            Color line = new Color(0.12f, 0.08f, 0.28f), fill = new Color(0.78f, 0.72f, 0.95f);
            for (int s = -1; s <= 1; s += 2)
            {
                SetupUtils.SpriteChild(go.transform, "Pilar", PlaceholderArt.Square, line, new Vector3(s * 1.1f, 0f, 0f), new Vector2(0.56f, 4.2f), -41);
                SetupUtils.SpriteChild(go.transform, "PilarLuz", PlaceholderArt.Square, fill, new Vector3(s * 1.1f, 0.05f, 0f), new Vector2(0.4f, 4.1f), -40);
            }
            SetupUtils.SpriteChild(go.transform, "Arco", PlaceholderArt.Circle, line, new Vector3(0f, 4.2f, 0f), new Vector2(2.8f, 1.1f), -41);
            SetupUtils.SpriteChild(go.transform, "ArcoLuz", PlaceholderArt.Circle, fill, new Vector3(0f, 4.2f, 0f), new Vector2(2.6f, 0.9f), -40);
            SetupUtils.SpriteChild(go.transform, "Hueco", PlaceholderArt.Circle, currentStyle.background, new Vector3(0f, 4.05f, 0f), new Vector2(1.7f, 0.6f), -39);
        }

        private void Cilia(float fromX, float toX, float push, float period) =>
            Node("EscaleraDeCilios", fromX, FloorY).AddComponent<CiliaConveyor>().Configure(fromX, toX, FloorY, push, period,
                PrefabSetup.ArtByWidth($"{RespArt}/BolaMoco.png", 1f));

        private void Vent(float x, float launch, float period, float phase) =>
            Node("CorrienteDeAire", x, FloorY).AddComponent<AirVent>().Configure(1.6f, launch, period, phase);

        private void Epiglottis(float x, float open, float closed, float phase) =>
            Node("Epiglotis", x, FloorY).AddComponent<TimedGate>().Configure(open, closed, phase, 9f,
                PrefabSetup.ArtByWidth($"{RespArt}/Epiglotis.png", 2f, new Vector2(0.5f, 0.99f)));

        private void Breathing(float fromX, float toX) =>
            Node("Respiracion", fromX, FloorY).AddComponent<BreathingCycle>().Configure(fromX, toX);

        private void CO2(float x, float y) =>
            Node("NubeCO2", x, FloorY + y).AddComponent<CO2Cloud>().Configure(1.3f, 2.2f, PrefabSetup.ArtByWidth($"{RespArt}/CO2.png", 3f));

        private void O2(float x, float y) =>
            Node("BurbujaO2", x, FloorY + y).AddComponent<OxygenBubble>().Configure(8f, PrefabSetup.ArtByWidth($"{RespArt}/O2.png", 0.9f));

        /// <summary>Bomba de regalo (bolsa de medicamento) en una ruta difícil.</summary>
        private void BombGift(float x, float y, string text)
        {
            GameObject go = Node("Regalo", x, FloorY + y);
            GameObject art = SetupUtils.Child(go.transform, "Dibujo", Vector3.zero);
            SetupUtils.SpriteChild(art.transform, "Contorno", PlaceholderArt.Circle, new Color(0.1f, 0.15f, 0.3f), Vector3.zero, new Vector2(0.9f, 0.9f), 20);
            SetupUtils.SpriteChild(art.transform, "Bomba", PlaceholderArt.Circle, new Color(0.55f, 0.9f, 1f), Vector3.zero, new Vector2(0.76f, 0.76f), 21);
            SetupUtils.SpriteChild(art.transform, "Cruz", PlaceholderArt.SquareCentered, Color.white, Vector3.zero, new Vector2(0.4f, 0.12f), 22);
            SetupUtils.SpriteChild(art.transform, "Cruz", PlaceholderArt.SquareCentered, Color.white, Vector3.zero, new Vector2(0.12f, 0.4f), 22);
            SetupUtils.SpriteChild(go.transform, "Aura", PlaceholderArt.Circle, new Color(0.6f, 0.9f, 1f, 0.3f), Vector3.zero, new Vector2(1.6f, 1.6f), 19);
            go.AddComponent<HealPickup>().Configure(art.transform, 15f, 1, text);
        }

        /// <summary>Viaje que espera a que te subas (glóbulo rojo o impulso nervioso).</summary>
        private void Ride(string name, Vector2 from, Vector2 to, float speed, float width, MovingPlatform.Style style, float wave, string message, Sprite art)
        {
            GameObject go = Node(name, from.x, from.y);
            var p = go.AddComponent<MovingPlatform>();
            p.Configure(from, to, speed, 0f, width, art);
            p.ConfigureRide(style, wave, message);
        }

        /// <summary>Capilar: hueco lleno de sangre (rojo) que se cruza en un glóbulo rojo.</summary>
        private void BloodPit(float fromX, float width)
        {
            SetupUtils.SpriteChild(environment, "Sangre", PlaceholderArt.Square, new Color(0.45f, 0.02f, 0.08f, 0.95f),
                new Vector3(fromX + width * 0.5f, FloorY - 4f, 0f), new Vector2(width, 3.3f), -94);
            SetupUtils.SpriteChild(environment, "SangreSuperficie", PlaceholderArt.Square, new Color(0.9f, 0.2f, 0.25f, 1f),
                new Vector3(fromX + width * 0.5f, FloorY - 0.95f, 0f), new Vector2(width, 0.2f), -93);
            for (int i = 0; i < 5; i++)
                SetupUtils.SpriteChild(environment, "GlobuloLejano", PlaceholderArt.Circle, new Color(0.75f, 0.12f, 0.16f, 0.8f),
                    new Vector3(fromX + width * (0.12f + i * 0.19f), FloorY - 1.6f - (i % 2) * 0.5f, 0f), new Vector2(0.9f, 0.45f), -92);
        }

        private void SaltatoryAt(float stepTime, params Vector2[] points)
        {
            var world = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++) world[i] = new Vector2(points[i].x, FloorY + points[i].y);
            Node("ConduccionSaltatoria", points.Length > 0 ? points[0].x : 0f, FloorY).AddComponent<SaltatoryNodes>().Configure(world, stepTime, 1.5f);
        }

        private void Reflex(float x, float y, float launch) =>
            Node("ArcoReflejo", x, FloorY + y).AddComponent<ReflexPad>().Configure(1.8f, launch);

        private void Darkness(float fromX, float toX) =>
            Node("LobuloOccipital", fromX, FloorY).AddComponent<DarknessZone>().Configure(fromX, toX);

        private void Beat(Vector3[] a, Vector3[] b, float beatTime)
        {
            for (int i = 0; i < a.Length; i++) a[i].y += FloorY;
            for (int i = 0; i < b.Length; i++) b[i].y += FloorY;
            Node("LobuloTemporal", a.Length > 0 ? a[0].x : 0f, FloorY).AddComponent<BeatPlatforms>().Configure(a, b, beatTime);
        }

        private void Tilt(float x, float y, float width) =>
            Node("Subibaja", x, FloorY + y).AddComponent<TiltPlatform>().Configure(width, 14f);

        private void FrontalPuzzle(float gateX, int[] order, params Vector2[] neurons)
        {
            var world = new Vector2[neurons.Length];
            for (int i = 0; i < neurons.Length; i++) world[i] = new Vector2(neurons[i].x, FloorY + neurons[i].y);
            Node("LobuloFrontal", gateX, FloorY).AddComponent<SequencePuzzle>().Configure(world, order, gateX);
        }

        private void MemoryBubble(float x, float y, float width) =>
            Node("BurbujaMemoria", x, FloorY + y).AddComponent<FadingPlatform>().Configure(width, 1.4f);

        private void Fragment(float x, float y, string text) =>
            Node("FragmentoMemoria", x, FloorY + y).AddComponent<MemoryFragment>().Configure(text, PrefabSetup.ArtByWidth($"{NerveArt}/FragmentoMemoria.png", 0.8f));

        private void Water(float x, float y) =>
            Node("BurbujaAgua", x, FloorY + y).AddComponent<WaterBubble>().Configure(4f, 3f, PrefabSetup.ArtByWidth($"{NerveArt}/BurbujaAgua.png", 1.1f));

        // ---------- Sistema Nervioso (GDD 5.12, 5.14, 5.15: tormentas eléctricas, neuronas sanas y salud del cerebro) ----------
        // Paleta de las imágenes de referencia: violeta profundo, bordes azul eléctrico, neuronas azules,
        // neuronas infectadas rojas, chispas doradas de sinapsis y rayos violetas.

        private static readonly Color NerveGlow = new Color(0.35f, 0.7f, 1f, 1f);

        private void Storm(float fromX, float toX)
        {
            var storm = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Storm.gameObject);
            storm.transform.position = new Vector3((fromX + toX) * 0.5f, FloorY, 0f);
            var component = storm.GetComponent<LightningStorm>();
            SetupUtils.SetFloat(component, "minX", fromX);
            SetupUtils.SetFloat(component, "maxX", toX);
            SetupUtils.SetFloat(component, "floorY", FloorY);
        }

        private void Neuron(float x, float y)
        {
            var neuron = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Neuron.gameObject);
            neuron.transform.position = new Vector3(x, FloorY + y, 0f);
        }

        /// <summary>Riel de axón (solo visual): por donde corre el jefe rabia.</summary>
        private void AxonRail(float fromX, float toX, float height)
        {
            float w = toX - fromX;
            SetupUtils.SpriteChild(environment, "Axon", PlaceholderArt.Capsule, new Color(0.35f, 0.6f, 1f, 0.35f),
                new Vector3(fromX + w * 0.5f, FloorY + height, 0f), new Vector2(w, 0.3f), -141);
            SetupUtils.SpriteChild(environment, "AxonBrillo", PlaceholderArt.Capsule, new Color(0.7f, 0.9f, 1f, 0.6f),
                new Vector3(fromX + w * 0.5f, FloorY + height, 0f), new Vector2(w, 0.08f), -140);
            for (float x = fromX + 1f; x < toX; x += 2.5f) // vainas de mielina
                SetupUtils.SpriteChild(environment, "Mielina", PlaceholderArt.Capsule, new Color(0.55f, 0.45f, 0.9f, 0.45f),
                    new Vector3(x, FloorY + height, 0f), new Vector2(1.6f, 0.5f), -139);
        }

        private void BuildHipocampo()
        {
            // Rediseño (150 u): burbujas de memoria que se desvanecen si te quedas quieto, fragmentos de memoria que la
            // rabia borra si tardas, y el jefe rabia con HIDROFOBIA (burbujas de agua que lo paralizan). Ver LEEME "Hipocampo".
            LevelManager manager = BuildLevelBase(150f, new LevelStyle
            {
                background = new Color(0.05f, 0.02f, 0.1f),
                wall = new Color(0.22f, 0.07f, 0.3f),
                floor = new Color(0.24f, 0.12f, 0.42f),
                floorEdge = NerveGlow,
                blob = new Color(0.45f, 0.3f, 0.8f, 0.3f),
                decor = Decor.Neurons,
                detail = new Color(0.35f, 0.65f, 1f, 0.7f),
                glow = new Color(0.85f, 0.55f, 1f, 1f),
                platformStems = true
                ,backdrop = "Fondo_Hipocampo", // pintura estilo caricatura (neuronas y burbujas de memoria)
                tissue = "Hipocampo",         // tejido magenta-orquídea con destellos dorados
                pitLiquid = new Color(1f, 1f, 1f, 1f), // huecos con líquido dorado (memorias)
                liquidDeep = new Color(0.45f, 0.22f, 0.08f, 0.95f),
                liquidSurface = new Color(0.98f, 0.72f, 0.3f, 0.82f),
                liquidOutline = new Color(0.22f, 0.06f, 0.12f, 1f),
                liquidCrest = new Color(1f, 0.92f, 0.65f, 0.95f)
            }, "NODO: Hipocampo, el centro de la memoria. Algo viene corriendo por los nervios...",
                new Vector2(20f, 3.5f), new Vector2(34f, 12f), new Vector2(52f, 14f));

            new GameObject("SaludDelCerebro").AddComponent<BrainHealth>();

            Arena("Arena_Entrada", 4f, 14f, "Prepárate: el jefe rabia cae con Antiviral; Anticonvulsivo para las neuronas rojas y la rabia suelta.",
                W(0.6f, E(prefabs.Rabia, data.Rabia, 2), E(prefabs.Poliovirus, data.Poliovirus, 1)));
            Neuron(10f, 3.2f);
            RedNeuron(15f, 1.5f);
            Platform(8f, 2.4f, 2.4f);
            Platform(21.75f, 2.4f, 1.6f);
            Storm(18f, 30f);

            Tip(17f, "RECUERDOS", "Recoge los FRAGMENTOS DE MEMORIA antes de que la rabia los borre: al acercarte se empiezan a apagar. Si salvas todos, ganas una bomba.");
            Fragment(25.5f, 2.5f, "el hipocampo convierte lo que vives en recuerdos que duran años.");
            Checkpoint(31f);

            // Burbujas de memoria sobre dos huecos.
            Tip(31f, "BURBUJAS", "Burbujas de MEMORIA: aguantan mientras te muevas; si te quedas quieto encima se olvidan y revientan (luego se vuelven a formar).");
            MemoryBubble(36.5f, 1.8f, 2.2f);
            MemoryBubble(40f, 2.8f, 2f);
            Fragment(40f, 4.6f, "si se daña, recuerdas tu infancia pero no lo que pasó ayer.");
            MemoryBubble(43.5f, 1.8f, 2.2f);
            Fragment(49f, 1.5f, "también es tu GPS: recuerda caminos y lugares.");
            MemoryBubble(54.5f, 2f, 2.2f);
            MemoryBubble(58f, 3.2f, 2f);
            Fragment(58f, 5f, "tiene forma de caballito de mar: 'hippocampus' en griego.");
            MemoryBubble(61.5f, 2f, 2.2f);
            MemoryBubble(64.5f, 3f, 1.8f);
            Checkpoint(67f);

            // Arena chica entre recuerdos.
            Arena("Arena_Recuerdos", 72f, 16f, "¡Te quieren quitar los recuerdos! Anticonvulsivo a la rabia suelta, Antiviral al poliovirus.",
                W(0.6f, E(prefabs.Rabia, data.Rabia, 2), E(prefabs.Poliovirus, data.Poliovirus, 1)),
                W(1.0f, E(prefabs.Rabia, data.Rabia, 2, SpawnSide.Left), E(prefabs.Herpes, data.Herpes, 1)));
            Platform(75f, 2.4f, 2.4f);
            Platform(80f, 3.8f, 2.4f);
            Fragment(80f, 5.4f, "hay uno en cada lado del cerebro.");
            Neuron(77f, 1.6f);

            Storm(88f, 99f);
            Platform(91f, 2.4f, 2.2f);
            Platform(95f, 3.8f, 2.2f);
            Fragment(95f, 5.3f, "dormir bien y hacer ejercicio le ayudan a formar neuronas nuevas.");
            Checkpoint(100f);

            // Arena del jefe: rieles de axones, neuronas sanas y BURBUJAS DE AGUA (hidrofobia).
            const float arenaX = 104f, arenaW = 34f;
            float left = arenaX - 3f, right = arenaX - 3f + arenaW;
            AxonRail(left, right, 6.2f);
            AxonRail(left, right, 4.4f);
            AxonRail(left, right, 2.6f);
            Arena("Arena_Rabia", arenaX, arenaW, "La rabia le tiene pánico al AGUA (hidrofobia): revienta las burbujas de agua cerca de ella para paralizarla.",
                W(1.2f, E(prefabs.RabiaJefe, data.RabiaJefe, 1)));
            Neuron(left + 4f, 3.4f);
            Neuron(left + 11f, 5.3f);
            Neuron(left + 18f, 3.4f);
            Neuron(left + 25f, 5.3f);
            Neuron(left + 31f, 3.4f);
            Platform(left + 7f, 2.4f, 2.6f);
            Platform(left + 14f, 4.2f, 2.6f);
            Platform(left + 21f, 2.4f, 2.6f);
            Platform(left + 28f, 4.2f, 2.6f);
            Water(left + 4f, 5.3f);
            Water(left + 11f, 3.5f);
            Water(left + 18f, 5.3f);
            Water(left + 25f, 3.5f);
            Water(left + 31f, 5.3f);

            SetupUtils.SetString(manager, "completeMessage",
                "NODO: Hipocampo a salvo. Fórmula 3 enviada a Aurelis. La infección huyó hacia el corazón...");
            SetupUtils.SetFloat(manager, "completeDelay", 4.5f);
        }

        private void RedNeuron(float x, float y)
        {
            var neuron = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.RedNeuron.gameObject);
            neuron.transform.position = new Vector3(x, FloorY + y, 0f);
        }

        private void BuildMedula()
        {
            // Rediseño (170 u): tormentas, ARCO REFLEJO (te lanza), surfear el IMPULSO NERVIOSO por un axón y la
            // CONDUCCIÓN SALTATORIA (nodos de Ranvier que se encienden en orden). Ver LEEME "Médula".
            BuildLevelBase(170f, new LevelStyle
            {
                background = new Color(0.05f, 0.02f, 0.12f),
                wall = new Color(0.2f, 0.08f, 0.32f),
                floor = new Color(0.22f, 0.14f, 0.42f),
                floorEdge = NerveGlow,
                blob = new Color(0.45f, 0.3f, 0.75f, 0.3f), // vértebras / haces de fibras
                decor = Decor.Rings,
                glow = new Color(0.65f, 0.45f, 1f, 1f),
                platformStems = true
                ,backdrop = "Fondo_Medula",  // pintura estilo caricatura (fibras con mielina y vértebras)
                tissue = "Medula",            // tejido índigo con brillo turquesa
                pitLiquid = new Color(1f, 1f, 1f, 1f), // huecos con líquido cefalorraquídeo turquesa
                liquidDeep = new Color(0.06f, 0.24f, 0.42f, 0.95f),
                liquidSurface = new Color(0.3f, 0.82f, 0.92f, 0.8f),
                liquidOutline = new Color(0.03f, 0.08f, 0.2f, 1f),
                liquidCrest = new Color(0.75f, 1f, 1f, 0.95f)
            }, "NODO: Sistema nervioso. Entraste por la médula espinal: la infección sube hacia el cerebro.",
                new Vector2(30f, 3.5f), new Vector2(44f, 4f), new Vector2(68f, 14f), new Vector2(88f, 18f), new Vector2(146f, 4f));

            Platform(2f, 2.4f, 3f);

            Arena("Arena_1_Meningitis", 8f, 18f, "N. meningitidis (meningitis): se agazapa y cruza a toda velocidad. Antibiótico.",
                W(0.6f, E(prefabs.Meningococo, data.Meningococo, 2)),
                W(1.0f, E(prefabs.Poliovirus, data.Poliovirus, 2), E(prefabs.Meningococo, data.Meningococo, 1, SpawnSide.Left)));
            Platform(10f, 2.4f, 2.6f);
            Platform(16f, 3.8f, 2.6f);
            Platform(21f, 2.4f, 2.6f);

            // Primera tormenta.
            Tip(25f, "¡TORMENTA!", "Tormenta eléctrica: cuando veas el destello violeta, sal de la columna. El escudo NO protege.", FloorY, Signpost.Kind.Warning);
            Storm(25f, 50f);
            Platform(31.75f, 2.4f, 1.6f);
            Platform(37f, 2.4f, 2.4f);
            Platform(40.5f, 4f, 2.4f);
            Platform(44f, 5.6f, 2.4f);
            Platform(47.5f, 4f, 2.4f);
            Checkpoint(52f);

            // Arco reflejo: la médula responde sola y te lanza sobre la pared.
            Reflex(56.5f, 0f, 7f);
            SolidBlock(60f, 64f, FloorY + 5.6f);

            // Surfear el impulso nervioso por un axón (hueco largo).
            Tip(65f, "IMPULSO", "Súbete al IMPULSO NERVIOSO: corre por el axón a más de 100 km/h. ¡No te caigas!", FloorY, Signpost.Kind.ArrowRight);
            AxonRail(66f, 84f, 1.6f);
            Ride("Impulso", new Vector2(66.8f, FloorY + 2.2f), new Vector2(83.6f, FloorY + 2.2f), 6.5f, 2.2f,
                MovingPlatform.Style.Impulse, 1.2f, null, PrefabSetup.ArtByWidth($"{NerveArt}/Impulso.png", 2.4f));
            Checkpoint(85f);

            // Conducción saltatoria: los nodos de Ranvier se encienden en orden; solo el encendido es sólido.
            Tip(85f, "SALTATORIA", "Conducción SALTATORIA: el impulso salta de nodo en nodo (nodos de Ranvier) y por eso es tan rápido. Solo el nodo ENCENDIDO es sólido: síguelo.");
            SaltatoryAt(0.9f, new Vector2(89.5f, 1.8f), new Vector2(93f, 2.6f), new Vector2(96.5f, 1.8f), new Vector2(100f, 2.6f), new Vector2(103.5f, 1.8f));
            Checkpoint(107f);

            Arena("Arena_2_Poliovirus", 114f, 20f, "Poliovirus: sus disparos te paralizan un momento. Antiviral. La rabia muerde en ráfaga: Anticonvulsivo. ¡Pelea bajo la tormenta!",
                W(0.6f, E(prefabs.Rabia, data.Rabia, 2), E(prefabs.Meningococo, data.Meningococo, 2, SpawnSide.Left)),
                W(1.0f, E(prefabs.Poliovirus, data.Poliovirus, 2), E(prefabs.Rabia, data.Rabia, 1)));
            Storm(110f, 133f);
            Platform(117f, 2.4f, 2.6f);
            Platform(123f, 3.8f, 2.6f);
            Platform(129f, 2.4f, 2.6f);
            Checkpoint(134f);

            // Segundo arco reflejo sobre una pared y luego un hueco.
            Reflex(137f, 0f, 7f);
            SolidBlock(140f, 143.5f, FloorY + 5.8f);
            Platform(148f, 2.4f, 1.6f);

            // Recta final bajo la tormenta.
            Storm(152f, 165f);
            Platform(155f, 2.4f, 2.4f);
            Platform(159f, 3.8f, 2.4f);
            Platform(163f, 2.4f, 2.4f);
        }

        private void BuildCerebro()
        {
            // Rediseño (190 u): cada lóbulo tiene su regla. Occipital (vista): oscuridad con destellos. Temporal (oído):
            // plataformas al ritmo. Cerebelo (equilibrio): subibajas. Frontal (planear): neuronas en orden. Ver LEEME "Cerebro".
            LevelManager manager = BuildLevelBase(190f, new LevelStyle
            {
                background = new Color(0.04f, 0.02f, 0.1f),
                wall = new Color(0.16f, 0.07f, 0.3f),
                floor = new Color(0.2f, 0.12f, 0.4f),
                floorEdge = NerveGlow,
                blob = new Color(0.4f, 0.3f, 0.8f, 0.3f),
                decor = Decor.Neurons,
                detail = new Color(0.35f, 0.65f, 1f, 0.7f),
                glow = new Color(0.75f, 0.5f, 1f, 1f), // luz del hipocampo al final
                platformStems = true
                ,backdrop = "Fondo_Cerebro",  // pintura estilo caricatura (circunvoluciones y red de neuronas)
                tissue = "Cerebro",           // tejido índigo con orilla azul
                pitLiquid = new Color(1f, 1f, 1f, 1f), // huecos con líquido magenta
                liquidDeep = new Color(0.3f, 0.06f, 0.36f, 0.95f),
                liquidSurface = new Color(0.86f, 0.36f, 0.82f, 0.8f),
                liquidOutline = new Color(0.12f, 0.03f, 0.16f, 1f),
                liquidCrest = new Color(1f, 0.8f, 0.96f, 0.95f)
            }, "NODO: Cerebro. Protege las neuronas azules: los disparos ENEMIGOS las dañan y las infectan. Las tuyas las atraviesan.",
                new Vector2(36f, 3f), new Vector2(44f, 3.5f), new Vector2(53f, 3f), new Vector2(66f, 22f), new Vector2(118f, 20f));

            new GameObject("SaludDelCerebro").AddComponent<BrainHealth>();
            Tip(1f, "ANTICONVULSIVO", "Tu cápsula regional es el ANTICONVULSIVO: calma neuronas rojas. Su bomba ({bomba}) protege a las neuronas 8 s.");

            // Tramo 1
            Arena("Tramo_1_Meningitis", 8f, 20f, "Acaba rápido con los virus: si sus disparos pegan en las neuronas, el cerebro sufre y se infectan.",
                W(0.6f, E(prefabs.Meningococo, data.Meningococo, 2), E(prefabs.Herpes, data.Herpes, 1)),
                W(1.0f, E(prefabs.Poliovirus, data.Poliovirus, 2), E(prefabs.Meningococo, data.Meningococo, 1, SpawnSide.Left)));
            Neuron(14f, 2.2f);
            Neuron(19.5f, 3.8f);
            Platform(11f, 2.4f, 2.4f);
            Platform(17f, 4.2f, 2.4f);
            Platform(22f, 2.4f, 2.4f);

            Tip(25f, "NEURONA ROJA", "Neurona ROJA hiperexcitada: suelta descargas y desgasta el cerebro. Cálmala con ANTICONVULSIVO (+5 cerebro).");
            RedNeuron(28f, 1.6f);
            Checkpoint(31f);

            // LÓBULO OCCIPITAL: a oscuras; los destellos de las neuronas muestran el camino.
            Darkness(32f, 62f);
            Platform(40f, 2.4f, 2f);
            Neuron(40f, 4f);
            Platform(49f, 3f, 2.4f);
            EnemyAt(prefabs.Meningococo, 50f, FloorY + 1f);
            // Secreto: el NERVIO ÓPTICO (entrada al Ojo), arriba, sobre el hueco. Se ve con los destellos.
            Platform(52.4f, 2.8f, 1.4f);
            Platform(55.5f, 4.6f, 1.8f);
            Sign(Signpost.Kind.Secret, 51.2f, FloorY, "?");   // pista: algo hay arriba
            SecretEntrance(55.5f, 5.7f, data.NerviosoOjo, "nervioso_ojo",
                "¡El NERVIO ÓPTICO! Por aquí la vista llega al cerebro... y se puede entrar al OJO.",
                "¿Otra vez al ojo? Silencio: que no te vean.", true);
            Neuron(58f, 2.2f);
            Checkpoint(63f);

            // LÓBULO TEMPORAL: plataformas al ritmo sobre un hueco largo.
            Tip(63f, "LÓBULO TEMPORAL", "Lóbulo TEMPORAL: aquí se procesa el sonido y la música. Las plataformas aparecen AL RITMO: escucha el golpe y salta.");
            Beat(new[] { new Vector3(67.5f, 1.8f, 2f), new Vector3(73f, 2.6f, 2f), new Vector3(78.5f, 1.8f, 2f), new Vector3(84f, 2.6f, 2f) },
                 new[] { new Vector3(70.2f, 2.6f, 2f), new Vector3(75.7f, 1.8f, 2f), new Vector3(81.2f, 2.6f, 2f), new Vector3(86.6f, 1.8f, 2f) }, 1.25f);
            Checkpoint(89f);

            // Tramo 2: pelea bajo la tormenta con neuronas en medio.
            Arena("Tramo_2_Encefalitis", 94f, 22f, "Herpes (encefalitis): se vuelve latente (invisible) y reaparece disparando en ráfaga. Antiviral.",
                W(0.6f, E(prefabs.Herpes, data.Herpes, 2), E(prefabs.Rabia, data.Rabia, 2, SpawnSide.Left)),
                W(1.0f, E(prefabs.Meningococo, data.Meningococo, 3, SpawnSide.Both), E(prefabs.Poliovirus, data.Poliovirus, 1)));
            Storm(91f, 114f);
            Neuron(99f, 1.1f);
            Neuron(104.5f, 3.4f);
            Neuron(110f, 1.8f);
            RedNeuron(96.5f, 3.6f);
            Platform(98f, 2.6f, 2.6f);
            Platform(103f, 4.4f, 2.6f);
            Platform(108.5f, 2.6f, 2.6f);
            Checkpoint(115f);

            // CEREBELO: subibajas sobre un hueco.
            Tip(115f, "CEREBELO", "CEREBELO: controla el equilibrio. Las plataformas son SUBIBAJAS: si te vas a la orilla se ladean y te resbalas. Cruza por el centro.");
            Tilt(121f, 2.2f, 4f);
            Tilt(127.5f, 3f, 4f);
            Tilt(134f, 2.2f, 4f);
            Checkpoint(139f);

            // LÓBULO FRONTAL: el circuito de neuronas abre la compuerta.
            FrontalPuzzle(156f, new[] { 2, 0, 3, 1 },
                new Vector2(143f, 3.2f), new Vector2(146.5f, 4.4f), new Vector2(150f, 3.2f), new Vector2(153.5f, 4.4f));
            // Cada neurona tiene su repisa (en el piso no se tocan por accidente).
            Platform(143f, 2.2f, 1.6f);
            Platform(146.5f, 3.4f, 1.6f);
            Platform(150f, 2.2f, 1.6f);
            Platform(153.5f, 3.4f, 1.6f);
            Checkpoint(157f);

            // Tramo 3 (clímax): la corteza.
            Arena("Tramo_3_Corteza", 162f, 24f, "NODO: Última zona antes del hipocampo. Muchos virus apuntando a las neuronas: sé rápido.",
                W(0.6f, E(prefabs.Meningococo, data.Meningococo, 4, SpawnSide.Both)),
                W(1.0f, E(prefabs.Herpes, data.Herpes, 2), E(prefabs.Rabia, data.Rabia, 2, SpawnSide.Left)),
                W(1.2f, E(prefabs.Herpes, data.Herpes, 1), E(prefabs.Poliovirus, data.Poliovirus, 3), E(prefabs.Meningococo, data.Meningococo, 2, SpawnSide.Left)));
            Storm(159f, 183f);
            Neuron(165f, 1.8f);
            Neuron(169.5f, 3.6f);
            Neuron(173.5f, 1.1f);
            Neuron(177.5f, 3.4f);
            Neuron(181f, 2f);
            RedNeuron(167.5f, 4.6f);
            RedNeuron(178.5f, 1.2f);
            Platform(167.5f, 2.6f, 2.4f);
            Platform(172f, 4.4f, 2.4f);
            Platform(176.5f, 2.6f, 2.4f);
            Platform(180.5f, 4.4f, 2.4f);

            SetupUtils.SetString(manager, "completeMessage",
                "NODO: Cerebro despejado... pero algo corre por los nervios hacia el hipocampo.");
            SetupUtils.SetFloat(manager, "completeDelay", 4.5f);
        }

        /// <summary>
        /// Alvéolo. Con 'breathOffset' (segundos) respira (BreathingPlatform: ciclo de 5.5 s, trampolín a 4 u
        /// cuando está inflado); con un valor negativo es fijo. Reglas: 5.5 u libres arriba, repisa destino
        /// a máximo 3.5 u del alvéolo y huecos de máximo 4.5 u.
        /// </summary>
        private void Alveolus(float centerX, float topY, float width, float breathOffset = -1f)
        {
            bool breathing = breathOffset >= 0f;
            const float thickness = 0.35f;
            var go = new GameObject("Alveolo_Plataforma");
            go.transform.SetParent(environment, false);
            go.transform.position = new Vector3(centerX, topY, 0f);
            go.layer = PhysicsLayers.Ground;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, thickness);
            box.offset = new Vector2(0f, -thickness * 0.5f);
            box.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;

            Transform visual = SetupUtils.Child(go.transform, "Visual", Vector3.zero).transform;
            // Los que respiran son más rosados, para distinguirlos de los fijos.
            Color bubble = breathing ? new Color(1f, 0.62f, 0.85f, 0.8f) : new Color(0.9f, 0.75f, 0.95f, 0.75f);
            int count = Mathf.Max(2, Mathf.RoundToInt(width / 0.7f));
            for (int i = 0; i < count; i++)
            {
                float x = -width * 0.5f + (i + 0.5f) * width / count;
                float size = width / count * 1.35f;
                SetupUtils.SpriteChild(visual, "Burbuja" + i, PlaceholderArt.Circle, bubble,
                    new Vector3(x, -size * 0.35f, 0f), new Vector2(size, size), -90);
                SetupUtils.SpriteChild(visual, "Brillo" + i, PlaceholderArt.Circle, new Color(1f, 1f, 1f, 0.5f),
                    new Vector3(x - size * 0.18f, -size * 0.18f, 0f), new Vector2(size * 0.25f, size * 0.25f), -89);
            }
            SetupUtils.SpriteChild(visual, "Borde", PlaceholderArt.Square, currentStyle.glow.a > 0f ? currentStyle.glow : currentStyle.floorEdge,
                new Vector3(0f, -0.08f, 0f), new Vector2(width * 0.9f, 0.08f), -88);

            if (breathing)
            {
                var breath = go.AddComponent<BreathingPlatform>();
                SetupUtils.SetRef(breath, "visual", visual);
                SetupUtils.SetFloat(breath, "phaseOffset", Mathf.Clamp(breathOffset, 0f, 5.5f));
            }
        }

        // ---------- Letreros e indicaciones ----------
        //
        // Cada nivel lleva letreros con el estilo de su sistema:
        //  - Bienvenida con el nombre del órgano y Salida al final (automáticos).
        //  - Info donde se enseña algo nuevo: se ve desde lejos y al llegar NODO lo explica (Tip).
        //  - Peligro antes del primer hueco o lago y de los grandes, y "¡JEFE!" antes de cada jefe (automáticos).
        //  - Flechas cuando el camino sube o baja, y un "?" chiquito cerca de los pasajes secretos.
        // Arte: Art/Letreros/{Digestivo|Respiratorio|Nervioso}/Letrero_{Bienvenida|Info|Peligro|FlechaDerecha|
        // FlechaArriba|FlechaAbajo|Secreto|Salida}.png (tablero en blanco: la palabra la escribe el juego).
        // Sin dibujo se arman con figuras del color del sistema.

        private struct SignTheme { public string system, title, hazard, exit; }

        private SignTheme signTheme;
        private Transform signRoot;
        private readonly List<Vector2> placedSigns = new List<Vector2>();
        private Vector2 currentSpawn;
        private float currentExitX;
        private Font signFont;

        private const string SignArtRoot = "Assets/_Project/Art/Letreros";

        private static SignTheme T(string system, string title, string hazard, string exit) =>
            new SignTheme { system = system, title = title, hazard = hazard, exit = exit };

        private static SignTheme SignThemeFor(string path)
        {
            if (path == EstomagoPath) return T("Digestivo", "ESTÓMAGO", "¡ÁCIDO!", "PÍLORO");
            if (path == DelgadoPath) return T("Digestivo", "INTESTINO DELGADO", "¡BILIS!", "AL COLON");
            if (path == GruesoPath) return T("Digestivo", "INTESTINO GRUESO", "¡CUIDADO!", "SALIDA");
            if (path == BocaPath) return T("Digestivo", "BOCA", "¡SALIVA!", "SALIDA");
            if (path == NarizPath) return T("Respiratorio", "NARIZ", "¡HUECO!", "A LA TRÁQUEA");
            if (path == TraqueaPath) return T("Respiratorio", "TRÁQUEA", "¡HUECO!", "A LOS PULMONES");
            if (path == PulmonesPath) return T("Respiratorio", "PULMONES", "¡HUECO!", "SALIDA");
            if (path == AmigdalasPath) return T("Respiratorio", "AMÍGDALAS", "¡HUECO!", "SALIDA");
            if (path == MedulaPath) return T("Nervioso", "MÉDULA ESPINAL", "¡HUECO!", "AL CEREBRO");
            if (path == CerebroPath) return T("Nervioso", "CEREBRO", "¡HUECO!", "AL HIPOCAMPO");
            if (path == HipocampoPath) return T("Nervioso", "HIPOCAMPO", "¡HUECO!", "SALIDA");
            if (path == OjoPath) return T("Nervioso", "OJO", null, "SALIDA");
            return new SignTheme();
        }

        private static string SignFile(Signpost.Kind kind)
        {
            switch (kind)
            {
                case Signpost.Kind.Welcome: return "Bienvenida";
                case Signpost.Kind.Warning: return "Peligro";
                case Signpost.Kind.ArrowRight: return "FlechaDerecha";
                case Signpost.Kind.ArrowUp: return "FlechaArriba";
                case Signpost.Kind.ArrowDown: return "FlechaAbajo";
                case Signpost.Kind.Secret: return "Secreto";
                case Signpost.Kind.Exit: return "Salida";
                default: return "Info";
            }
        }

        /// <summary>Ancho en el mundo de cada letrero (el alto sale del dibujo).</summary>
        private static float SignWidth(Signpost.Kind kind)
        {
            switch (kind)
            {
                case Signpost.Kind.Welcome: return 3.8f;
                case Signpost.Kind.Warning: return 2.3f;
                case Signpost.Kind.ArrowRight: return 2.3f;
                case Signpost.Kind.ArrowUp: return 1.5f;
                case Signpost.Kind.ArrowDown: return 1.6f;
                case Signpost.Kind.Secret: return 1.2f;
                case Signpost.Kind.Exit: return 2.5f;
                default: return 2.1f;
            }
        }

        /// <summary>
        /// Dónde va la palabra en cada dibujo: centro y tamaño como fracción del dibujo (x desde la izquierda,
        /// y desde abajo). Medido sobre el tablero en blanco de cada letrero (Tools: zona libre más grande).
        /// </summary>
        private static readonly Dictionary<string, Rect> SignTextAreas = new Dictionary<string, Rect>
        {
                { "Digestivo/Bienvenida", new Rect(0.496f, 0.713f, 0.623f, 0.239f) },
                { "Digestivo/Info", new Rect(0.488f, 0.613f, 0.629f, 0.231f) },
                { "Digestivo/Peligro", new Rect(0.581f, 0.714f, 0.411f, 0.286f) },
                { "Digestivo/Salida", new Rect(0.326f, 0.694f, 0.295f, 0.304f) },
                { "Digestivo/FlechaDerecha", new Rect(0.5f, 0.655f, 0.585f, 0.209f) },
                { "Digestivo/FlechaArriba", new Rect(0.504f, 0.628f, 0.358f, 0.338f) },
                { "Digestivo/FlechaAbajo", new Rect(0.519f, 0.684f, 0.332f, 0.359f) },
                { "Digestivo/Secreto", new Rect(0.798f, 0.686f, 0.198f, 0.304f) },
                { "Respiratorio/Bienvenida", new Rect(0.503f, 0.628f, 0.598f, 0.183f) },
                { "Respiratorio/Info", new Rect(0.597f, 0.633f, 0.439f, 0.304f) },
                { "Respiratorio/Peligro", new Rect(0.596f, 0.645f, 0.431f, 0.245f) },
                { "Respiratorio/Salida", new Rect(0.332f, 0.622f, 0.301f, 0.282f) },
                { "Respiratorio/FlechaDerecha", new Rect(0.511f, 0.635f, 0.536f, 0.215f) },
                { "Respiratorio/FlechaArriba", new Rect(0.488f, 0.611f, 0.305f, 0.331f) },
                { "Respiratorio/FlechaAbajo", new Rect(0.508f, 0.634f, 0.348f, 0.315f) },
                { "Respiratorio/Secreto", new Rect(0.237f, 0.568f, 0.223f, 0.248f) },
                { "Nervioso/Bienvenida", new Rect(0.501f, 0.662f, 0.575f, 0.223f) },
                { "Nervioso/Info", new Rect(0.559f, 0.648f, 0.4f, 0.314f) },
                { "Nervioso/Peligro", new Rect(0.576f, 0.639f, 0.383f, 0.239f) },
                { "Nervioso/Salida", new Rect(0.345f, 0.636f, 0.299f, 0.272f) },
                { "Nervioso/FlechaDerecha", new Rect(0.537f, 0.663f, 0.536f, 0.218f) },
                { "Nervioso/FlechaArriba", new Rect(0.503f, 0.688f, 0.31f, 0.333f) },
                { "Nervioso/FlechaAbajo", new Rect(0.48f, 0.696f, 0.285f, 0.333f) },
                { "Nervioso/Secreto", new Rect(0.305f, 0.633f, 0.153f, 0.251f) },
        };

        private static Rect SignTextArea(string system, Signpost.Kind kind)
        {
            return SignTextAreas.TryGetValue($"{system}/{SignFile(kind)}", out Rect r) ? r : new Rect(0.5f, 0.68f, 0.6f, 0.25f);
        }

        /// <summary>Colores de los letreros de figuras (mientras no hay dibujo) según el sistema.</summary>
        private void SignColors(Signpost.Kind kind, out Color post, out Color board, out Color ink)
        {
            Color line = new Color(0.18f, 0.05f, 0.1f);
            switch (signTheme.system)
            {
                case "Respiratorio": post = new Color(0.36f, 0.42f, 0.66f); board = new Color(0.9f, 0.96f, 1f); break;
                case "Nervioso": post = new Color(0.42f, 0.26f, 0.6f); board = new Color(0.95f, 0.9f, 1f); break;
                default: post = new Color(0.62f, 0.27f, 0.32f); board = new Color(1f, 0.93f, 0.85f); break;
            }
            ink = line;
            if (kind == Signpost.Kind.Warning) { board = new Color(1f, 0.8f, 0.25f); ink = new Color(0.45f, 0.05f, 0.05f); }
            if (kind == Signpost.Kind.Exit) board = new Color(0.62f, 0.95f, 0.68f);
            if (kind == Signpost.Kind.Secret) board = new Color(0.85f, 0.75f, 1f);
        }

        /// <summary>
        /// Pone un letrero. Si cae dentro de un hueco o lago, se recorre a la orilla de antes.
        /// 'message' = lo que dice NODO al llegar (opcional). 'flip' voltea la flecha a la izquierda.
        /// </summary>
        private Signpost Sign(Signpost.Kind kind, float x, float y, string label, string message = null, bool flip = false)
        {
            if (signTheme.system == null) { if (!string.IsNullOrEmpty(message)) SetMessage(message, x); return null; }
            if (y <= FloorY + 0.1f)
                foreach (Vector2 cut in currentCuts)
                    if (x > cut.x - 0.6f && x < cut.x + cut.y + 0.6f) { x = cut.x - 1.2f; break; }
            // No encimar letreros: si ya hay uno pegado, este se recorre a la derecha.
            foreach (Vector2 other in placedSigns)
                if (Mathf.Abs(other.x - x) < 1.6f && Mathf.Abs(other.y - y) < 1.5f) { x = other.x + 1.8f; break; }
            placedSigns.Add(new Vector2(x, y));

            if (signRoot == null) signRoot = new GameObject("Letreros").transform;
            var go = new GameObject($"Letrero_{SignFile(kind)}" + (string.IsNullOrEmpty(label) ? "" : $"_{label}"));
            go.transform.SetParent(signRoot, false);
            go.transform.position = new Vector3(x, y, 0f);
            Transform board = SetupUtils.Child(go.transform, "Tablero", Vector3.zero).transform;

            Rect layout = SignTextArea(signTheme.system, kind);
            Sprite art = PrefabSetup.ArtByWidth($"{SignArtRoot}/{signTheme.system}/Letrero_{SignFile(kind)}.png", SignWidth(kind), new Vector2(0.5f, 0.015f));
            Vector2 center, size;
            SignColors(kind, out Color post, out Color boardColor, out Color ink);
            const int order = -30;
            if (art != null)
            {
                SpriteRenderer r = SetupUtils.SpriteChild(board, "Dibujo", art, Color.white, Vector3.zero, Vector2.one, order);
                r.flipX = flip;
                Vector2 b = art.bounds.size;
                float cx = flip ? 1f - layout.x : layout.x;
                center = new Vector2((cx - 0.5f) * b.x, (layout.y - 0.015f) * b.y);   // el pivote está en la base
                size = new Vector2(layout.width * b.x, layout.height * b.y);
                if (kind == Signpost.Kind.Secret) label = null;   // el dibujo ya trae el "?"
            }
            else PlaceholderSign(board, kind, flip, post, boardColor, order, out center, out size);

            var sign = go.AddComponent<Signpost>();
            sign.Configure(kind, label, message, board, center, size, ink, signFont,
                kind == Signpost.Kind.Secret ? 1.4f : 2.6f, order);
            return sign;
        }

        /// <summary>Letrero de figuras: poste y tablero redondeado con contorno (hasta que llegue el dibujo).</summary>
        private static void PlaceholderSign(Transform board, Signpost.Kind kind, bool flip, Color post, Color fill, int order,
            out Vector2 center, out Vector2 size)
        {
            Color line = new Color(0.18f, 0.05f, 0.1f);
            float w = 2f, h = 0.9f, y = 1.6f;
            switch (kind)
            {
                case Signpost.Kind.Welcome: w = 3.8f; h = 1.1f; y = 2.3f; break;
                case Signpost.Kind.Secret: w = 0.8f; h = 0.65f; y = 0.95f; break;
                case Signpost.Kind.ArrowUp:
                case Signpost.Kind.ArrowDown: w = 1.5f; h = 0.75f; break;
                case Signpost.Kind.Exit: w = 2.5f; h = 0.95f; y = 1.8f; break;
            }
            // Postes (dos en la bienvenida).
            float[] posts = kind == Signpost.Kind.Welcome ? new[] { -1.4f, 1.4f } : new[] { 0f };
            foreach (float px in posts)
            {
                SetupUtils.SpriteChild(board, "PosteContorno", PlaceholderArt.Square, line, new Vector3(px, -0.05f, 0f), new Vector2(0.3f, y + 0.05f), order);
                SetupUtils.SpriteChild(board, "Poste", PlaceholderArt.Square, post, new Vector3(px, 0f, 0f), new Vector2(0.17f, y - 0.05f), order + 1);
            }
            // La cápsula mide 1 x 0.5: por eso la altura va al doble.
            SetupUtils.SpriteChild(board, "TableroContorno", PlaceholderArt.Capsule, line, new Vector3(0f, y, 0f), new Vector2(w + 0.16f, (h + 0.16f) * 2f), order + 2);
            SetupUtils.SpriteChild(board, "Tablero", PlaceholderArt.Capsule, fill, new Vector3(0f, y, 0f), new Vector2(w, h * 2f), order + 3);
            SetupUtils.SpriteChild(board, "Brillo", PlaceholderArt.Capsule, new Color(1f, 1f, 1f, 0.35f), new Vector3(0f, y + h * 0.28f, 0f), new Vector2(w * 0.8f, h * 0.36f), order + 4);

            // Puntas de flecha.
            float dir = flip ? -1f : 1f;
            if (kind == Signpost.Kind.ArrowRight || kind == Signpost.Kind.Exit)
                Arrowhead(board, new Vector3(dir * (w * 0.5f + 0.12f), y, 0f), dir > 0 ? 0f : 180f, h, line, fill, order);
            if (kind == Signpost.Kind.ArrowUp) Arrowhead(board, new Vector3(0f, y + h * 0.5f + 0.12f, 0f), 90f, h, line, fill, order);
            if (kind == Signpost.Kind.ArrowDown) Arrowhead(board, new Vector3(0f, y - h * 0.5f - 0.12f, 0f), -90f, h, line, fill, order);
            if (kind == Signpost.Kind.Warning)
            {
                // Insignia "!" a la izquierda.
                SetupUtils.SpriteChild(board, "AlertaContorno", PlaceholderArt.Circle, line, new Vector3(-w * 0.5f, y + 0.05f, 0f), new Vector2(0.7f, 0.7f), order + 4);
                SetupUtils.SpriteChild(board, "Alerta", PlaceholderArt.Circle, new Color(0.9f, 0.2f, 0.15f), new Vector3(-w * 0.5f, y + 0.05f, 0f), new Vector2(0.56f, 0.56f), order + 5);
                SetupUtils.SpriteChild(board, "AlertaPalo", PlaceholderArt.SquareCentered, Color.white, new Vector3(-w * 0.5f, y + 0.12f, 0f), new Vector2(0.09f, 0.24f), order + 6);
                SetupUtils.SpriteChild(board, "AlertaPunto", PlaceholderArt.Circle, Color.white, new Vector3(-w * 0.5f, y - 0.09f, 0f), new Vector2(0.1f, 0.1f), order + 6);
                center = new Vector2(0.18f, y);
                size = new Vector2(w - 0.6f, h * 0.7f);
                return;
            }
            center = new Vector2(kind == Signpost.Kind.ArrowRight ? -dir * 0.1f : 0f, y);
            size = new Vector2(w * 0.86f, h * 0.7f);
            if (kind == Signpost.Kind.Welcome) size = new Vector2(w * 0.9f, h * 0.75f);
        }

        private static void Arrowhead(Transform board, Vector3 pos, float angle, float h, Color line, Color fill, int order)
        {
            Transform head = SetupUtils.Child(board, "Punta", pos).transform;
            head.localRotation = Quaternion.Euler(0f, 0f, angle + 45f);
            float s = h * 0.78f;
            SetupUtils.SpriteChild(head, "PuntaContorno", PlaceholderArt.SquareCentered, line, Vector3.zero, new Vector2(s + 0.16f, s + 0.16f), order + 2);
            SetupUtils.SpriteChild(head, "PuntaRelleno", PlaceholderArt.SquareCentered, fill, Vector3.zero, new Vector2(s, s), order + 3);
        }

        /// <summary>Letrero de información: se ve desde antes y NODO explica la mecánica al llegar.</summary>
        private Signpost Tip(float x, string label, string message, float y = FloorY, Signpost.Kind kind = Signpost.Kind.Info)
            => Sign(kind, x, y, label, message);

        /// <summary>Bienvenida, salida y avisos de peligro antes de huecos y lagos (al terminar de armar el nivel).</summary>
        private void FinishSigns()
        {
            if (signTheme.system == null) return;
            Sign(Signpost.Kind.Welcome, currentSpawn.x + 2.8f, FloorY, signTheme.title);
            Sign(Signpost.Kind.Exit, currentExitX - 0.8f, FloorY, signTheme.exit);
            if (string.IsNullOrEmpty(signTheme.hazard)) return;

            float prevEnd = float.MinValue;
            for (int i = 0; i < currentCuts.Count; i++)
            {
                Vector2 cut = currentCuts[i];
                float x = cut.x - 1.3f;
                bool wanted = i == 0 || cut.y >= 9f;
                bool room = x - 0.7f > prevEnd && x > currentSpawn.x + 6f;
                bool alone = true;
                foreach (Vector2 other in placedSigns)
                    if (other.x > cut.x - 4.5f && other.x < cut.x + 0.5f && Mathf.Abs(other.y - FloorY) < 1.5f) { alone = false; break; }
                if (wanted && room && alone) Sign(Signpost.Kind.Warning, x, FloorY, signTheme.hazard);
                prevEnd = Mathf.Max(prevEnd, cut.x + cut.y);
            }
        }

        /// <summary>"¡JEFE!" antes de una arena con jefe (sus datos se llaman Jefe_...).</summary>
        private void BossSignIfNeeded(float triggerX, Wave[] waves)
        {
            if (signTheme.system == null || waves == null) return;
            foreach (Wave w in waves)
                foreach (SpawnEntry e in w.entries)
                    if (e.data != null && e.data.name.StartsWith("Jefe_") && !e.data.name.Contains("Virion"))
                    {
                        Sign(Signpost.Kind.Warning, triggerX - 2.4f, FloorY, "¡JEFE!");
                        return;
                    }
        }

        /// <summary>Mensaje de NODO que aparece al pasar por una X (tutoriales sin arena).</summary>
        private static void SetMessage(string text, float x)
        {
            var go = new GameObject("Mensaje");
            go.transform.position = new Vector3(x, FloorY, 0f);
            var trigger = go.AddComponent<MessageTrigger>();
            SetupUtils.SetString(trigger, "message", text);
        }

        private void BuildSandbox()
        {
            BuildLevelBase(70f, new LevelStyle
            {
                background = new Color(0.1f, 0.1f, 0.12f),
                wall = new Color(0.3f, 0.3f, 0.34f),
                floor = new Color(0.45f, 0.45f, 0.5f),
                floorEdge = new Color(0.6f, 0.6f, 0.65f),
                blob = new Color(0.4f, 0.4f, 0.45f, 0.5f)
            }, "SANDBOX: escena libre para probar mecánicas. Duplícala para tus pruebas.",
                new Vector2(5f, 2f));

            Acid(1f);
            Platform(-3f, 2.2f, 3f);
            Platform(1f, 3.8f, 2.5f);

            Arena("Arena_Enemigos", 10f, 20f, "Todos los enemigos",
                W(0.5f, E(prefabs.HPylori, data.HPylori, 1), E(prefabs.Giardia, data.Giardia, 1, SpawnSide.Left)),
                W(0.8f, E(prefabs.Rotavirus, data.Rotavirus, 1), E(prefabs.Entamoeba, data.Entamoeba, 1, SpawnSide.Left)),
                W(0.8f, E(prefabs.CDifficile, data.CDifficile, 1), E(prefabs.Oxiuro, data.Oxiuro, 2, SpawnSide.Left)),
                // Un tipo de comportamiento por oleada, para probarlos todos (ver EnemyBehavior).
                W(0.8f, E(prefabs.SMutans, data.SMutans, 2), E(prefabs.SAureus, data.SAureus, 1, SpawnSide.Left)),
                W(0.8f, E(prefabs.Pertussis, data.Pertussis, 1), E(prefabs.Neumococo, data.Neumococo, 2, SpawnSide.Left)),
                W(0.8f, E(prefabs.Rinovirus, data.Rinovirus, 2), E(prefabs.Polen, data.Polen, 2, SpawnSide.Left)),
                W(0.8f, E(prefabs.Influenza, data.Influenza, 1), E(prefabs.Tuberculosis, data.Tuberculosis, 1, SpawnSide.Left)),
                W(0.8f, E(prefabs.Meningococo, data.Meningococo, 1), E(prefabs.Poliovirus, data.Poliovirus, 1, SpawnSide.Left)),
                W(0.8f, E(prefabs.Herpes, data.Herpes, 1), E(prefabs.Rabia, data.Rabia, 2, SpawnSide.Left)));
            Platform(14f, 2.2f, 3f);
            Platform(22f, 2.2f, 3f);
            Platform(18f, 4.6f, 2.5f); // más alta: para ver que los enemigos suben y bajan

            Arena("Arena_Jefe", 38f, 22f, "Jefe lombriz (versión final)",
                W(1.0f, E(prefabs.LombrizGrueso, data.Lombriz, 1)));
        }
    }
}
