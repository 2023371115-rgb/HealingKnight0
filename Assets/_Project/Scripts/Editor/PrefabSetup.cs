using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Hazards;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealingKnight.EditorTools
{
    /// <summary>
    /// Crea los prefabs con figuras de placeholder. Cuando tengas el arte, reemplaza lo que está
    /// dentro de "Visual" por tus piezas: los scripts de la raíz no cambian.
    /// </summary>
    internal class PrefabSetup
    {
        public const string Folder = "Assets/_Project/Prefabs";

        public Projectile Capsule, EnemyProjectile;
        public MedicineBomb Bomb;
        public PlayerController Player;
        public EnemyHealth HPylori, Giardia, Rotavirus, Entamoeba, CDifficile, Oxiuro, SMutans;
        public EnemyHealth LombrizDelgado, LombrizGrueso;
        public EnemyHealth Rinovirus, Polen, SAureus, Pertussis, Neumococo, Influenza, Tuberculosis;
        public EnemyHealth Mucosidad, Inflamacion, Colmena, RabiaJefe;
        public EnemyHealth Poliovirus, Meningococo, Herpes, Rabia;
        public EnemyHealth SAureusCoco, OxiuroHuevo;
        public HealthyNeuron Neuron;
        public EnemyHealth RedNeuron;
        public LightningStorm Storm;
        public AcidPool Acid;
        public SecretPassage Secret;

        private readonly DataSetup data;

        public PrefabSetup(DataSetup data)
        {
            this.data = data;
        }

        public void Run()
        {
            Capsule = LoadOrBuild<Projectile>($"{Folder}/Projectiles/Capsule.prefab", () => BuildProjectile("Capsule", new Vector2(0.55f, 0.28f)));
            EnemyProjectile = LoadOrBuild<Projectile>($"{Folder}/Projectiles/EnemyProjectile.prefab", () => BuildProjectile("EnemyProjectile", new Vector2(0.4f, 0.4f)));
            Bomb = LoadOrBuild<MedicineBomb>($"{Folder}/Projectiles/MedicineBomb.prefab", BuildBomb);
            Player = LoadOrBuild<PlayerController>($"{Folder}/Player/HealingKnight.prefab", BuildPlayer);

            HPylori = Enemy("Bacteria_HPylori", data.HPylori, EnemyShape.Capsule);
            Giardia = Enemy("Parasito_Giardia", data.Giardia, EnemyShape.Pear);
            Rotavirus = Enemy("Virus_Rotavirus", data.Rotavirus, EnemyShape.Wheel);
            Entamoeba = Enemy("Parasito_Entamoeba", data.Entamoeba, EnemyShape.Blob);
            CDifficile = Enemy("Bacteria_CDifficile", data.CDifficile, EnemyShape.Capsule);
            Oxiuro = Enemy("Parasito_Oxiuro", data.Oxiuro, EnemyShape.Long);
            SMutans = Enemy("Bacteria_SMutans", data.SMutans, EnemyShape.Blob);

            LombrizDelgado = LoadOrBuild<EnemyHealth>($"{Folder}/Bosses/Lombriz_IntestinoDelgado.prefab",
                () => BuildLombriz("Lombriz_IntestinoDelgado", 0.5f, null, null,
                    "¡LA LOMBRIZ! Usa Antiparasitario. Salta sobre su embestida.",
                    "NODO: ¡Escapa hacia el intestino grueso! Síguela."));
            LombrizGrueso = LoadOrBuild<EnemyHealth>($"{Folder}/Bosses/Lombriz_IntestinoGrueso.prefab",
                () => BuildLombriz("Lombriz_IntestinoGrueso", 0f, Oxiuro, data.Oxiuro,
                    "NODO: Aquí se esconde. Acaba con ella.",
                    "NODO: Foco eliminado. Fórmula 1 obtenida."));

            // Sistema Respiratorio
            Rinovirus = Enemy("Virus_Rinovirus", data.Rinovirus, EnemyShape.Round);
            Polen = Enemy("Alergeno_Polen", data.Polen, EnemyShape.Pollen);
            SAureus = Enemy("Bacteria_SAureus", data.SAureus, EnemyShape.Cluster);
            Pertussis = Enemy("Bacteria_Pertussis", data.Pertussis, EnemyShape.Capsule);
            Neumococo = Enemy("Bacteria_Neumococo", data.Neumococo, EnemyShape.Diplo);
            Influenza = Enemy("Virus_Influenza", data.Influenza, EnemyShape.Round);
            Tuberculosis = Enemy("Bacteria_Tuberculosis", data.Tuberculosis, EnemyShape.Capsule);

            // Crías: el racimo suelta cocos; el oxiuro deja un huevo y del huevo nace otro oxiuro.
            SAureusCoco = Enemy("Bacteria_SAureus_Coco", data.SAureusCoco, EnemyShape.Cluster);
            OxiuroHuevo = Enemy("Parasito_Oxiuro_Huevo", data.OxiuroHuevo, EnemyShape.Egg);
            LinkChild(data.SAureus, SAureusCoco, data.SAureusCoco);
            LinkChild(data.Oxiuro, OxiuroHuevo, data.OxiuroHuevo);
            LinkChild(data.OxiuroHuevo, Oxiuro, data.Oxiuro);
            Mucosidad = LoadOrBuild<EnemyHealth>($"{Folder}/Hazards/Bloqueo_Mucosidad.prefab", () => BuildObstacle("Bloqueo_Mucosidad", data.Mucosidad, true));
            Inflamacion = LoadOrBuild<EnemyHealth>($"{Folder}/Hazards/Bloqueo_Inflamacion.prefab", () => BuildObstacle("Bloqueo_Inflamacion", data.Inflamacion, false));

            // Sistema Nervioso
            Poliovirus = Enemy("Virus_Poliovirus", data.Poliovirus, EnemyShape.Round);
            Meningococo = Enemy("Bacteria_Meningococo", data.Meningococo, EnemyShape.Diplo);
            Herpes = Enemy("Virus_Herpes", data.Herpes, EnemyShape.Round);
            Rabia = Enemy("Virus_Rabia", data.Rabia, EnemyShape.Bullet);
            Neuron = LoadOrBuild<HealthyNeuron>($"{Folder}/Level/Neurona_Sana.prefab", BuildNeuron);
            RedNeuron = LoadOrBuild<EnemyHealth>($"{Folder}/Level/Neurona_Hiperexcitada.prefab", BuildRedNeuron);
            if (Neuron != null && RedNeuron != null)
            {
                // La neurona sana se infecta y se vuelve roja (referencia cruzada entre los dos prefabs).
                SetupUtils.SetRef(Neuron, "infectedPrefab", RedNeuron);
                EditorUtility.SetDirty(Neuron.gameObject);
            }
            Storm = LoadOrBuild<LightningStorm>($"{Folder}/Hazards/Tormenta_Electrica.prefab", BuildStorm);

            Colmena = LoadOrBuild<EnemyHealth>($"{Folder}/Bosses/Colmena_Pulmones.prefab", BuildHive);
            RabiaJefe = LoadOrBuild<EnemyHealth>($"{Folder}/Bosses/Rabia_Hipocampo.prefab", BuildRabiesBoss);

            Secret = LoadOrBuild<SecretPassage>($"{Folder}/Level/SecretPassage.prefab", BuildSecret);

            Acid = LoadOrBuild<AcidPool>($"{Folder}/Hazards/AcidPool.prefab", BuildAcid);
        }

        private static void LinkChild(EnemyData parent, EnemyHealth childPrefab, EnemyData childData)
        {
            if (parent == null || childPrefab == null) return;
            parent.childPrefab = childPrefab.gameObject;
            parent.childData = childData;
            EditorUtility.SetDirty(parent);
        }

        private EnemyHealth Enemy(string name, EnemyData enemyData, EnemyShape shape)
        {
            return LoadOrBuild<EnemyHealth>($"{Folder}/Enemies/{name}.prefab", () => BuildEnemy(name, enemyData, shape));
        }

        private static T LoadOrBuild<T>(string path, System.Func<GameObject> build) where T : Component
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<T>();
            return SetupUtils.SavePrefab<T>(build(), path);
        }

        // ---------- Física ----------

        private PhysicsMaterial2D noFriction;

        /// <summary>Sin fricción: evita que los personajes se queden pegados a las paredes al saltar.</summary>
        private PhysicsMaterial2D NoFriction
        {
            get
            {
                if (noFriction != null) return noFriction;
                const string path = "Assets/_Project/Data/Physics/NoFriction.physicsMaterial2D";
                noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
                if (noFriction == null)
                {
                    SetupUtils.EnsureFolder("Assets/_Project/Data/Physics");
                    noFriction = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
                    AssetDatabase.CreateAsset(noFriction, path);
                }
                return noFriction;
            }
        }

        /// <summary>Rigidbody2D + BoxCollider2D + CharacterMotor2D con los pies en el origen del objeto.</summary>
        private CharacterMotor2D AddMotor(GameObject root, Transform visual, Vector2 colliderSize)
        {
            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var box = root.AddComponent<BoxCollider2D>();
            box.size = colliderSize;
            box.offset = new Vector2(0f, colliderSize.y * 0.5f);
            box.sharedMaterial = NoFriction;

            var motor = root.AddComponent<CharacterMotor2D>();
            SetupUtils.SetRef(motor, "visual", visual);
            return motor;
        }

        private static void SetHurtbox(Hurtbox hurtbox, Vector2 offset, Vector2 size)
        {
            SetupUtils.SetVector2(hurtbox, "offset", offset);
            SetupUtils.SetVector2(hurtbox, "size", size);
        }

        // ---------- Proyectiles ----------

        private GameObject BuildProjectile(string name, Vector2 size)
        {
            var root = new GameObject(name);
            root.AddComponent<SortingGroup>().sortingOrder = 20;
            var projectile = root.AddComponent<Projectile>();
            var sr = SetupUtils.SpriteChild(root.transform, "Sprite", PlaceholderArt.Capsule, Color.white, Vector3.zero, size, 0);
            SetupUtils.SetRef(projectile, "spriteRenderer", sr);
            return root;
        }

        private GameObject BuildBomb()
        {
            var root = new GameObject("MedicineBomb");
            root.AddComponent<SortingGroup>().sortingOrder = 21;
            var bomb = root.AddComponent<MedicineBomb>();
            var sr = SetupUtils.SpriteChild(root.transform, "Sprite", PlaceholderArt.Circle, Color.white, Vector3.zero, new Vector2(0.45f, 0.45f), 1);
            SetupUtils.SpriteChild(sr.transform, "Cap", PlaceholderArt.SquareCentered, new Color(1f, 1f, 1f, 0.9f),
                new Vector3(0f, 0.45f, 0f), new Vector2(0.35f, 0.25f), 2);
            var blast = SetupUtils.SpriteChild(root.transform, "Blast", PlaceholderArt.Circle, new Color(1f, 1f, 1f, 0.6f), Vector3.zero, Vector2.one, 0);
            blast.enabled = false;
            SetupUtils.SetRef(bomb, "spriteRenderer", sr);
            SetupUtils.SetRef(bomb, "blastRenderer", blast);
            return root;
        }

        // ---------- Jugador ----------

        private GameObject BuildPlayer()
        {
            PlayerConfig config = data.PlayerConfig;
            var root = new GameObject("HealingKnight");
            root.layer = PhysicsLayers.Player;
            root.AddComponent<SortingGroup>().sortingOrder = 10;

            // Visual: se voltea al mirar a la izquierda. Los pies están en (0, 0).
            GameObject visual = SetupUtils.Child(root.transform, "Visual", Vector3.zero);
            Transform v = visual.transform;

            // Body: se encoge entero al presionar abajo (PlayerController.Size).
            GameObject bodyGo = SetupUtils.Child(v, "Body", Vector3.zero);
            Transform b = bodyGo.transform;
            GameObject armPivot;
            SpriteRenderer arm;
            var colorParts = new System.Collections.Generic.List<Object>();
            SpriteRenderer shieldSprite;

            if (PlayerArt.Available)
            {
                // Muñeco articulado: cada pieza con su capa de color encima (la pinta el medicamento).
                // Orden de dibujo: pierna de atrás < pierna de enfrente < torso < brazo < escudo.
                SpriteRenderer Piece(Transform parent, PlayerArt.Piece piece, Vector3 localPos, int order)
                {
                    SetupUtils.SpriteChild(parent, piece.file, PlayerArt.Sprite(piece), Color.white, localPos, Vector2.one, order);
                    SpriteRenderer col = SetupUtils.SpriteChild(parent, piece.file + "_Color", PlayerArt.ColorSprite(piece), Color.white, localPos, Vector2.one, order + 1);
                    colorParts.Add(col);
                    return col;
                }

                Transform Joint(Transform parent, string name, Vector2 localPos) =>
                    SetupUtils.Child(parent, name, new Vector3(localPos.x, localPos.y, 0f)).transform;

                // Piernas: muslo (gira en la cadera) y, colgando de él, rodilla+pie (gira en la rodilla).
                Vector2 kneeOffset = PlayerArt.FrontShin.position - PlayerArt.FrontThigh.position;

                // Pierna de atrás: mismo dibujo que la de enfrente, inclinada hacia atrás en reposo.
                // "PiernaAtras" guarda la inclinación; "MusloAtras" es lo que mueve el animador.
                Transform backHip = Joint(b, "PiernaAtras", PlayerArt.BackHip);
                backHip.localRotation = Quaternion.Euler(0f, 0f, PlayerArt.BackLegRestAngle);
                Transform backThigh = Joint(backHip, "MusloAtras", Vector2.zero);
                Piece(backThigh, PlayerArt.FrontThigh, Vector3.zero, 0);
                Transform backKneeRest = Joint(backThigh, "RodillaAtrasReposo", kneeOffset);
                backKneeRest.localRotation = Quaternion.Euler(0f, 0f, -PlayerArt.BackLegRestAngle); // el pie queda plano
                Transform backKnee = Joint(backKneeRest, "RodillaAtras", Vector2.zero);
                Piece(backKnee, PlayerArt.FrontShin, Vector3.zero, 2);

                Transform frontThigh = Joint(b, "PiernaFrente", PlayerArt.FrontThigh.position);
                Piece(frontThigh, PlayerArt.FrontThigh, Vector3.zero, 4);
                Transform frontKnee = Joint(frontThigh, "RodillaFrente", kneeOffset);
                Piece(frontKnee, PlayerArt.FrontShin, Vector3.zero, 6);

                // Parte de arriba (torso + brazo): sube y baja un poco al caminar.
                Transform upper = SetupUtils.Child(b, "Arriba", Vector3.zero).transform;
                Piece(upper, PlayerArt.Torso, new Vector3(PlayerArt.Torso.position.x, PlayerArt.Torso.position.y, 0f), 8);

                // Brazo-cañón: gira desde el hombro hacia donde apuntas.
                armPivot = SetupUtils.Child(upper, "ArmPivot", new Vector3(PlayerArt.Arm.position.x, PlayerArt.Arm.position.y, 0f));
                SetupUtils.SpriteChild(armPivot.transform, PlayerArt.Arm.file, PlayerArt.Sprite(PlayerArt.Arm), Color.white, Vector3.zero, Vector2.one, 10);
                arm = SetupUtils.SpriteChild(armPivot.transform, PlayerArt.Arm.file + "_Color", PlayerArt.ColorSprite(PlayerArt.Arm), Color.white, Vector3.zero, Vector2.one, 11);

                // Escudo burbuja: dentro de Body para que se encoja junto con el caballero.
                shieldSprite = SetupUtils.SpriteChild(b, "ShieldBubble", PlaceholderArt.Circle, new Color(0.03f, 0.91f, 0.95f, 0.45f),
                    new Vector3(0f, 1.0f, 0f), new Vector2(2.4f, 2.4f), 12);

                var rigAnimator = root.AddComponent<PlayerRigAnimator>();
                SetupUtils.SetRef(rigAnimator, "upper", upper);
                SetupUtils.SetRef(rigAnimator, "backThigh", backThigh);
                SetupUtils.SetRef(rigAnimator, "backKnee", backKnee);
                SetupUtils.SetRef(rigAnimator, "frontThigh", frontThigh);
                SetupUtils.SetRef(rigAnimator, "frontKnee", frontKnee);
            }
            else
            {
                // Sin arte: figuras de relleno.
                SetupUtils.SpriteChild(b, "BackLeg", PlaceholderArt.Square, new Color(0.65f, 0.67f, 0.72f), new Vector3(-0.15f, 0f, 0f), new Vector2(0.25f, 0.45f), -2);
                SetupUtils.SpriteChild(b, "FrontLeg", PlaceholderArt.Square, new Color(0.85f, 0.87f, 0.9f), new Vector3(0.12f, 0f, 0f), new Vector2(0.25f, 0.45f), 1);
                SetupUtils.SpriteChild(b, "Torso", PlaceholderArt.Square, new Color(0.92f, 0.93f, 0.96f), new Vector3(0f, 0.4f, 0f), new Vector2(0.75f, 0.6f), 0);
                SetupUtils.SpriteChild(b, "Head", PlaceholderArt.CircleBottom, new Color(0.95f, 0.96f, 0.98f), new Vector3(0f, 0.95f, 0f), new Vector2(1.05f, 1.0f), 3);
                SetupUtils.SpriteChild(b, "Visor", PlaceholderArt.SquareCentered, new Color(0.3f, 0.9f, 1f), new Vector3(0.22f, 1.48f, 0f), new Vector2(0.55f, 0.14f), 4);
                armPivot = SetupUtils.Child(b, "ArmPivot", new Vector3(0.1f, config.muzzleHeight, 0f));
                arm = SetupUtils.SpriteChild(armPivot.transform, "ArmCannon", PlaceholderArt.Capsule, Color.white,
                    new Vector3(0.35f, 0f, 0f), new Vector2(0.8f, 0.32f), 5);
                shieldSprite = SetupUtils.SpriteChild(b, "ShieldBubble", PlaceholderArt.Circle, new Color(0.03f, 0.91f, 0.95f, 0.45f),
                    new Vector3(0.05f, 0.95f, 0f), new Vector2(2.1f, 2.1f), 8);
            }

            AddMotor(root, v, new Vector2(0.7f, config.standingColliderHeight));

            root.AddComponent<PlayerInputReader>();
            var controller = root.AddComponent<PlayerController>();
            SetupUtils.SetRef(controller, "config", config);
            SetupUtils.SetRef(controller, "bodyVisual", b);

            var shooter = root.AddComponent<PlayerShooter>();
            SetupUtils.SetRef(shooter, "capsulePrefab", Capsule);
            SetupUtils.SetRef(shooter, "armPivot", armPivot.transform);
            SetupUtils.SetRef(shooter, "tintedArm", arm);
            if (colorParts.Count > 0) SetupUtils.SetRefArray(shooter, "medicationColorParts", colorParts.ToArray());
            SetupUtils.SetRef(shooter, "fallbackRegionalMedication", data.Antiparasitario);

            var bombs = root.AddComponent<PlayerBombs>();
            SetupUtils.SetRef(bombs, "bombPrefab", Bomb);

            var shield = root.AddComponent<PlayerShield>();
            SetupUtils.SetRef(shield, "shieldVisual", shieldSprite.gameObject);

            root.AddComponent<PlayerHealth>();

            var hurtbox = root.AddComponent<Hurtbox>();
            SetHurtbox(hurtbox, new Vector2(0f, config.standingColliderHeight * 0.5f), new Vector2(0.75f, config.standingColliderHeight));

            return root;
        }

        // ---------- Enemigos ----------

        private enum EnemyShape { Long, Capsule, Round, Blob, Pear, Cluster, Diplo, Pollen, Bullet, Egg, Wheel }

        private GameObject BuildEnemy(string name, EnemyData enemyData, EnemyShape shape)
        {
            float s = enemyData.visualScale;
            bool flying = enemyData.movement == EnemyMovement.Flying;
            var root = new GameObject(name);
            root.layer = PhysicsLayers.Enemy;
            root.AddComponent<SortingGroup>().sortingOrder = 5;

            // Los de suelo tienen los pies en (0, 0); los voladores, el centro.
            GameObject visual = SetupUtils.Child(root.transform, "Visual", Vector3.zero);
            SpriteRenderer bodySprite;
            Vector2 bodySize;

            switch (shape)
            {
                case EnemyShape.Long: // parásito: alargado y pegado al piso
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.CircleBottom, enemyData.tint,
                        Vector3.zero, new Vector2(1.4f * s, 0.6f * s), 0);
                    bodySize = new Vector2(1.3f * s, 0.6f * s);
                    break;
                case EnemyShape.Capsule: // bacteria: cápsula/bastón de pie
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.Capsule, enemyData.tint,
                        new Vector3(0f, 0.55f * s, 0f), new Vector2(1.1f * s, 0.55f * s), 0);
                    bodySprite.transform.localRotation = Quaternion.Euler(0f, 0f, 70f);
                    bodySize = new Vector2(0.6f * s, 1.1f * s);
                    break;
                case EnemyShape.Blob: // ameba / colonia: mancha pegada al piso
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.CircleBottom, enemyData.tint,
                        Vector3.zero, new Vector2(1.1f * s, 0.85f * s), 0);
                    SetupUtils.SpriteChild(visual.transform, "Lobe", PlaceholderArt.CircleBottom, Color.Lerp(enemyData.tint, Color.white, 0.2f),
                        new Vector3(-0.35f * s, 0f, 0f), new Vector2(0.6f * s, 0.55f * s), -1);
                    bodySize = new Vector2(1.1f * s, 0.85f * s);
                    break;
                case EnemyShape.Pear: // giardia: forma de pera con "ojos"
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.CircleBottom, enemyData.tint,
                        new Vector3(0f, 0.1f * s, 0f), new Vector2(0.9f * s, 1.0f * s), 0);
                    SetupUtils.SpriteChild(visual.transform, "Tail", PlaceholderArt.Capsule, Color.Lerp(enemyData.tint, Color.black, 0.15f),
                        new Vector3(-0.45f * s, 0.35f * s, 0f), new Vector2(0.6f * s, 0.18f * s), -1);
                    bodySize = new Vector2(0.85f * s, 1.1f * s);
                    break;
                case EnemyShape.Cluster: // estafilococo: racimo de esferas
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.Circle, enemyData.tint,
                        new Vector3(0f, 0.4f * s, 0f), new Vector2(0.55f * s, 0.55f * s), 0);
                    SetupUtils.SpriteChild(visual.transform, "Ball2", PlaceholderArt.Circle, Color.Lerp(enemyData.tint, Color.black, 0.1f),
                        new Vector3(-0.32f * s, 0.25f * s, 0f), new Vector2(0.5f * s, 0.5f * s), -1);
                    SetupUtils.SpriteChild(visual.transform, "Ball3", PlaceholderArt.Circle, Color.Lerp(enemyData.tint, Color.black, 0.15f),
                        new Vector3(0.3f * s, 0.22f * s, 0f), new Vector2(0.48f * s, 0.48f * s), -1);
                    SetupUtils.SpriteChild(visual.transform, "Ball4", PlaceholderArt.Circle, Color.Lerp(enemyData.tint, Color.white, 0.15f),
                        new Vector3(-0.05f * s, 0.75f * s, 0f), new Vector2(0.45f * s, 0.45f * s), -1);
                    bodySize = new Vector2(1f * s, 1f * s);
                    break;
                case EnemyShape.Diplo: // neumococo: diplococo (dos esferas juntas)
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.Circle, enemyData.tint,
                        new Vector3(0.22f * s, 0.32f * s, 0f), new Vector2(0.62f * s, 0.62f * s), 0);
                    SetupUtils.SpriteChild(visual.transform, "Twin", PlaceholderArt.Circle, Color.Lerp(enemyData.tint, Color.black, 0.12f),
                        new Vector3(-0.25f * s, 0.32f * s, 0f), new Vector2(0.62f * s, 0.62f * s), -1);
                    bodySize = new Vector2(1.05f * s, 0.65f * s);
                    break;
                case EnemyShape.Bullet: // virus de la rabia: forma de bala
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.Capsule, enemyData.tint,
                        Vector3.zero, new Vector2(1.2f * s, 0.55f * s), 0);
                    SetupUtils.SpriteChild(visual.transform, "Tip", PlaceholderArt.Circle, Color.Lerp(enemyData.tint, Color.white, 0.25f),
                        new Vector3(0.45f * s, 0f, 0f), new Vector2(0.45f * s, 0.5f * s), 1);
                    for (int i = 0; i < 5; i++)
                        SetupUtils.SpriteChild(visual.transform, "Spike" + i, PlaceholderArt.Circle, Color.Lerp(enemyData.tint, Color.black, 0.2f),
                            new Vector3((-0.4f + i * 0.2f) * s, 0.3f * s, 0f), new Vector2(0.12f * s, 0.12f * s), -1);
                    bodySize = new Vector2(1.2f * s, 0.6f * s);
                    break;
                case EnemyShape.Wheel: // rotavirus: rueda con aro, rayos y bordes dentados (así se ve al microscopio)
                {
                    Color dark = Color.Lerp(enemyData.tint, Color.black, 0.35f);
                    Color light = Color.Lerp(enemyData.tint, Color.white, 0.35f);
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.Circle, enemyData.tint,
                        Vector3.zero, new Vector2(1.0f * s, 1.0f * s), 0);
                    SetupUtils.SpriteChild(visual.transform, "Inner", PlaceholderArt.Circle, dark,
                        Vector3.zero, new Vector2(0.66f * s, 0.66f * s), 1);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * 60f;
                        float r = a * Mathf.Deg2Rad;
                        SpriteRenderer spoke = SetupUtils.SpriteChild(visual.transform, "Spoke" + i, PlaceholderArt.Capsule, light,
                            new Vector3(Mathf.Cos(r) * 0.17f * s, Mathf.Sin(r) * 0.17f * s, 0f), new Vector2(0.34f * s, 0.07f * s), 2);
                        spoke.transform.localRotation = Quaternion.Euler(0f, 0f, a);
                    }
                    SetupUtils.SpriteChild(visual.transform, "Hub", PlaceholderArt.Circle, light,
                        Vector3.zero, new Vector2(0.2f * s, 0.2f * s), 3);
                    for (int i = 0; i < 14; i++)
                    {
                        float r = i * (360f / 14f) * Mathf.Deg2Rad;
                        SetupUtils.SpriteChild(visual.transform, "Tooth" + i, PlaceholderArt.Circle, dark,
                            new Vector3(Mathf.Cos(r) * 0.52f * s, Mathf.Sin(r) * 0.52f * s, 0f), new Vector2(0.13f * s, 0.13f * s), -1);
                    }
                    bodySize = new Vector2(1.0f * s, 1.0f * s);
                    break;
                }
                case EnemyShape.Egg: // huevo de oxiuro: ovalado, aplanado de un lado, con la larva adentro
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.CircleBottom, enemyData.tint,
                        Vector3.zero, new Vector2(0.75f * s * 2f, 1.1f * s * 2f), 0);
                    SetupUtils.SpriteChild(visual.transform, "Larva", PlaceholderArt.Capsule, new Color(0.85f, 0.8f, 0.6f, 0.8f),
                        new Vector3(0f, 0.5f * s, 0f), new Vector2(0.7f * s, 0.25f * s), 1);
                    bodySize = new Vector2(0.75f * s * 2f, 1.1f * s * 2f);
                    break;
                case EnemyShape.Pollen: // polen: esfera con muchas bolitas
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.Circle, enemyData.tint,
                        Vector3.zero, new Vector2(0.8f * s, 0.8f * s), 0);
                    for (int i = 0; i < 10; i++)
                    {
                        float a = i * 36f * Mathf.Deg2Rad;
                        SetupUtils.SpriteChild(bodySprite.transform, "Bump" + i, PlaceholderArt.Circle, Color.Lerp(enemyData.tint, new Color(0.8f, 0.5f, 0f), 0.35f),
                            new Vector3(Mathf.Cos(a) * 0.5f, Mathf.Sin(a) * 0.5f, 0f), new Vector2(0.24f, 0.24f), -1);
                    }
                    bodySize = new Vector2(0.85f * s, 0.85f * s);
                    break;
                default: // virus: esfera
                    bodySprite = SetupUtils.SpriteChild(visual.transform, "Body", PlaceholderArt.Circle, enemyData.tint,
                        flying ? Vector3.zero : new Vector3(0f, 0.45f * s, 0f), new Vector2(0.9f * s, 0.9f * s), 0);
                    // Espículas para que se lea como virus.
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * 60f * Mathf.Deg2Rad;
                        SetupUtils.SpriteChild(bodySprite.transform, "Spike" + i, PlaceholderArt.Circle, Color.Lerp(enemyData.tint, Color.white, 0.3f),
                            new Vector3(Mathf.Cos(a) * 0.55f, Mathf.Sin(a) * 0.55f, 0f), new Vector2(0.22f, 0.22f), -1);
                    }
                    bodySize = new Vector2(0.9f * s, 0.9f * s);
                    break;
            }

            // Ojo para saber hacia dónde mira.
            Vector3 eyeOffset = shape == EnemyShape.Capsule ? new Vector3(0.12f * s, 0.85f * s, 0f) : bodySprite.transform.localPosition + new Vector3(0.3f * s, 0.25f * s, 0f);
            if (shape != EnemyShape.Wheel) // la rueda gira: sin ojo
                SetupUtils.SpriteChild(visual.transform, "Eye", PlaceholderArt.Circle, Color.black, eyeOffset, new Vector2(0.14f * s, 0.14f * s), 1);

            if (!flying) AddMotor(root, visual.transform, new Vector2(Mathf.Min(bodySize.x, 0.9f), bodySize.y));

            var health = root.AddComponent<EnemyHealth>();
            SetupUtils.SetRef(health, "data", enemyData);
            SetupUtils.SetRef(health, "effectivenessTable", data.Table);
            SetupUtils.SetRefArray(health, "flashRenderers", new Object[] { bodySprite });

            var brain = root.AddComponent<EnemyBrain>();
            SetupUtils.SetRef(brain, "data", enemyData);
            SetupUtils.SetRef(brain, "visual", visual.transform);
            SetupUtils.SetRef(brain, "projectilePrefab", EnemyProjectile); // varios lanzan algo aunque peleen de cerca (ácido, gotitas)

            var hurtbox = root.AddComponent<Hurtbox>();
            Vector2 hurtSize = bodySize + new Vector2(0.15f, 0.15f);
            SetHurtbox(hurtbox, flying ? Vector2.zero : new Vector2(0f, hurtSize.y * 0.5f), hurtSize);

            return root;
        }

        // ---------- Jefe lombriz ----------

        private GameObject BuildLombriz(string name, float fleeAt, EnemyHealth minionPrefab, EnemyData minionData,
            string intro, string endMessage)
        {
            EnemyData d = data.Lombriz;
            var root = new GameObject(name);
            root.layer = PhysicsLayers.Enemy;
            root.AddComponent<SortingGroup>().sortingOrder = 4;

            // La raíz es la cabeza (centro).
            GameObject visual = SetupUtils.Child(root.transform, "Visual", Vector3.zero);
            SpriteRenderer head = SetupUtils.SpriteChild(visual.transform, "Head", PlaceholderArt.Circle, d.tint,
                Vector3.zero, new Vector2(1.4f, 1.3f), 2);
            SetupUtils.SpriteChild(visual.transform, "Mouth", PlaceholderArt.Circle, new Color(0.35f, 0.08f, 0.12f),
                new Vector3(0.5f, -0.05f, 0f), new Vector2(0.45f, 0.55f), 3);
            SetupUtils.SpriteChild(visual.transform, "Eye", PlaceholderArt.Circle, Color.black,
                new Vector3(0.2f, 0.35f, 0f), new Vector2(0.22f, 0.22f), 4);

            // Montículo de tierra que tiembla en el piso antes de salir (lo coloca el script).
            SpriteRenderer mound = SetupUtils.SpriteChild(root.transform, "Mound", PlaceholderArt.CircleBottom, new Color(0.45f, 0.25f, 0.2f, 0.95f),
                Vector3.zero, new Vector2(1.8f, 0.5f), 30);

            GameObject segmentsRoot = SetupUtils.Child(root.transform, "Segments", Vector3.zero);

            var health = root.AddComponent<EnemyHealth>();
            SetupUtils.SetRef(health, "data", d);
            SetupUtils.SetRef(health, "effectivenessTable", data.Table);
            SetupUtils.SetRefArray(health, "flashRenderers", new Object[] { head });

            var hurtbox = root.AddComponent<Hurtbox>();
            SetHurtbox(hurtbox, Vector2.zero, new Vector2(1.4f, 1.3f));

            var boss = root.AddComponent<LombrizBoss>();
            SetupUtils.SetFloat(boss, "fleeAtHealth01", fleeAt);
            SetupUtils.SetString(boss, "introMessage", intro);
            SetupUtils.SetString(boss, fleeAt > 0f ? "fleeMessage" : "defeatMessage", endMessage);
            SetupUtils.SetRef(boss, "visual", visual.transform);
            SetupUtils.SetRef(boss, "segmentsRoot", segmentsRoot.transform);
            SetupUtils.SetRef(boss, "segmentSprite", PlaceholderArt.Circle);
            SetupUtils.SetColor(boss, "segmentColor", d.tint);
            SetupUtils.SetRef(boss, "moundRenderer", mound);
            if (minionPrefab != null)
            {
                SetupUtils.SetRef(boss, "minionPrefab", minionPrefab);
                SetupUtils.SetRef(boss, "minionData", minionData);
            }
            return root;
        }

        // ---------- Sistema Nervioso ----------

        /// <summary>Neurona sana: cuerpo azul brillante con dendritas. Si la tocas con una cápsula, baja la salud del cerebro.</summary>
        private GameObject BuildNeuron()
        {
            var root = new GameObject("Neurona_Sana");
            root.AddComponent<SortingGroup>().sortingOrder = 2;
            var parts = new System.Collections.Generic.List<Object>();

            parts.Add(SetupUtils.SpriteChild(root.transform, "Halo", PlaceholderArt.Circle, new Color(0.35f, 0.7f, 1f, 0.25f),
                Vector3.zero, new Vector2(2.2f, 2.2f), -2));
            for (int i = 0; i < 6; i++)
            {
                float a = (i * 60f + 15f) * Mathf.Deg2Rad;
                float len = 0.9f + (i % 2) * 0.4f;
                var dendrite = SetupUtils.SpriteChild(root.transform, "Dendrita" + i, PlaceholderArt.Capsule, new Color(0.4f, 0.75f, 1f, 0.9f),
                    new Vector3(Mathf.Cos(a) * len * 0.5f, Mathf.Sin(a) * len * 0.5f, 0f), new Vector2(len, 0.14f), -1);
                dendrite.transform.localRotation = Quaternion.Euler(0f, 0f, i * 60f + 15f);
                parts.Add(dendrite);
            }
            parts.Add(SetupUtils.SpriteChild(root.transform, "Soma", PlaceholderArt.Circle, new Color(0.45f, 0.8f, 1f), Vector3.zero, new Vector2(0.75f, 0.75f), 0));
            parts.Add(SetupUtils.SpriteChild(root.transform, "Nucleo", PlaceholderArt.Circle, new Color(0.85f, 0.97f, 1f), Vector3.zero, new Vector2(0.3f, 0.3f), 1));

            var neuron = root.AddComponent<HealthyNeuron>();
            SetupUtils.SetRefArray(neuron, "renderers", parts.ToArray());
            var hurtbox = root.AddComponent<Hurtbox>();
            SetHurtbox(hurtbox, Vector2.zero, new Vector2(0.9f, 0.9f));
            return root;
        }

        /// <summary>Neurona hiperexcitada (roja): suelta descargas; se sana con anticonvulsivo y se vuelve sana.</summary>
        private GameObject BuildRedNeuron()
        {
            var root = new GameObject("Neurona_Hiperexcitada");
            root.layer = PhysicsLayers.Enemy;
            root.AddComponent<SortingGroup>().sortingOrder = 2;
            var parts = new System.Collections.Generic.List<Object>();

            var ring = SetupUtils.SpriteChild(root.transform, "Descarga", PlaceholderArt.Circle, new Color(1f, 0.5f, 0.9f, 0.3f),
                Vector3.zero, new Vector2(1f, 1f), -3);
            parts.Add(SetupUtils.SpriteChild(root.transform, "Halo", PlaceholderArt.Circle, new Color(1f, 0.2f, 0.25f, 0.3f),
                Vector3.zero, new Vector2(2.2f, 2.2f), -2));
            for (int i = 0; i < 7; i++)
            {
                float a = (i * 51f + 10f) * Mathf.Deg2Rad;
                float len = 0.8f + (i % 3) * 0.35f;
                var dendrite = SetupUtils.SpriteChild(root.transform, "Dendrita" + i, PlaceholderArt.Capsule, new Color(1f, 0.3f, 0.35f, 0.95f),
                    new Vector3(Mathf.Cos(a) * len * 0.5f, Mathf.Sin(a) * len * 0.5f, 0f), new Vector2(len, 0.13f), -1);
                dendrite.transform.localRotation = Quaternion.Euler(0f, 0f, i * 51f + 10f);
                parts.Add(dendrite);
            }
            parts.Add(SetupUtils.SpriteChild(root.transform, "Soma", PlaceholderArt.Circle, new Color(1f, 0.25f, 0.3f), Vector3.zero, new Vector2(0.8f, 0.8f), 0));
            var core = SetupUtils.SpriteChild(root.transform, "Nucleo", PlaceholderArt.Circle, new Color(1f, 0.85f, 0.6f), Vector3.zero, new Vector2(0.32f, 0.32f), 1);

            var health = root.AddComponent<EnemyHealth>();
            SetupUtils.SetRef(health, "data", data.NeuronaHiperexcitada);
            SetupUtils.SetRef(health, "effectivenessTable", data.Table);
            SetupUtils.SetRefArray(health, "flashRenderers", parts.ToArray());

            var hurtbox = root.AddComponent<Hurtbox>();
            SetHurtbox(hurtbox, Vector2.zero, new Vector2(1f, 1f));

            var neuron = root.AddComponent<HyperexcitedNeuron>();
            SetupUtils.SetRef(neuron, "healedPrefab", Neuron);
            SetupUtils.SetRef(neuron, "ring", ring);
            SetupUtils.SetRef(neuron, "core", core);
            return root;
        }

        /// <summary>Tormenta eléctrica: aviso (columna que parpadea) y rayo. El script los mueve a donde cae.</summary>
        private GameObject BuildStorm()
        {
            var root = new GameObject("Tormenta_Electrica");
            root.AddComponent<SortingGroup>().sortingOrder = 30;
            var storm = root.AddComponent<LightningStorm>();

            var warning = SetupUtils.SpriteChild(root.transform, "Aviso", PlaceholderArt.Square, new Color(0.85f, 0.6f, 1f, 0.3f),
                Vector3.zero, new Vector2(2f, 14f), 0);
            var bolt = SetupUtils.SpriteChild(root.transform, "Rayo", PlaceholderArt.Square, new Color(0.95f, 0.85f, 1f, 0.95f),
                Vector3.zero, new Vector2(2f, 14f), 2);
            var glow = SetupUtils.SpriteChild(root.transform, "Brillo", PlaceholderArt.Circle, new Color(0.8f, 0.5f, 1f, 0.6f),
                Vector3.zero, new Vector2(3f, 0.8f), 1);

            SetupUtils.SetRef(storm, "warningRenderer", warning);
            SetupUtils.SetRef(storm, "boltRenderer", bolt);
            SetupUtils.SetRef(storm, "groundGlow", glow);
            return root;
        }

        // ---------- Jefe rabia (Hipocampo) ----------

        private GameObject BuildRabiesBoss()
        {
            RabiesSegment virion = LoadOrBuild<RabiesSegment>($"{Folder}/Bosses/Rabia_Virion.prefab", BuildRabiesVirion);

            // La raíz no se ve ni recibe daño: solo dirige la cadena y lleva la barra del jefe.
            var root = new GameObject("Rabia_Hipocampo");
            root.layer = PhysicsLayers.Enemy;
            var health = root.AddComponent<EnemyHealth>();
            SetupUtils.SetRef(health, "data", data.RabiaJefe);
            SetupUtils.SetRef(health, "effectivenessTable", data.Table);

            var boss = root.AddComponent<RabiesBoss>();
            SetupUtils.SetRef(boss, "segmentPrefab", virion);
            SetupUtils.SetRef(boss, "segmentData", data.RabiaVirion);
            SetupUtils.SetRef(boss, "projectilePrefab", EnemyProjectile);
            SetupUtils.SetRef(boss, "stormPrefab", Storm);
            return root;
        }

        /// <summary>Virión de rabia (forma de bala con espículas). La cabeza muestra ojos y punta brillante.</summary>
        private GameObject BuildRabiesVirion()
        {
            EnemyData d = data.RabiaVirion;
            var root = new GameObject("Rabia_Virion");
            root.layer = PhysicsLayers.Enemy;
            root.AddComponent<SortingGroup>().sortingOrder = 6;

            GameObject visual = SetupUtils.Child(root.transform, "Visual", Vector3.zero);
            Transform v = visual.transform;
            var body = new System.Collections.Generic.List<Object>();
            body.Add(SetupUtils.SpriteChild(v, "Cuerpo", PlaceholderArt.Capsule, d.tint, Vector3.zero, new Vector2(1.05f, 0.62f), 0));
            body.Add(SetupUtils.SpriteChild(v, "Punta", PlaceholderArt.Circle, Color.Lerp(d.tint, Color.white, 0.15f),
                new Vector3(0.32f, 0f, 0f), new Vector2(0.5f, 0.62f), 1));
            body.Add(SetupUtils.SpriteChild(v, "Base", PlaceholderArt.SquareCentered, Color.Lerp(d.tint, Color.black, 0.25f),
                new Vector3(-0.47f, 0f, 0f), new Vector2(0.12f, 0.58f), 1));
            for (int i = 0; i < 4; i++)
            {
                float x = -0.35f + i * 0.22f;
                var up = SetupUtils.SpriteChild(v, "EspiculaArriba" + i, PlaceholderArt.Capsule, Color.Lerp(d.tint, Color.black, 0.3f),
                    new Vector3(x, 0.38f, 0f), new Vector2(0.2f, 0.08f), -1);
                up.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                var down = SetupUtils.SpriteChild(v, "EspiculaAbajo" + i, PlaceholderArt.Capsule, Color.Lerp(d.tint, Color.black, 0.3f),
                    new Vector3(x, -0.38f, 0f), new Vector2(0.2f, 0.08f), -1);
                down.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                body.Add(up);
                body.Add(down);
            }
            body.Add(SetupUtils.SpriteChild(v, "Linea", PlaceholderArt.Capsule, new Color(0.9f, 0.2f, 0.45f), new Vector3(-0.1f, 0f, 0f), new Vector2(0.7f, 0.07f), 2));

            // Partes de cabeza: ojo y halo rojo.
            GameObject headGroup = SetupUtils.Child(v, "Cabeza", Vector3.zero);
            SetupUtils.SpriteChild(headGroup.transform, "Halo", PlaceholderArt.Circle, new Color(1f, 0.2f, 0.45f, 0.35f), new Vector3(0.2f, 0f, 0f), new Vector2(1.3f, 1f), -2);
            SetupUtils.SpriteChild(headGroup.transform, "Ojo", PlaceholderArt.Circle, new Color(1f, 0.25f, 0.5f), new Vector3(0.38f, 0.1f, 0f), new Vector2(0.2f, 0.16f), 3);

            var health = root.AddComponent<EnemyHealth>();
            SetupUtils.SetRef(health, "data", d);
            SetupUtils.SetRef(health, "effectivenessTable", data.Table);
            SetupUtils.SetRefArray(health, "flashRenderers", body.ToArray());

            var hurtbox = root.AddComponent<Hurtbox>();
            SetHurtbox(hurtbox, Vector2.zero, new Vector2(1f, 0.75f));

            var seg = root.AddComponent<RabiesSegment>();
            SetupUtils.SetRef(seg, "visual", v);
            SetupUtils.SetRefArray(seg, "headParts", new Object[] { headGroup });
            return root;
        }

        // ---------- Jefe colmena (Pulmones) ----------

        private GameObject BuildHive()
        {
            EnemyData d = data.Colmena;
            var root = new GameObject("Colmena_Pulmones");
            root.layer = PhysicsLayers.Enemy;
            root.AddComponent<SortingGroup>().sortingOrder = 6;

            GameObject visual = SetupUtils.Child(root.transform, "Visual", Vector3.zero);
            Transform v = visual.transform;
            var body = new System.Collections.Generic.List<Object>();

            // Membranas laterales que "aletean" (lectura de nave).
            body.Add(SetupUtils.SpriteChild(v, "WingL", PlaceholderArt.Circle, new Color(0.75f, 0.55f, 0.85f, 0.7f),
                new Vector3(-1.9f, 0.3f, 0f), new Vector2(1.8f, 0.8f), -2));
            body.Add(SetupUtils.SpriteChild(v, "WingR", PlaceholderArt.Circle, new Color(0.75f, 0.55f, 0.85f, 0.7f),
                new Vector3(1.9f, 0.3f, 0f), new Vector2(1.8f, 0.8f), -2));

            // Cuerpo: granuloma con forma de colmena.
            body.Add(SetupUtils.SpriteChild(v, "Body", PlaceholderArt.Circle, d.tint, Vector3.zero, new Vector2(3.6f, 2.4f), 0));
            body.Add(SetupUtils.SpriteChild(v, "Crown", PlaceholderArt.Circle, Color.Lerp(d.tint, Color.white, 0.2f),
                new Vector3(0f, 0.9f, 0f), new Vector2(2.4f, 1.1f), 1));
            body.Add(SetupUtils.SpriteChild(v, "Belly", PlaceholderArt.Circle, Color.Lerp(d.tint, Color.black, 0.2f),
                new Vector3(0f, -0.8f, 0f), new Vector2(2.6f, 0.9f), 1));

            // Celdas de la colmena: brillan antes de atacar.
            var cells = new System.Collections.Generic.List<Object>();
            Vector2[] cellPos =
            {
                new Vector2(-1.1f, 0.15f), new Vector2(-0.35f, 0.35f), new Vector2(0.4f, 0.3f), new Vector2(1.15f, 0.1f),
                new Vector2(-0.75f, -0.4f), new Vector2(0f, -0.35f), new Vector2(0.75f, -0.45f)
            };
            for (int i = 0; i < cellPos.Length; i++)
                cells.Add(SetupUtils.SpriteChild(v, "Cell" + i, PlaceholderArt.Circle, new Color(0.25f, 0.1f, 0.15f),
                    new Vector3(cellPos[i].x, cellPos[i].y, 0f), new Vector2(0.45f, 0.42f), 2));

            var health = root.AddComponent<EnemyHealth>();
            SetupUtils.SetRef(health, "data", d);
            SetupUtils.SetRef(health, "effectivenessTable", data.Table);
            SetupUtils.SetRefArray(health, "flashRenderers", body.ToArray());

            var hurtbox = root.AddComponent<Hurtbox>();
            SetHurtbox(hurtbox, Vector2.zero, new Vector2(3.4f, 2.4f));

            var boss = root.AddComponent<HiveBoss>();
            SetupUtils.SetRef(boss, "visual", v);
            SetupUtils.SetRefArray(boss, "cellRenderers", cells.ToArray());
            SetupUtils.SetRef(boss, "projectilePrefab", EnemyProjectile);
            SetupUtils.SetRef(boss, "minionPrefab", Neumococo);
            SetupUtils.SetRef(boss, "minionData", data.Neumococo);
            return root;
        }

        // ---------- Bloqueos tratables (Pulmones, GDD 5.12) ----------

        private GameObject BuildObstacle(string name, EnemyData obstacleData, bool mucus)
        {
            const float width = 1.4f;
            const float height = 12f; // más alto que cualquier salto: hay que disolverlo
            var root = new GameObject(name);
            root.layer = PhysicsLayers.Ground;
            root.AddComponent<SortingGroup>().sortingOrder = 3;

            var wall = root.AddComponent<BoxCollider2D>();
            wall.size = new Vector2(width, height);
            wall.offset = new Vector2(0f, height * 0.5f);

            // El dibujo tiene el pivote en el piso para que se encoja hacia abajo.
            GameObject visual = SetupUtils.Child(root.transform, "Visual", Vector3.zero);
            Color c = obstacleData.tint;
            SpriteRenderer main = SetupUtils.SpriteChild(visual.transform, "Mass", PlaceholderArt.Square, c,
                new Vector3(0f, 0f, 0f), new Vector2(width, height), 0);
            var renderers = new System.Collections.Generic.List<Object> { main };
            for (int i = 0; i < 9; i++)
            {
                // Bultos en la orilla para que se lea orgánico (gotas de moco o tejido hinchado).
                float y = 0.5f + i * 1.25f;
                float side = i % 2 == 0 ? -1f : 1f;
                float size = mucus ? 0.9f + (i % 3) * 0.25f : 1.2f + (i % 2) * 0.3f;
                SpriteRenderer bump = SetupUtils.SpriteChild(visual.transform, "Bump" + i, PlaceholderArt.Circle,
                    Color.Lerp(c, mucus ? Color.white : new Color(0.7f, 0.1f, 0.15f), mucus ? 0.25f : 0.2f),
                    new Vector3(side * width * 0.45f, y, 0f), new Vector2(size, size), 1);
                renderers.Add(bump);
            }
            if (!mucus)
            {
                // Vetas de la inflamación.
                for (int i = 0; i < 4; i++)
                    SetupUtils.SpriteChild(visual.transform, "Vein" + i, PlaceholderArt.Capsule, new Color(0.7f, 0.1f, 0.2f, 0.8f),
                        new Vector3(0f, 1.2f + i * 2.4f, 0f), new Vector2(1f, 0.18f), 2).transform.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 25f : -25f);
            }

            var health = root.AddComponent<EnemyHealth>();
            SetupUtils.SetRef(health, "data", obstacleData);
            SetupUtils.SetRef(health, "effectivenessTable", data.Table);
            SetupUtils.SetRefArray(health, "flashRenderers", renderers.ToArray());

            // Hurtbox un poco más ancha que la pared para que la cápsula la toque antes de chocar.
            var hurtbox = root.AddComponent<Hurtbox>();
            SetHurtbox(hurtbox, new Vector2(0f, height * 0.5f), new Vector2(width + 0.9f, height));

            var obstacle = root.AddComponent<TreatableObstacle>();
            SetupUtils.SetRef(obstacle, "visual", visual.transform);
            SetupUtils.SetBool(obstacle, "slowsOnTouch", mucus);
            SetupUtils.SetBool(obstacle, "shrinkWidth", !mucus);
            SetupUtils.SetFloat(obstacle, "minScale", mucus ? 0.45f : 0.55f);
            return root;
        }

        // ---------- Nivel ----------

        private GameObject BuildSecret()
        {
            var root = new GameObject("SecretPassage");
            var secret = root.AddComponent<SecretPassage>();
            // Grieta brillante en la pared: tócala para descubrir el pasaje.
            SpriteRenderer glow = SetupUtils.SpriteChild(root.transform, "Glow", PlaceholderArt.Circle, new Color(1f, 0.9f, 0.4f, 0.75f),
                Vector3.zero, new Vector2(0.6f, 1.3f), -5);
            SetupUtils.SetRef(secret, "glow", glow);
            return root;
        }

        // ---------- Peligros ----------

        private GameObject BuildAcid()
        {
            var root = new GameObject("AcidPool");
            var acid = root.AddComponent<AcidPool>();
            SetupUtils.SetVector2(acid, "size", new Vector2(2.8f, 0.5f));

            // Charco sobre el piso (el objeto se coloca a la altura del piso).
            SetupUtils.SpriteChild(root.transform, "Glow", PlaceholderArt.Circle, new Color(0.5f, 1f, 0.15f, 0.25f),
                new Vector3(0f, 0.25f, 0f), new Vector2(3.8f, 1.2f), 1);
            SetupUtils.SpriteChild(root.transform, "Puddle", PlaceholderArt.Circle, new Color(0.5f, 1f, 0.15f, 0.95f),
                Vector3.zero, new Vector2(3.2f, 0.35f), 2);
            SetupUtils.SpriteChild(root.transform, "Bubble1", PlaceholderArt.Circle, new Color(0.85f, 1f, 0.6f, 0.9f),
                new Vector3(-0.6f, 0.12f, 0f), new Vector2(0.25f, 0.25f), 3);
            SetupUtils.SpriteChild(root.transform, "Bubble2", PlaceholderArt.Circle, new Color(0.85f, 1f, 0.6f, 0.9f),
                new Vector3(0.7f, 0.08f, 0f), new Vector2(0.18f, 0.18f), 3);
            return root;
        }
    }
}
