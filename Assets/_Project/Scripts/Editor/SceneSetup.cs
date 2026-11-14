using System.Collections.Generic;
using System.IO;
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

            try
            {
                build();
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

        private static void BuildMenuScene<T>(string name, Color background) where T : Component
        {
            CreateCamera(background, false);
            new GameObject(name, typeof(T));
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

            new GameObject("HUD", typeof(HUD));
            new GameObject("PauseMenu", typeof(PauseMenu));

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
            }, "NODO: Estás en el estómago. Muévete con A/D, salta con K o Espacio. Apunta con W (W + A/D = diagonal).");

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
                "H. pylori: una bacteria que se entierra en el moco y sale debajo de ti (enterrada no le haces daño). Cambia de cápsula con Q/E hasta encontrar la que funciona.",
                W(0.8f, E(prefabs.HPylori, data.HPylori, 1)),
                W(0.8f, E(prefabs.HPylori, data.HPylori, 2)));

            // ----- 2) Lagos con comida "viva" y chorros de ácido -----
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
                "NODO: ¡Una colonia de H. pylori! Destruye a la MADRE o no dejará de soltar crías. La H. pylori turquesa neutraliza el ácido: cerca de ella no te quema. Escudo: Shift (o I); justo antes del golpe = PARRY.",
                W(0.6f, E(prefabs.HPyloriUreasa, data.HPyloriUreasa, 1), E(prefabs.HPylori, data.HPylori, 2, SpawnSide.Left)),
                W(1.0f, E(prefabs.HPyloriMadre, data.HPyloriMadre, 1), E(prefabs.HPyloriUreasa, data.HPyloriUreasa, 1, SpawnSide.Left)));
            Food("Hamburguesa", 59.6f, 0.95f, FloatingFood.FoodKind.Sinking);
            Food("Salchicha", 64.2f, 0.79f, FloatingFood.FoodKind.Rolling);
            Platform(62f, 3.4f, 3f);

            // ----- 4) Burbujas de gas y el sushi -----
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
            // Rediseño (190 de largo): 1) duodeno (bilis y grasa) · 2) yeyuno (vellosidades y rotavirus) ·
            // 3) tenia · 4) íleon (río de quimo en balsa) · 5) persecución de la lombriz hasta la válvula ileocecal.
            BuildLevelBase(190f, new LevelStyle
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

            SetMessage("NODO: Duodeno. La bilis rompe la grasa en gotitas (la emulsiona). Cruza las gotas ANTES de que llegue el chorro.", 26f);
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
            SetMessage("NODO: Yeyuno. Las vellosidades absorben la comida. Brinca de punta en punta... y cuidado: hay Giardias escondidas en la bilis.", 50f);
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
            SetMessage("NODO: Íleon. Súbete a la salchicha: el quimo te lleva río abajo. Brinca los chorros de bilis.", 126f);
            Food("Salchicha", 130f, 0.79f, FloatingFood.FoodKind.Raft);
            BileGeyser(138f, bottom, 3.2f, 3.6f, 0f);
            BileGeyser(146.5f, bottom, 3.2f, 3.6f, 1.2f);
            BileGeyser(155f, bottom, 3.2f, 3.6f, 2.4f);
            Ambush(134f, 4.2f, prefabs.Rotavirus, data.Rotavirus, 2, 0.5f, 9f, Vector2.zero, false);
            Ambush(142f, 4.6f, prefabs.Rotavirus, data.Rotavirus, 1, 0.5f, 9f, Vector2.zero, false);
            Ambush(150f, 4.2f, prefabs.Rotavirus, data.Rotavirus, 2, 0.5f, 9f, Vector2.zero, false);

            // ---------- 5) Persecución de la lombriz ----------
            Peyer(163.5f);
            WormChaseAt(164f, 168f, 173f, 178f, 183f);
            IleocecalValve(185f);
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
            SetMessage("NODO: Haustras. Las bolsas del colon se aprietan y te lanzan: úsalas para subir.", 26f);
            Haustra(28f, 0f);
            Haustra(31.5f, 0.9f);
            Haustra(35f, 1.8f);
            // Apéndice secreto: arriba, se llega con la bolsa de en medio + doble salto.
            Platform(31.5f, 5.6f, 3.2f);
            AppendixReward(31.5f, 6.8f);

            // ---------- 2) Colon ascendente: la subida ----------
            SetMessage("NODO: Colon ascendente. Sube... ¡y no dejes que te alcance lo que viene de abajo!", 40f);
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
            SetMessage("NODO: Microbiota. Las bacterias verdes son BUENAS: no les dispares antibiótico o crecerá C. difficile.", 58f);
            foreach (float fx in new[] { 61f, 63.5f, 66f, 68.5f, 71f, 73.5f }) FloraAt(fx, top);
            Probiotic(66f, top + 1.6f);
            ArenaZone cdiff = Arena("Arena_2_CDifficile", 80f, 22f, "C. difficile: herida se vuelve espora y todo le rebota. Solo el antibiótico... ¡y cuidado con la flora buena!",
                W(0.6f, E(prefabs.CDifficile, data.CDifficile, 2), E(prefabs.Oxiuro, data.Oxiuro, 1, SpawnSide.Left)),
                W(1.0f, E(prefabs.CDifficile, data.CDifficile, 2), E(prefabs.Entamoeba, data.Entamoeba, 1, SpawnSide.Left)));
            cdiff.floorY = top;
            cdiff.transform.position = new Vector3(80f, top, 0f);
            foreach (float fx in new[] { 83f, 88.5f, 94f }) FloraAt(fx, top);
            Platform(86f, top + 2.6f, 3f);
            Platform(93f, top + 2.6f, 3f);
            foreach (float fx in new[] { 104f, 107f, 110f, 113f }) FloraAt(fx, top);
            Probiotic(108.5f, top + 1.6f);
            Ambush(110f, top + 3f, prefabs.CDifficile, data.CDifficile, 2, 0.5f, 7f, new Vector2(0f, 2f), false);
            Ambush(116f, top + 3f, prefabs.Oxiuro, data.Oxiuro, 3, 0.5f, 7f, new Vector2(0f, 2f), false);

            // ---------- 4) Colon descendente: la bajada ----------
            SetMessage("NODO: Colon descendente. El colon absorbe el agua: todo se vuelve LODO espeso. No te quedes parado.", 120f);
            Bolus(new Vector2(124.5f, top - 0.6f), new Vector2(124.5f, 1.2f), 2.2f, 0f);
            Bolus(new Vector2(130f, 1.2f), new Vector2(130f, top - 2f), 2.2f, 0.5f);
            Platform(127.5f, 11f, 2.4f);
            Platform(133f, 7f, 2.4f);
            Platform(137f, 3.5f, 2.4f);
            Mud(126f, 152f);
            Ulcer(135.5f);
            Ulcer(145f);

            // ---------- 5) Sigmoide y recto ----------
            SetMessage("NODO: Recto. Los oxiuros ponen sus huevos aquí: rómpelos antes de que salgan más.", 152f);
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

        private void BuildBoca()
        {
            BuildLevelBase(70f, new LevelStyle
            {
                // Boca: encías rosas, dientes blancos de fondo y saliva.
                background = new Color(0.14f, 0.03f, 0.06f),
                wall = new Color(0.45f, 0.12f, 0.2f),
                floor = new Color(0.85f, 0.4f, 0.5f),
                floorEdge = new Color(1f, 0.97f, 0.9f),
                blob = new Color(0.97f, 0.95f, 0.88f, 0.85f),
                decor = Decor.Blobs,
                drip = new Color(0.85f, 0.95f, 1f, 0.6f),
                glow = new Color(0.9f, 0.95f, 1f, 1f),
                backdrop = "Fondo_Boca",     // pintura estilo caricatura (dientes, lengua y saliva)
                tissue = "Boca",             // tejido de encía rosa
                pitLiquid = new Color(1f, 1f, 1f, 1f), // si hay huecos: saliva
                liquidDeep = new Color(0.45f, 0.62f, 0.8f, 0.9f),
                liquidSurface = new Color(0.82f, 0.94f, 1f, 0.75f),
                liquidOutline = new Color(0.2f, 0.28f, 0.42f, 1f),
                liquidCrest = new Color(1f, 1f, 1f, 0.95f)
            }, "NIVEL SECRETO · BOCA. NODO: Placa bacteriana detectada.");

            // Dientes: plataformas blancas.
            Arena("Arena_1_Caries", 8f, 18f, "S. mutans (caries): te lanza ácido en arco desde lejos. Antibiótico.",
                W(0.6f, E(prefabs.SMutans, data.SMutans, 3)),
                W(1.0f, E(prefabs.SMutans, data.SMutans, 2), E(prefabs.SMutans, data.SMutans, 2, SpawnSide.Left)));
            Platform(12f, 2.2f, 2f);
            Platform(18f, 2.2f, 2f);

            Arena("Arena_2_Placa", 34f, 20f, "La placa: muchas a la vez. (Mini jefe 'caries': próximamente)",
                W(0.6f, E(prefabs.SMutans, data.SMutans, 3, SpawnSide.Both)),
                W(1.0f, E(prefabs.SMutans, data.SMutans, 4, SpawnSide.Both)));
            Platform(38f, 2.2f, 2f);
            Platform(44f, 3.4f, 2f);
            Platform(50f, 2.2f, 2f);
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
            BuildLevelBase(90f, new LevelStyle
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
                new Vector2(26f, 3f), new Vector2(44f, 3f));

            Platform(1f, 2.2f, 3f);

            // Arena 1: virus del resfriado y polen. Plataformas para pelear con los voladores.
            Arena("Arena_1_Resfriado", 8f, 18f,
                "Rinovirus (resfriado): no hay antiviral para él, los medicamentos le hacen poco y se cura solo con el tiempo. El polen te persigue con el aire: Antihistamínico.",
                W(0.8f, E(prefabs.Rinovirus, data.Rinovirus, 1), E(prefabs.Polen, data.Polen, 2)),
                W(1.0f, E(prefabs.Rinovirus, data.Rinovirus, 2), E(prefabs.Polen, data.Polen, 2, SpawnSide.Left)));
            Platform(10f, 2.4f, 2.6f);
            Platform(15.5f, 3.8f, 2.6f);
            Platform(21f, 2.4f, 2.6f);

            // Tramo de saltos: hueco con escalón flotante y escalera hacia arriba.
            Platform(27.5f, 2.2f, 1.6f);
            Platform(31f, 2.2f, 2.2f);
            Platform(33.8f, 3.8f, 2.2f);
            Platform(36.6f, 5.4f, 2.2f);
            SetMessage("Mucosidad: tapa el paso y te frena si la tocas. Dispárale MUCOLÍTICO (el antihistamínico también sirve, pero menos).", 37f);
            Block(41f, true);

            // Hueco con burbuja en medio.
            Platform(45.5f, 2.6f, 1.6f);

            BlockedArena("Arena_2_Estafilococo", 52f, 22f,
                "S. aureus: racimo de cocos; al romperlo se separa en cocos sueltos. Antibiótico. Luego disuelve la mucosidad con Mucolítico.", true,
                W(0.6f, E(prefabs.SAureus, data.SAureus, 3)),
                W(1.0f, E(prefabs.SAureus, data.SAureus, 2, SpawnSide.Left), E(prefabs.Polen, data.Polen, 2), E(prefabs.Rinovirus, data.Rinovirus, 1)));
            Platform(55f, 2.4f, 2.6f);
            Platform(60f, 3.8f, 2.6f);
            Platform(65f, 2.4f, 2.6f);
        }

        private void BuildTraquea()
        {
            BuildLevelBase(100f, new LevelStyle
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
                new Vector2(31f, 4f), new Vector2(44.5f, 5f), new Vector2(76f, 3f));

            Arena("Arena_1_TosFerina", 8f, 18f, "B. pertussis (tos ferina): se infla y tose tres veces; cada tos te empuja. Antibiótico.",
                W(0.6f, E(prefabs.Pertussis, data.Pertussis, 2)),
                W(1.0f, E(prefabs.Pertussis, data.Pertussis, 2, SpawnSide.Both), E(prefabs.Rinovirus, data.Rinovirus, 1)));
            Platform(11f, 2.4f, 2.6f);
            Platform(18f, 2.4f, 2.6f);

            // Inflamación: tutorial sin enemigos.
            SetMessage("Inflamación: el tejido hinchado cierra el paso. Dispárale ANTIINFLAMATORIO hasta que se contraiga.", 24f);
            Block(28f, false);

            // Tramo de saltos sobre dos huecos: escalera de ida y vuelta por arriba.
            Platform(33f, 2.6f, 1.8f);
            Platform(37f, 2.4f, 2.4f);
            Platform(40f, 4f, 2.4f);
            Platform(43.5f, 5.6f, 2.4f);
            Platform(47f, 4f, 2.4f);
            Platform(50.5f, 2.4f, 2.2f);

            BlockedArena("Arena_2_Bronquio", 56f, 22f,
                "Influenza: flota alto y se lanza en picada con sus espículas. Antiviral. Al final, mucosidad: Mucolítico.", true,
                W(0.6f, E(prefabs.Pertussis, data.Pertussis, 3)),
                W(1.0f, E(prefabs.Influenza, data.Influenza, 1), E(prefabs.Pertussis, data.Pertussis, 2, SpawnSide.Left)),
                W(1.0f, E(prefabs.Influenza, data.Influenza, 2), E(prefabs.Polen, data.Polen, 2, SpawnSide.Both)));
            Platform(59f, 2.4f, 2.6f);
            Platform(64f, 3.8f, 2.6f);
            Platform(69f, 2.4f, 2.6f);

            Platform(77.5f, 2.4f, 1.8f);
            Block(84f, false);
        }

        private void BuildPulmones()
        {
            // 3 tramos por bronquiolos, cada uno con un bloqueo; el último es la arena de la colmena.
            LevelManager manager = BuildLevelBase(135f, new LevelStyle
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
                new Vector2(30f, 4f), new Vector2(45f, 4.5f), new Vector2(76f, 4.5f));

            // Tramo 1
            BlockedArena("Tramo_1_Neumonia", 8f, 20f,
                "S. pneumoniae (neumonía): avanzan a saltos y te caen encima. Antibiótico.", true,
                W(0.6f, E(prefabs.Neumococo, data.Neumococo, 3)),
                W(1.0f, E(prefabs.Neumococo, data.Neumococo, 2, SpawnSide.Left), E(prefabs.Influenza, data.Influenza, 1)));
            Platform(10f, 2.4f, 2.6f);
            Platform(15.5f, 3.8f, 2.6f);
            Platform(20.5f, 2.4f, 2.6f);

            // Bronquiolos: los alvéolos RESPIRAN (rosados): se inflan, tiemblan y se desinflan. Salta cuando estén llenos.
            // Reglas: huecos de máx. 4.5 u, repisa destino a máx. 3.5 u arriba del alvéolo, 5.5 u libres arriba.
            SetMessage("NODO: Los alvéolos respiran. Inflados son trampolines que te lanzan muy alto; desinflados son una plataforma normal.", 27f);
            Alveolus(32f, 1.2f, 2f, 0f);          // sobre el hueco 1 (4 u)
            Platform(35.8f, 4.4f, 2.4f);           // repisa: 3.2 u arriba del alvéolo
            Alveolus(39.5f, 1.2f, 2f, 1.4f);       // desfasado 1.4 s
            Platform(43f, 4.6f, 2.4f);             // repisa: 3.4 u arriba
            Alveolus(47.2f, 1.2f, 1.8f, 2.75f);    // sobre el hueco 2 (4.5 u), alternado con el primero
            Platform(51.2f, 4.2f, 2f);

            // Tramo 2
            BlockedArena("Tramo_2_Influenza", 54f, 20f,
                "Influenza en el aire y neumococos en el piso. Al final, inflamación: Antiinflamatorio.", false,
                W(0.6f, E(prefabs.Influenza, data.Influenza, 2)),
                W(1.0f, E(prefabs.Neumococo, data.Neumococo, 3, SpawnSide.Both), E(prefabs.Influenza, data.Influenza, 1)));
            Alveolus(56f, 2.4f, 2.4f);
            Alveolus(61f, 3.8f, 2.4f);
            Alveolus(66f, 2.4f, 2.4f);

            // Secuencia de timing sobre el hueco 3 (4.5 u): dos alvéolos alternados (medio ciclo de diferencia).
            Alveolus(77.2f, 1.2f, 1.6f, 0f);
            Alveolus(79.6f, 1.2f, 1.6f, 2.75f);
            Platform(84f, 2.4f, 2.4f);
            Platform(87f, 4.2f, 2.2f);

            // Tramo 3: arena del jefe. Primero un grupo, luego la colmena voladora.
            BlockedArena("Tramo_3_Colmena", 92f, 28f, "NODO: Algo enorme se mueve arriba...", true,
                W(0.6f, E(prefabs.Neumococo, data.Neumococo, 4, SpawnSide.Both)),
                W(1.5f, E(prefabs.Colmena, data.Colmena, 1)));
            // Plataformas para subir a pegarle y para esquivar el bombardeo.
            Alveolus(96f, 2.6f, 3f);
            Platform(101.5f, 4.2f, 3.4f);
            Alveolus(106f, 1.2f, 3f, 1f); // inflado te lanza a la altura de la colmena (cuidado con chocarla)
            Platform(111f, 4.2f, 3f);

            // Cierre de la entrega de diciembre (GDD 8.6).
            SetupUtils.SetString(manager, "completeMessage",
                "NODO: Fórmula 2 enviada a Aurelis. Alerta: la infección avanza hacia el cerebro...");
            SetupUtils.SetFloat(manager, "completeDelay", 4.5f);
        }

        /// <summary>Alvéolo: plataforma de un solo sentido que se ve como racimo de burbujas.</summary>
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
            LevelManager manager = BuildLevelBase(80f, new LevelStyle
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
                new Vector2(20f, 3.5f));

            new GameObject("SaludDelCerebro").AddComponent<BrainHealth>();

            // Entrada: un grupo y una neurona roja para calentar.
            Arena("Arena_Entrada", 4f, 14f, "Prepárate: el jefe rabia cae con Antiviral; Anticonvulsivo para las neuronas rojas y la rabia suelta.",
                W(0.6f, E(prefabs.Rabia, data.Rabia, 2), E(prefabs.Poliovirus, data.Poliovirus, 1)));
            Neuron(10f, 3.2f);
            RedNeuron(15f, 1.5f);
            Platform(8f, 2.4f, 2.4f);
            Platform(21.75f, 2.4f, 1.6f);
            Storm(24f, 30f);

            // Arena del jefe: rieles de axones a dos alturas, neuronas sanas que puede infectar y plataformas.
            const float arenaX = 34f, arenaW = 30f;
            float left = arenaX - 3f, right = arenaX - 3f + arenaW;
            // Rieles de la cadena (Centipede): baja de riel en cada orilla. En furia aparece uno a ras de piso.
            AxonRail(left, right, 6.2f);
            AxonRail(left, right, 4.4f);
            AxonRail(left, right, 2.6f);
            Arena("Arena_Rabia", arenaX, arenaW, null,
                W(1.2f, E(prefabs.RabiaJefe, data.RabiaJefe, 1)));
            // Neuronas cerca de los rieles: la cadena las infecta al pasar (fase 2).
            Neuron(left + 4f, 3.4f);
            Neuron(left + 10f, 5.3f);
            Neuron(left + 16f, 3.4f);
            Neuron(left + 22f, 5.3f);
            Neuron(left + 27f, 3.4f);
            Platform(left + 7f, 2.4f, 2.6f);
            Platform(left + 13f, 4.2f, 2.6f);
            Platform(left + 19f, 2.4f, 2.6f);
            Platform(left + 25f, 4.2f, 2.6f);

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
            BuildLevelBase(100f, new LevelStyle
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
                new Vector2(30f, 3.5f), new Vector2(44f, 4f), new Vector2(78f, 3.5f));

            Platform(2f, 2.4f, 3f);

            Arena("Arena_1_Meningitis", 8f, 18f, "N. meningitidis (meningitis): se agazapa y cruza a toda velocidad. Antibiótico.",
                W(0.6f, E(prefabs.Meningococo, data.Meningococo, 2)),
                W(1.0f, E(prefabs.Poliovirus, data.Poliovirus, 2), E(prefabs.Meningococo, data.Meningococo, 1, SpawnSide.Left)));
            Platform(10f, 2.4f, 2.6f);
            Platform(16f, 3.8f, 2.6f);
            Platform(21f, 2.4f, 2.6f);

            // Primera tormenta: aprende a leer el destello.
            SetMessage("Tormenta eléctrica: cuando veas el destello violeta, sal de la columna. El escudo NO protege.", 25f);
            Storm(25f, 50f);
            Platform(31.75f, 2.4f, 1.6f);
            Platform(37f, 2.4f, 2.4f);
            Platform(40.5f, 4f, 2.4f);
            Platform(44f, 5.6f, 2.4f);
            Platform(47.5f, 4f, 2.4f);

            Arena("Arena_2_Poliovirus", 54f, 20f, "Poliovirus: sus disparos te paralizan un momento. Antiviral. La rabia muerde en ráfaga: Anticonvulsivo. ¡Pelea bajo la tormenta!",
                W(0.6f, E(prefabs.Rabia, data.Rabia, 2), E(prefabs.Meningococo, data.Meningococo, 2, SpawnSide.Left)),
                W(1.0f, E(prefabs.Poliovirus, data.Poliovirus, 2), E(prefabs.Rabia, data.Rabia, 1)));
            Storm(51f, 74f);
            Platform(57f, 2.4f, 2.6f);
            Platform(63f, 3.8f, 2.6f);
            Platform(69f, 2.4f, 2.6f);

            Platform(79.75f, 2.4f, 1.6f);
        }

        private void BuildCerebro()
        {
            LevelManager manager = BuildLevelBase(130f, new LevelStyle
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
                new Vector2(29f, 4f), new Vector2(70f, 4f), new Vector2(84f, 4f));

            // Salud del cerebro (segunda barra del HUD).
            new GameObject("SaludDelCerebro").AddComponent<BrainHealth>();
            SetMessage("Tu cápsula regional es el ANTICONVULSIVO: calma neuronas rojas. Su bomba (L/F) protege a las neuronas 8 s.", 1f);

            // Tramo 1
            Arena("Tramo_1_Meningitis", 8f, 20f, "Acaba rápido con los virus: si sus disparos pegan en las neuronas, el cerebro sufre y se infectan.",
                W(0.6f, E(prefabs.Meningococo, data.Meningococo, 2), E(prefabs.Herpes, data.Herpes, 1)),
                W(1.0f, E(prefabs.Poliovirus, data.Poliovirus, 2), E(prefabs.Meningococo, data.Meningococo, 1, SpawnSide.Left)));
            Neuron(14f, 2.2f);
            Neuron(19.5f, 3.8f);
            Platform(11f, 2.4f, 2.4f);
            Platform(17f, 4.2f, 2.4f);
            Platform(22f, 2.4f, 2.4f);

            // Primera neurona hiperexcitada (roja): suelta descargas. Cálmala con Anticonvulsivo.
            SetMessage("Neurona ROJA hiperexcitada: suelta descargas y desgasta el cerebro. Cálmala con ANTICONVULSIVO (+5 cerebro).", 24f);
            RedNeuron(26.5f, 1.6f);

            // Puente de dendritas sobre el primer hueco, con tormenta.
            Storm(27f, 44f);
            Platform(31f, 2.4f, 1.8f);
            Platform(38.5f, 4.2f, 6f);
            Neuron(36f, 1.8f);
            Neuron(41f, 1.8f);

            // Tramo 2: pelea bajo la tormenta con neuronas en medio.
            Arena("Tramo_2_Encefalitis", 46f, 22f, "Herpes (encefalitis): se vuelve latente (invisible) y reaparece disparando en ráfaga. Antiviral.",
                W(0.6f, E(prefabs.Herpes, data.Herpes, 2), E(prefabs.Rabia, data.Rabia, 2, SpawnSide.Left)),
                W(1.0f, E(prefabs.Meningococo, data.Meningococo, 3, SpawnSide.Both), E(prefabs.Poliovirus, data.Poliovirus, 1)));
            Storm(43f, 68f);
            Neuron(52f, 1.1f);
            Neuron(57.5f, 3.4f);
            Neuron(63f, 1.8f);
            RedNeuron(48.5f, 3.6f);
            Platform(50f, 2.6f, 2.6f);
            Platform(55f, 4.4f, 2.6f);
            Platform(60.5f, 2.6f, 2.6f);
            Platform(65.5f, 4f, 2.4f);

            // Escalera de tejido nervioso sobre dos huecos.
            Platform(72f, 2.4f, 1.8f);
            Platform(77f, 2.4f, 2.4f);
            Platform(80.5f, 4.2f, 2.4f);
            Platform(84f, 5.8f, 2.4f);
            Platform(87.5f, 4.2f, 2.4f);
            Neuron(80.5f, 1.4f);
            RedNeuron(76f, 4.4f);

            // Tramo 3 (clímax sin jefe): la mayor cantidad de enemigos rodeados de neuronas sanas.
            Arena("Tramo_3_Corteza", 92f, 24f, "NODO: Última zona antes del hipocampo. Muchos virus apuntando a las neuronas: sé rápido.",
                W(0.6f, E(prefabs.Meningococo, data.Meningococo, 4, SpawnSide.Both)),
                W(1.0f, E(prefabs.Herpes, data.Herpes, 2), E(prefabs.Rabia, data.Rabia, 2, SpawnSide.Left)),
                W(1.2f, E(prefabs.Herpes, data.Herpes, 1), E(prefabs.Poliovirus, data.Poliovirus, 3), E(prefabs.Meningococo, data.Meningococo, 2, SpawnSide.Left)));
            Storm(89f, 113f);
            Neuron(95f, 1.8f);
            Neuron(99.5f, 3.6f);
            Neuron(103.5f, 1.1f);
            Neuron(107.5f, 3.4f);
            Neuron(111f, 2f);
            RedNeuron(97.5f, 4.6f);
            RedNeuron(108.5f, 1.2f);
            Platform(97.5f, 2.6f, 2.4f);
            Platform(102f, 4.4f, 2.6f);
            Platform(106.5f, 2.6f, 2.4f);
            Platform(110.5f, 4.4f, 2.4f);

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
