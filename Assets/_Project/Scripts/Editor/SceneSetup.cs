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
            CreateIfMissing(SandboxPath, BuildSandbox);

            EditorBuildSettings.scenes = new[]
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
        }

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

            build();
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

        private enum Decor { Blobs, Rings, Alveoli, Veins, Villi, Folds, Neurons }

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
            SetupUtils.SpriteChild(environment, "BackWall", PlaceholderArt.Square, style.wall,
                new Vector3(startX + length * 0.5f, FloorY, 0f), new Vector2(length + 30f, 10f), -200);

            // Piso por tramos (los huecos quedan entre tramos).
            var sorted = new List<Vector2>(pits ?? new Vector2[0]);
            sorted.Sort((a, b) => a.x.CompareTo(b.x));
            float segmentStart = startX - 15f;
            int index = 0;
            foreach (Vector2 pit in sorted)
            {
                FloorSegment($"Floor_{index++}", segmentStart, pit.x);
                segmentStart = pit.x + pit.y;
            }
            FloorSegment($"Floor_{index}", segmentStart, endX + 15f);

            if (style.pitLiquid.a > 0f)
            {
                foreach (Vector2 pit in sorted) PitLiquid(pit.x, pit.y);
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

            if (style.drip.a > 0f)
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
                for (float x = startX - 5f; x < endX + 10f; x += 6f)
                    SetupUtils.SpriteChild(environment, "Bruma", PlaceholderArt.Circle, new Color(style.glow.r, style.glow.g, style.glow.b, 0.08f),
                        new Vector3(x, FloorY + 1.2f, 0f), new Vector2(9f, 3.5f), -145);
                SetupUtils.SpriteChild(environment, "LuzSalida", PlaceholderArt.Circle, new Color(style.glow.r, style.glow.g, style.glow.b, 0.35f),
                    new Vector3(endX + 2f, FloorY + 3.5f, 0f), new Vector2(3.5f, 7f), -140);
                SetupUtils.SpriteChild(environment, "LuzSalidaCentro", PlaceholderArt.Circle, new Color(0.9f, 1f, 1f, 0.6f),
                    new Vector3(endX + 2f, FloorY + 3.5f, 0f), new Vector2(1.6f, 5f), -139);
            }

            if (style.cilia)
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

            SetupUtils.SpriteChild(go.transform, "Fill", PlaceholderArt.Square, currentStyle.floor,
                new Vector3(0f, -depth, 0f), new Vector2(width, depth), -100);
            SetupUtils.SpriteChild(go.transform, "Edge", PlaceholderArt.Square, currentStyle.floorEdge,
                new Vector3(0f, -0.2f, 0f), new Vector2(width, 0.2f), -99);
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

            SetupUtils.SpriteChild(go.transform, "Sprite", PlaceholderArt.Square, Color.Lerp(currentStyle.floor, Color.black, 0.1f),
                new Vector3(0f, -thickness, 0f), new Vector2(width, thickness), -90);
            SetupUtils.SpriteChild(go.transform, "Edge", PlaceholderArt.Square, currentStyle.floorEdge,
                new Vector3(0f, -0.12f, 0f), new Vector2(width, 0.12f), -89);

            if (currentStyle.platformStems)
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

            if (currentStyle.drip.a > 0f)
            {
                // Plataforma de carne tipo hongo: borde redondeado y gotas colgando.
                SetupUtils.SpriteChild(go.transform, "Tapa", PlaceholderArt.Circle, Color.Lerp(currentStyle.floor, Color.white, 0.12f),
                    new Vector3(0f, -0.18f, 0f), new Vector2(width + 0.4f, 0.5f), -91);
                int drips = Mathf.Max(2, Mathf.RoundToInt(width / 0.8f));
                for (int i = 0; i < drips; i++)
                {
                    float x = -width * 0.5f + (i + 0.5f) * width / drips + ((i * 37) % 10 - 5) * 0.02f;
                    float len = 0.25f + (i % 3) * 0.18f;
                    SetupUtils.SpriteChild(go.transform, "Gota" + i, PlaceholderArt.Capsule, currentStyle.drip,
                        new Vector3(x, -thickness - len * 0.5f + 0.05f, 0f), new Vector2(len, 0.14f), -88)
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

        /// <summary>Pasaje secreto; 'groundY' = piso o plataforma donde está.</summary>
        private void Secret(float x, float groundY)
        {
            var secret = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Secret.gameObject);
            secret.transform.position = new Vector3(x, groundY + 0.7f, 0f);
        }

        private void BuildEstomago()
        {
            BuildLevelBase(100f, new LevelStyle
            {
                // Estómago: tejido carmesí, plataformas de carne rosada que escurren ácido verde brillante.
                background = new Color(0.1f, 0.02f, 0.04f),
                wall = new Color(0.32f, 0.06f, 0.1f),
                floor = new Color(0.6f, 0.2f, 0.28f),
                floorEdge = new Color(0.95f, 0.45f, 0.55f),
                blob = new Color(0.55f, 0.15f, 0.22f, 0.6f),
                decor = Decor.Veins,
                detail = new Color(0.15f, 0.55f, 0.6f, 0.7f),
                pitLiquid = new Color(0.5f, 1f, 0.15f, 1f),
                drip = new Color(0.55f, 1f, 0.2f, 0.9f),
                glow = new Color(0.45f, 1f, 0.2f, 1f)
            }, "NODO: Estás en el estómago. Muévete con A/D, salta con K o Espacio. Apunta con W (W + A/D = diagonal).",
                new Vector2(31f, 2.5f), new Vector2(65.5f, 2.5f));

            // Plataformas para practicar el salto y el disparo hacia abajo.
            Platform(0f, 2.2f, 3f);
            Platform(4.5f, 3.6f, 2.5f);

            // Tutorial: un enemigo a la vez.
            Arena("Arena_1_Tutorial", 8f, 18f,
                "H. pylori: una bacteria que se entierra en el moco y sale debajo de ti (enterrada no le haces daño). Cambia de cápsula con Q/E hasta encontrar la que funciona.",
                W(0.8f, E(prefabs.HPylori, data.HPylori, 1)),
                W(0.8f, E(prefabs.HPylori, data.HPylori, 2)));

            Acid(27.5f);

            // Pasaje secreto en la plataforma más alta, entre la primera y la segunda arena (desbloquea la Boca).
            Platform(35.5f, 2.2f, 3f);
            Platform(39f, 4.2f, 2.5f);
            Secret(39.3f, 4.2f);

            Arena("Arena_2_Escudo", 44f, 18f,
                "Escudo: mantén Shift (o I). Si lo levantas justo antes del golpe haces PARRY. Bomba de medicamento: L.",
                W(0.6f, E(prefabs.HPylori, data.HPylori, 2), E(prefabs.HPylori, data.HPylori, 1, SpawnSide.Left)),
                W(1.0f, E(prefabs.HPylori, data.HPylori, 3, SpawnSide.Both)));
            Platform(50f, 2.4f, 4f);

            Acid(62f);

            Arena("Arena_3_Acido", 72f, 20f, "Ácido gástrico: sáltalo. El escudo no te protege.",
                W(0.6f, E(prefabs.HPylori, data.HPylori, 3)),
                W(1.0f, E(prefabs.HPylori, data.HPylori, 2), E(prefabs.HPylori, data.HPylori, 2, SpawnSide.Left)));
            Acid(80f);
            Platform(75f, 2.2f, 3f);
            Platform(85f, 2.2f, 3f);
        }

        private void BuildDelgado()
        {
            BuildLevelBase(95f, new LevelStyle
            {
                // Intestino delgado: tejido rosa coral con vellosidades y bilis amarilla verdosa.
                background = new Color(0.11f, 0.03f, 0.05f),
                wall = new Color(0.36f, 0.1f, 0.16f),
                floor = new Color(0.72f, 0.3f, 0.36f),
                floorEdge = new Color(1f, 0.6f, 0.62f),
                blob = new Color(0.8f, 0.38f, 0.45f, 0.75f),
                decor = Decor.Villi,
                detail = new Color(0.2f, 0.6f, 0.65f, 0.55f),
                pitLiquid = new Color(0.8f, 0.95f, 0.2f, 1f),
                drip = new Color(0.8f, 0.95f, 0.25f, 0.85f),
                glow = new Color(0.75f, 1f, 0.3f, 1f)
            }, "NODO: Intestino delgado. Aquí viven los parásitos: usa la cápsula Antiparasitario.",
                new Vector2(28f, 2.5f), new Vector2(55f, 3f));

            Platform(3f, 2.2f, 3f);

            Arena("Arena_1_Giardia", 10f, 18f, "Giardia: salta y se te pega (te frena y te daña). Dispárale para despegarla. Antiparasitario (el antibiótico también sirve).",
                W(0.6f, E(prefabs.Giardia, data.Giardia, 2)),
                W(1.0f, E(prefabs.Giardia, data.Giardia, 2, SpawnSide.Both)));
            Platform(18f, 2.4f, 3.5f);

            Arena("Arena_2_Rotavirus", 36f, 18f, "Rotavirus (rota = rueda): rueda voladora. Cuando gira más rápido y se echa para atrás, se va a lanzar: muévete o usa el escudo (rebota). Antiviral.",
                W(0.6f, E(prefabs.Rotavirus, data.Rotavirus, 2)),
                W(1.0f, E(prefabs.Rotavirus, data.Rotavirus, 1), E(prefabs.Giardia, data.Giardia, 2, SpawnSide.Left)));
            Platform(40f, 2.2f, 3f);
            Platform(47f, 3.4f, 3f);

            // La lombriz aparece, pelea un rato y huye al intestino grueso. Arena sin plataformas.
            Arena("Arena_3_Lombriz", 64f, 22f, null,
                W(1.0f, E(prefabs.LombrizDelgado, data.Lombriz, 1)));
        }

        private void BuildGrueso()
        {
            BuildLevelBase(95f, new LevelStyle
            {
                // Intestino grueso: más oscuro y pesado, pliegues en arco y líquido verde olivo turbio.
                background = new Color(0.07f, 0.03f, 0.03f),
                wall = new Color(0.26f, 0.08f, 0.09f),
                floor = new Color(0.5f, 0.2f, 0.2f),
                floorEdge = new Color(0.85f, 0.42f, 0.4f),
                blob = new Color(0.42f, 0.13f, 0.15f, 0.85f),
                decor = Decor.Folds,
                detail = new Color(0.15f, 0.45f, 0.45f, 0.6f),
                pitLiquid = new Color(0.55f, 0.7f, 0.15f, 1f),
                drip = new Color(0.6f, 0.75f, 0.2f, 0.8f),
                glow = new Color(0.55f, 0.85f, 0.2f, 1f)
            }, "NODO: Intestino grueso. La lombriz está cerca.",
                new Vector2(29f, 3f));

            Arena("Arena_1_Amebas", 10f, 18f, "Entamoeba: ameba lenta; su seudópodo llega lejos y te jala. No te quedes enfrente. Antiparasitario.",
                W(0.6f, E(prefabs.Entamoeba, data.Entamoeba, 1), E(prefabs.Oxiuro, data.Oxiuro, 2, SpawnSide.Left)),
                W(1.0f, E(prefabs.Entamoeba, data.Entamoeba, 1), E(prefabs.Oxiuro, data.Oxiuro, 2, SpawnSide.Both)));
            Platform(14f, 2.2f, 3f);

            Platform(31f, 2.4f, 2.5f);

            Arena("Arena_2_CDifficile", 36f, 18f, "C. difficile: herida se vuelve espora y todo le rebota; espera a que despierte. Solo el antibiótico.",
                W(0.6f, E(prefabs.CDifficile, data.CDifficile, 2), E(prefabs.Oxiuro, data.Oxiuro, 1, SpawnSide.Left)),
                W(1.0f, E(prefabs.CDifficile, data.CDifficile, 2), E(prefabs.Entamoeba, data.Entamoeba, 1, SpawnSide.Left)));
            Platform(42f, 2.2f, 3f);
            Platform(48f, 2.2f, 3f);

            // Pelea final: desde la fase 2 invoca oxiuros. Arena sin plataformas.
            Arena("Arena_3_LombrizFinal", 64f, 22f, null,
                W(1.0f, E(prefabs.LombrizGrueso, data.Lombriz, 1)));
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
                glow = new Color(0.9f, 0.95f, 1f, 1f)
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
                cilia = true
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
                cilia = true
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
                cilia = true
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
