using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Progression;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace HealingKnight.EditorTools
{
    /// <summary>Crea los ScriptableObjects base con los valores del GDD. Si ya existen, no los toca.</summary>
    internal class DataSetup
    {
        public const string DataFolder = "Assets/_Project/Data";

        public MedicationData Antibiotico, Antiviral, Antiinflamatorio, Antihistaminico, Antiparasitario, Mucolitico, Anticonvulsivo, Anticoagulante;
        public EffectivenessTable Table;
        public PlayerConfig PlayerConfig;
        public EnemyData HPyloriMadre, HPyloriUreasa, Anisakis, Bezoar;
        public EnemyData Tenia, TeniaSegmento, Proglotido, LombrizHuye, Flora;
        public EnemyData HPylori, Giardia, Rotavirus, Entamoeba, CDifficile, Oxiuro, SMutans, Lombriz;
        public EnemyData Rinovirus, Polen, SAureus, Pertussis, Neumococo, Influenza, Tuberculosis;
        public EnemyData Mucosidad, Inflamacion, Colmena, TosFerinaReina;
        public EnemyData SAureusCoco, OxiuroHuevo;
        public EnemyData Poliovirus, Meningococo, Herpes, Rabia, NeuronaHiperexcitada, RabiaJefe, RabiaVirion;
        public GameCatalog Catalog;
        public LevelData DigestivoEstomago, DigestivoDelgado, DigestivoGrueso, DigestivoBoca;
        public LevelData RespiratorioNariz, RespiratorioTraquea, RespiratorioPulmones;
        public LevelData NerviosoMedula, NerviosoCerebro, NerviosoHipocampo;
        public LevelData NerviosoOjo, RespiratorioAmigdalas;
        public EnemyData SarroJefe, SPyogenes, AbscesoJefe, ToxoplasmaJefe;
        public EnemyData Plaquetas, Ldl, CelulaEspumosa, PlacaColesterol, AteromaJefe;
        public EnemyData Esporozoito, Merozoito, PlasmodiumJefe;
        public EnemyData LinfocitoInfectado, EpsteinBarrJefe;
        public EnemyData SViridans, Vegetacion, TCruziJefe;
        public EnemyData Embolo, GranTromboJefe;
        public EnemyData EColi, CalculoJefe, Leptospira, Cercaria, Proteus, ProteusColonia;
        public LevelData CirculatorioBazo, CirculatorioArteriasVenas, CirculatorioCorazon, CirculatorioGranTrombo, CirculatorioRinon;

        public void Run()
        {
            CreateMedications();
            CreateAudio();
            CreateEffectivenessTable();
            CreatePlayerConfig();
            CreateEnemies();
            CreateRespiratoryEnemies();
            CreateNervousEnemies();
            CreateCirculatoryEnemies();
            CreateMalariaEnemies();
            CreateSpleenEnemies();
            CreateHeartEnemies();
            CreateThrombusEnemies();
            CreateKidneyEnemies();
            CreateOffspring();
            CreateCatalog();
            CreateBestiary();
        }

        private MedicationData Med(string file, string name, Color color, bool regional, string description)
        {
            return SetupUtils.LoadOrCreate<MedicationData>($"{DataFolder}/Medications/{file}.asset", m =>
            {
                m.displayName = name;
                m.color = color;
                m.isRegional = regional;
                m.description = description;
            });
        }

        private void CreateMedications()
        {
            Antibiotico = Med("Antibiotico", "Antibiótico", new Color(0.25f, 0.55f, 1f), false, "Efectivo contra bacterias.");
            Antiviral = Med("Antiviral", "Antiviral", new Color(0.85f, 0.95f, 1f), false, "Efectivo contra virus.");
            Antiinflamatorio = Med("Antiinflamatorio", "Antiinflamatorio", new Color(1f, 0.5f, 0.75f), false, "Reduce la inflamación. Parcial contra bacterias y virus.");
            Antihistaminico = Med("Antihistaminico", "Antihistamínico", new Color(0.7f, 0.5f, 1f), false, "Efectivo contra alérgenos (polen). Parcial contra parásitos y mucosidad.");
            Antiparasitario = Med("Antiparasitario", "Antiparasitario", new Color(1f, 0.72f, 0.1f), true, "Cápsula regional del Sistema Digestivo. Efectivo contra parásitos.");
            Mucolitico = Med("Mucolitico", "Mucolítico", new Color(0.55f, 1f, 0.6f), true, "Cápsula regional del Sistema Respiratorio. Disuelve la mucosidad.");
            Anticonvulsivo = Med("Anticonvulsivo", "Anticonvulsivo", new Color(1f, 0.85f, 0.3f), true,
                "Cápsula regional del Sistema Nervioso (como la fenitoína o el levetiracetam): calma las neuronas hiperexcitadas. " +
                "Su bomba deja un campo neuroprotector. Parcial contra bacterias y virus.");
            if (!Anticonvulsivo.calmsNeurons)
            {
                Anticonvulsivo.calmsNeurons = true;
                UnityEditor.EditorUtility.SetDirty(Anticonvulsivo);
            }
            // Turquesa: se ve bien contra la sangre roja del sistema circulatorio.
            Anticoagulante = Med("Anticoagulante", "Anticoagulante", new Color(0.25f, 0.95f, 0.95f), true,
                "Cápsula regional del Sistema Circulatorio (como la heparina): deshace los coágulos de plaquetas y fibrina. " +
                "Parcial contra bacterias y virus.");
        }

        public const string SfxFolder = "Assets/_Project/Audio/Resources/SFX";

        /// <summary>
        /// Sonidos de prueba (sintetizados, de ciencia ficción): uno de disparo y uno de impacto por medicamento,
        /// más el rebote general. Solo se asignan si el medicamento aún no tiene sonido (no pisa los tuyos).
        /// </summary>
        private void CreateAudio()
        {
            AssignSounds(Antibiotico, "Antibiotico");
            AssignSounds(Antiviral, "Antiviral");
            AssignSounds(Antiinflamatorio, "Antiinflamatorio");
            AssignSounds(Antihistaminico, "Antihistaminico");
            AssignSounds(Antiparasitario, "Antiparasitario");
            AssignSounds(Mucolitico, "Mucolitico");
            AssignSounds(Anticonvulsivo, "Anticonvulsivo");
            AssignSounds(Anticoagulante, "Anticoagulante");

            var audio = SetupUtils.LoadOrCreate<HealingKnight.Gameplay.Audio.CombatAudio>(
                $"{DataFolder}/Resources/{HealingKnight.Gameplay.Audio.CombatAudio.ResourcePath}.asset", a => { });
            if (audio.resistSound == null)
            {
                audio.resistSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxFolder}/Rebote_Ineficaz.ogg");
                UnityEditor.EditorUtility.SetDirty(audio);
            }
        }

        private static void AssignSounds(MedicationData med, string name)
        {
            if (med == null) return;
            bool changed = false;
            if (med.fireSound == null)
            {
                med.fireSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxFolder}/Disparo_{name}.ogg");
                changed |= med.fireSound != null;
            }
            if (med.hitSound == null)
            {
                med.hitSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxFolder}/Impacto_{name}.ogg");
                changed |= med.hitSound != null;
            }
            if (changed) UnityEditor.EditorUtility.SetDirty(med);
        }

        private void CreateEffectivenessTable()
        {
            Table = SetupUtils.LoadOrCreate<EffectivenessTable>($"{DataFolder}/Combat/EffectivenessTable.asset", t =>
            {
                // GDD 5.7 (la columna "Regional" se reparte entre Antiparasitario y Mucolítico).
                t.Set(ThreatType.Bacteria, Antibiotico, Effectiveness.Effective);
                t.Set(ThreatType.Bacteria, Antiinflamatorio, Effectiveness.Partial);
                t.Set(ThreatType.Bacteria, Antiparasitario, Effectiveness.Partial);
                t.Set(ThreatType.Bacteria, Mucolitico, Effectiveness.Partial);

                t.Set(ThreatType.Virus, Antiviral, Effectiveness.Effective);
                t.Set(ThreatType.Virus, Antiinflamatorio, Effectiveness.Partial);
                t.Set(ThreatType.Virus, Antiparasitario, Effectiveness.Partial);
                t.Set(ThreatType.Virus, Mucolitico, Effectiveness.Partial);

                t.Set(ThreatType.Parasito, Antiparasitario, Effectiveness.Effective);
                t.Set(ThreatType.Parasito, Antibiotico, Effectiveness.Partial);
                t.Set(ThreatType.Parasito, Antihistaminico, Effectiveness.Partial);

                t.Set(ThreatType.Inflamacion, Antiinflamatorio, Effectiveness.Effective);

                t.Set(ThreatType.Mucosidad, Mucolitico, Effectiveness.Effective);
                t.Set(ThreatType.Mucosidad, Antihistaminico, Effectiveness.Partial);

                t.Set(ThreatType.Alergeno, Antihistaminico, Effectiveness.Effective);
                t.Set(ThreatType.Alergeno, Antiinflamatorio, Effectiveness.Partial);
            });

            // Anticonvulsivo (regional del nervioso). Columna "Regional" del GDD: parcial contra bacterias y virus.
            if (Table.Evaluate(ThreatType.Hiperexcitacion, Anticonvulsivo) != Effectiveness.Effective)
            {
                Table.Set(ThreatType.Hiperexcitacion, Anticonvulsivo, Effectiveness.Effective);
                Table.Set(ThreatType.Bacteria, Anticonvulsivo, Effectiveness.Partial);
                Table.Set(ThreatType.Virus, Anticonvulsivo, Effectiveness.Partial);
                UnityEditor.EditorUtility.SetDirty(Table);
            }

            // Anticoagulante (regional del circulatorio): deshace coágulos; como toda cápsula regional, parcial contra bacterias y virus.
            if (Table.Evaluate(ThreatType.Coagulo, Anticoagulante) != Effectiveness.Effective)
            {
                Table.Set(ThreatType.Coagulo, Anticoagulante, Effectiveness.Effective);
                Table.Set(ThreatType.Bacteria, Anticoagulante, Effectiveness.Partial);
                Table.Set(ThreatType.Virus, Anticoagulante, Effectiveness.Partial);
                UnityEditor.EditorUtility.SetDirty(Table);
            }

            // Tablas creadas antes de que existieran los alérgenos: se completan sin tocar lo demás.
            if (Table.Evaluate(ThreatType.Alergeno, Antihistaminico) != Effectiveness.Effective)
            {
                Table.Set(ThreatType.Alergeno, Antihistaminico, Effectiveness.Effective);
                Table.Set(ThreatType.Alergeno, Antiinflamatorio, Effectiveness.Partial);
                UnityEditor.EditorUtility.SetDirty(Table);
            }
        }

        private void CreatePlayerConfig()
        {
            PlayerConfig = SetupUtils.LoadOrCreate<PlayerConfig>($"{DataFolder}/Player/PlayerConfig.asset", c =>
            {
                // Altura del hombro del caballero (Art/Player): 1.15. Al encogerse se multiplica por el tamaño (55 %).
                c.muzzleHeight = 1.15f;
                c.crouchMuzzleHeight = 0.63f;
                c.muzzleForward = 0.69f; // largo del brazo-cañón: la cápsula sale de la punta
                c.genericMedications = new List<MedicationData> { Antibiotico, Antiviral, Antiinflamatorio, Antihistaminico };
            });
        }

        private EnemyData Enemy(string file, System.Action<EnemyData> init)
        {
            return SetupUtils.LoadOrCreate<EnemyData>($"{DataFolder}/Enemies/{file}.asset", init);
        }

        private void CreateEnemies()
        {
            // Todos son patógenos reales del sistema digestivo (con licencia de "mutados" para el juego).
            HPylori = Enemy("Bacteria_HPylori", e =>
            {
                e.displayName = "H. pylori";
                e.threat = ThreatType.Bacteria;
                e.behavior = EnemyBehavior.Burrower;
                e.biologia = "Bacteria en espiral con flagelos: se entierra en la capa de moco del estómago para que el ácido no la mate. " +
                    "En el juego se mete bajo la mucosa (no la puedes dañar) y sale debajo de ti.";
                e.maxHealth = 22f; e.moveSpeed = 3.4f;
                e.attackDistance = 1f; e.attackWindup = 0.35f; e.attackDamage = 10f;
                e.specialCooldown = 4.5f;
                e.tint = new Color(0.95f, 0.76f, 0.31f); // amarillo mostaza: resalta sobre el ácido verde
            });

            HPyloriMadre = Enemy("Bacteria_HPylori_Madre", e =>
            {
                e.displayName = "H. pylori (colonia madre)";
                e.threat = ThreatType.Bacteria;
                e.behavior = EnemyBehavior.Walker;
                e.biologia = "Las bacterias viven en colonias pegadas a la mucosa. Esta es grande y lenta: suelta H. pylori " +
                    "nuevas cada pocos segundos hasta que la destruyes. Ataca primero a la madre.";
                e.maxHealth = 70f; e.moveSpeed = 1.4f;
                e.attackDistance = 1.5f; e.attackWindup = 0.6f; e.attackCooldown = 2.4f; e.attackDamage = 14f; e.attackKnockback = 6f;
                e.knockbackResistance = 0.85f;
                e.tint = new Color(0.95f, 0.72f, 0.28f);
                e.visualScale = 1.9f;
            });

            HPyloriUreasa = Enemy("Bacteria_HPylori_Ureasa", e =>
            {
                e.displayName = "H. pylori (ureasa)";
                e.threat = ThreatType.Bacteria;
                e.behavior = EnemyBehavior.Walker;
                e.biologia = "La H. pylori fabrica ureasa: convierte la urea en amoniaco, que neutraliza el ácido a su alrededor. " +
                    "En el juego, dentro de su burbuja el ácido no te quema: pelea cerca de ella... y luego acábala.";
                e.maxHealth = 26f; e.moveSpeed = 2.2f;
                e.attackDistance = 1f; e.attackWindup = 0.4f; e.attackDamage = 9f;
                e.tint = new Color(0.25f, 0.78f, 0.69f);
                e.visualScale = 1.05f;
            });

            Anisakis = Enemy("Parasito_Anisakis", e =>
            {
                e.displayName = "Anisakis";
                e.threat = ThreatType.Parasito;
                e.behavior = EnemyBehavior.Latcher;
                e.biologia = "Gusano parásito que llega escondido en pescado crudo mal preparado (sushi, ceviche) y se clava en la " +
                    "pared del estómago. En el juego sale del sushi cuando te paras encima y se te pega.";
                e.maxHealth = 24f; e.moveSpeed = 4.2f;
                e.attackDistance = 2.6f; e.attackWindup = 0.35f; e.attackCooldown = 2f; e.attackDamage = 8f;
                e.tint = new Color(0.96f, 0.84f, 0.84f);
                e.visualScale = 1.1f;
            });

            Bezoar = Enemy("Jefe_Bezoar", e =>
            {
                e.displayName = "Bezoar";
                e.threat = ThreatType.Bacteria;
                e.biologia = "Bola de comida (o pelo, o chicle) que no se digiere y se queda atorada; puede tapar la salida del " +
                    "estómago. Este está lleno de H. pylori: el antibiótico lo deshace.";
                e.maxHealth = 320f;
                e.meleeDamageTakenMultiplier = 0.6f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 6;
                e.tint = new Color(0.78f, 0.55f, 0.32f);
            });

            // Intestino delgado (rediseño): tenia (minijefe), sus pedazos y la lombriz que huye.
            Tenia = Enemy("Jefe_Tenia", e =>
            {
                e.displayName = "Tenia (solitaria)";
                e.threat = ThreatType.Parasito;
                e.biologia = "Gusano plano que puede medir varios metros. Se agarra a la pared del intestino con la cabeza (escólex), " +
                    "que tiene ganchos y ventosas. Se contagia al comer carne de cerdo o res mal cocida. Se trata con antiparasitario.";
                e.maxHealth = 260f;
                e.meleeDamageTakenMultiplier = 0.7f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 5;
                e.tint = new Color(0.98f, 0.93f, 0.8f);
            });
            TeniaSegmento = Enemy("Parasito_Tenia_Segmento", e =>
            {
                e.displayName = "Proglótido (cuerpo de la tenia)";
                e.threat = ThreatType.Parasito;
                e.biologia = "El cuerpo de la tenia está hecho de pedacitos llamados proglótidos. Cada uno puede vivir solo.";
                e.maxHealth = 22f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f;
                e.tint = new Color(0.97f, 0.9f, 0.76f);
            });
            Proglotido = Enemy("Parasito_Proglotido", e =>
            {
                e.displayName = "Proglótido";
                e.threat = ThreatType.Parasito;
                e.behavior = EnemyBehavior.Walker;
                e.biologia = "Pedazo del final de la tenia, lleno de huevos. Se suelta y sale con las heces: así se contagia.";
                e.maxHealth = 16f; e.moveSpeed = 1.7f;
                e.attackDistance = 0.9f; e.attackWindup = 0.45f; e.attackDamage = 6f;
                e.tint = new Color(0.97f, 0.9f, 0.76f);
                e.visualScale = 0.9f;
            });
            LombrizHuye = Enemy("Jefe_Lombriz_Huye", e =>
            {
                e.displayName = "Lombriz";
                e.threat = ThreatType.Parasito;
                e.biologia = "Ascaris lumbricoides: lombriz redonda que puede medir 30 cm. Vive en el intestino delgado y se mueve " +
                    "por él. Huevos en tierra o comida sucia: por eso hay que lavarse las manos y lavar la verdura.";
                e.maxHealth = 160f;
                e.meleeDamageTakenMultiplier = 0.6f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 4;
                e.tint = new Color(0.95f, 0.62f, 0.6f);
            });

            // Intestino grueso (rediseño): flora intestinal buena (no ataca; si la matas con antibiótico sale C. difficile).
            Flora = Enemy("Bacteria_Flora", e =>
            {
                e.displayName = "Flora intestinal (bacteria buena)";
                e.threat = ThreatType.Bacteria;
                e.biologia = "Lactobacillus y Bifidobacterium: bacterias buenas del colon que ayudan a digerir y no dejan espacio a las malas. " +
                    "Los antibióticos también las matan, y entonces C. difficile puede crecer. NO les dispares antibiótico.";
                e.maxHealth = 12f;
                e.meleeDamageTakenMultiplier = 0f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 0;
                e.tint = new Color(0.45f, 0.85f, 0.5f);
            });

            Giardia = Enemy("Parasito_Giardia", e =>
            {
                e.displayName = "Giardia";
                e.threat = ThreatType.Parasito;
                e.behavior = EnemyBehavior.Latcher;
                e.biologia = "Protozoo con forma de pera y un disco ventral con el que se pega a la pared del intestino. " +
                    "Se trata con metronidazol, que es antiparasitario Y antibiótico: por eso el antibiótico también le sirve.";
                e.weaknesses.Add(new EnemyWeakness { medication = Antibiotico, result = Effectiveness.Effective });
                e.maxHealth = 22f; e.moveSpeed = 3.9f;
                e.attackDistance = 2.8f; e.attackWindup = 0.35f; e.attackCooldown = 2f; e.attackDamage = 8f;
                e.tint = new Color(0.95f, 0.85f, 0.55f);
            });

            Rotavirus = Enemy("Virus_Rotavirus", e =>
            {
                e.displayName = "Rotavirus";
                e.threat = ThreatType.Virus;
                e.behavior = EnemyBehavior.Roller;
                e.biologia = "Su nombre viene de 'rota' (rueda en latín): al microscopio parece una rueda con rayos. " +
                    "En el juego es una rueda voladora: gira a tu alrededor, acelera el giro, se echa para atrás y se lanza " +
                    "como sierra; rebota en el piso, las paredes y tu escudo.";
                e.movement = EnemyMovement.Flying; e.flyHeight = 2.2f;
                e.maxHealth = 22f; e.moveSpeed = 4.2f;
                e.attackDistance = 4f; e.attackWindup = 0.55f; e.attackCooldown = 2.2f;
                e.attackDamage = 10f; e.attackKnockback = 5f;
                e.tint = new Color(0.75f, 0.45f, 0.95f);
            });

            Entamoeba = Enemy("Parasito_Entamoeba", e =>
            {
                e.displayName = "Entamoeba (ameba)";
                e.threat = ThreatType.Parasito;
                e.behavior = EnemyBehavior.Engulfer;
                e.biologia = "Ameba: estira seudópodos y engulle lo que encuentra (fagocitosis), hasta glóbulos rojos. " +
                    "Su seudópodo llega lejos y te jala hacia ella.";
                e.maxHealth = 50f; e.moveSpeed = 1.8f;
                e.attackDistance = 2.4f; e.attackWindup = 0.6f; e.attackCooldown = 1.8f; e.attackDamage = 14f;
                e.knockbackResistance = 0.6f; e.linfocitoCharge = 2; e.jumpHeight = 2.2f;
                e.tint = new Color(0.8f, 0.6f, 0.75f);
                e.visualScale = 1.4f;
            });

            CDifficile = Enemy("Bacteria_CDifficile", e =>
            {
                e.displayName = "C. difficile";
                e.threat = ThreatType.Bacteria;
                e.behavior = EnemyBehavior.Sporer;
                e.biologia = "Forma esporas que aguantan antibióticos, calor y desinfectantes; aparece cuando los antibióticos " +
                    "acaban con la flora buena. Herida, se vuelve espora (todo rebota) y luego despierta. Solo su antibiótico la daña.";
                e.weaknesses.Add(new EnemyWeakness { medication = Antiinflamatorio, result = Effectiveness.Ineffective });
                e.weaknesses.Add(new EnemyWeakness { medication = Antiparasitario, result = Effectiveness.Ineffective });
                e.maxHealth = 30f; e.moveSpeed = 3.1f;
                e.attackDistance = 1.1f; e.attackWindup = 0.4f; e.attackDamage = 12f;
                e.specialCooldown = 7f; e.specialDuration = 3f;
                e.tint = new Color(0.45f, 0.65f, 0.35f);
                e.visualScale = 1.1f;
            });

            Oxiuro = Enemy("Parasito_Oxiuro", e =>
            {
                e.displayName = "Oxiuro";
                e.threat = ThreatType.Parasito;
                e.behavior = EnemyBehavior.Breeder;
                e.biologia = "Lombriz pequeña y rápida. Pone huevos que vuelven a infectar a la misma persona (autoinfección): " +
                    "al morir deja un huevo; rómpelo antes de que eclosione.";
                e.maxHealth = 14f; e.moveSpeed = 4.7f;
                e.attackDistance = 0.9f; e.attackWindup = 0.3f; e.attackDamage = 6f;
                e.childCount = 1;
                e.tint = new Color(0.97f, 0.95f, 0.88f);
                e.visualScale = 0.8f;
            });

            SMutans = Enemy("Bacteria_SMutans", e =>
            {
                e.displayName = "S. mutans (caries)";
                e.threat = ThreatType.Bacteria;
                e.behavior = EnemyBehavior.Lobber;
                e.biologia = "Vive en la placa dental: fermenta el azúcar y suelta ácido láctico que disuelve el esmalte (caries). " +
                    "Se queda lejos y te lanza ácido en arco; te alcanza aunque estés en una plataforma.";
                e.maxHealth = 18f; e.moveSpeed = 2.9f;
                e.attackDistance = 4.5f; e.attackWindup = 0.5f; e.attackCooldown = 2.2f; e.attackDamage = 8f;
                e.projectileGravity = 14f; e.projectileSpeed = 6f;
                e.projectileColor = new Color(1f, 0.95f, 0.45f);
                e.tint = new Color(0.85f, 0.85f, 0.55f);
            });

            SarroJefe = Enemy("Jefe_Sarro", e =>
            {
                e.displayName = "El Sarro";
                e.threat = ThreatType.Bacteria;
                e.biologia = "Sarro (cálculo dental): placa bacteriana que se endureció como piedra. Solo se quita raspando: " +
                    "arráncale las placas con el hilo dental y luego pégale con antibiótico.";
                e.maxHealth = 300f;
                e.meleeDamageTakenMultiplier = 0.5f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 5;
                e.tint = new Color(0.88f, 0.8f, 0.5f);
            });

            SPyogenes = Enemy("Bacteria_SPyogenes", e =>
            {
                e.displayName = "S. pyogenes (faringitis)";
                e.threat = ThreatType.Bacteria;
                e.behavior = EnemyBehavior.Walker;
                e.biologia = "El estreptococo de la garganta (faringitis, 'anginas'). Forma cadenitas de cocos. " +
                    "Al morir suelta un ANTÍGENO: llévalo al centro germinal para fabricar anticuerpos.";
                e.maxHealth = 20f; e.moveSpeed = 3.2f; e.jumpHeight = 2.4f;
                e.attackDistance = 0.9f; e.attackWindup = 0.35f; e.attackDamage = 8f;
                e.tint = new Color(1f, 0.55f, 0.55f);
            });

            AbscesoJefe = Enemy("Jefe_Absceso", e =>
            {
                e.displayName = "El Absceso";
                e.threat = ThreatType.Bacteria;
                e.biologia = "Absceso periamigdalino: una bolsa cerrada de pus junto a la amígdala. El medicamento casi no entra: " +
                    "solo le hacen daño los anticuerpos de los linfocitos B.";
                e.maxHealth = 260f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 0;
                e.tint = new Color(1f, 0.75f, 0.7f);
            });

            ToxoplasmaJefe = Enemy("Jefe_Toxoplasma", e =>
            {
                e.displayName = "Toxoplasma";
                e.threat = ThreatType.Parasito;
                e.biologia = "Toxoplasma gondii: parásito con forma de media luna que se esconde en quistes de la retina y se reactiva. " +
                    "Sorpréndelo sin que te vean y usa antiparasitario.";
                e.maxHealth = 180f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 5;
                e.tint = new Color(0.65f, 0.9f, 0.55f);
            });

            Lombriz = Enemy("Jefe_Lombriz", e =>
            {
                e.displayName = "Lombriz";
                e.threat = ThreatType.Parasito;
                e.maxHealth = 320f;
                e.meleeDamageTakenMultiplier = 0.6f;
                e.knockbackResistance = 1f;
                e.hurtStunTime = 0f;
                e.linfocitoCharge = 5;
                e.tint = new Color(0.95f, 0.62f, 0.6f);
            });
        }

        private void CreateOffspring()
        {
            // Crías: lo que suelta el racimo de estafilococo y el huevo del oxiuro (las conecta PrefabSetup).
            SAureusCoco = Enemy("Bacteria_SAureus_Coco", e =>
            {
                e.displayName = "S. aureus (coco suelto)";
                e.threat = ThreatType.Bacteria;
                e.behavior = EnemyBehavior.Walker;
                e.biologia = "Un coco que se separó del racimo: pequeño, rápido y débil.";
                e.maxHealth = 8f; e.moveSpeed = 4.4f; e.jumpHeight = 2.6f;
                e.attackDistance = 0.8f; e.attackWindup = 0.3f; e.attackDamage = 6f;
                e.tint = new Color(1f, 0.85f, 0.4f);
                e.visualScale = 0.55f;
            });

            OxiuroHuevo = Enemy("Parasito_Oxiuro_Huevo", e =>
            {
                e.displayName = "Huevo de oxiuro";
                e.threat = ThreatType.Parasito;
                e.behavior = EnemyBehavior.Egg;
                e.biologia = "Los huevos del oxiuro maduran en horas y reinfectan. Si no lo rompes, eclosiona otro oxiuro.";
                e.maxHealth = 6f; e.moveSpeed = 0.0f; e.jumpHeight = 0f;
                e.specialDuration = 4.5f;
                e.meleeDamageTakenMultiplier = 2f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 0;
                e.tint = new Color(0.98f, 0.96f, 0.85f);
                e.visualScale = 0.5f;
            });
        }

        private void CreateRespiratoryEnemies()
        {
            // Sistema Respiratorio: patógenos reales de cada zona (GDD 5.11: bacterias en grupo y virus que flotan y disparan).
            Rinovirus = Enemy("Virus_Rinovirus", e =>
            {
                e.displayName = "Rinovirus (resfriado)";
                e.threat = ThreatType.Virus;
                e.behavior = EnemyBehavior.Spreader;
                e.biologia = "Causa el resfriado común. No existe un antiviral para él: se cura solo cuando tu sistema inmune lo elimina. " +
                    "Por eso los medicamentos le hacen poco y desaparece solo después de un rato. Estornuda en abanico.";
                e.weaknesses.Add(new EnemyWeakness { medication = Antiviral, result = Effectiveness.Partial });
                e.weaknesses.Add(new EnemyWeakness { medication = Antihistaminico, result = Effectiveness.Partial });
                e.movement = EnemyMovement.Flying; e.flyHeight = 2.4f;
                e.maxHealth = 12f; e.moveSpeed = 2.9f; e.lifetime = 14f;
                e.attackType = EnemyAttackType.Ranged;
                e.attackDistance = 5f; e.attackWindup = 0.45f; e.attackCooldown = 2.8f; e.attackDamage = 6f;
                e.spreadShots = 3; e.spreadDegrees = 30f; e.projectileSpeed = 5.5f;
                e.projectileColor = new Color(0.75f, 1f, 0.55f);
                e.tint = new Color(0.6f, 0.9f, 0.45f);
                e.visualScale = 0.8f;
            });

            Polen = Enemy("Alergeno_Polen", e =>
            {
                e.displayName = "Polen (alérgeno)";
                e.threat = ThreatType.Alergeno;
                e.behavior = EnemyBehavior.Drifter;
                e.biologia = "No es un microbio: es un alérgeno. Lo lleva el aire y tu cuerpo reacciona soltando histamina " +
                    "(estornudos, comezón). Flota en ondas y, si te alcanza, revienta y te deja lento un rato.";
                e.movement = EnemyMovement.Flying; e.flyHeight = 0.9f;
                e.maxHealth = 10f; e.moveSpeed = 1.8f;
                e.attackWindup = 0.45f; e.attackDamage = 6f; e.attackKnockback = 3f;
                e.slowOnHit = 0.55f; e.slowDuration = 2f;
                e.tint = new Color(1f, 0.85f, 0.25f);
                e.visualScale = 0.75f;
            });

            SAureus = Enemy("Bacteria_SAureus", e =>
            {
                e.displayName = "S. aureus";
                e.threat = ThreatType.Bacteria;
                e.behavior = EnemyBehavior.Splitter;
                e.biologia = "Cocos agrupados en racimos de uvas ('staphylé' = racimo en griego). Al romper el racimo, los cocos siguen solos.";
                e.maxHealth = 24f; e.moveSpeed = 3.6f;
                e.attackDistance = 1f; e.attackWindup = 0.35f; e.attackDamage = 10f;
                e.childCount = 2;
                e.tint = new Color(0.95f, 0.8f, 0.35f);
            });

            Pertussis = Enemy("Bacteria_Pertussis", e =>
            {
                e.displayName = "B. pertussis (tos ferina)";
                e.threat = ThreatType.Bacteria;
                e.behavior = EnemyBehavior.Cougher;
                e.biologia = "Su toxina paraliza los cilios de la tráquea y provoca ataques de tos en ráfaga. " +
                    "Toma aire (se infla) y tose tres veces: cada tos empuja lejos.";
                e.maxHealth = 26f; e.moveSpeed = 3.4f;
                e.attackDistance = 1.8f; e.attackWindup = 0.7f; e.attackCooldown = 2.4f; e.attackDamage = 12f; e.attackKnockback = 7f;
                e.tint = new Color(0.55f, 0.75f, 0.95f);
            });

            Neumococo = Enemy("Bacteria_Neumococo", e =>
            {
                e.displayName = "S. pneumoniae (neumonía)";
                e.threat = ThreatType.Bacteria;
                e.behavior = EnemyBehavior.Hopper;
                e.biologia = "Diplococo (dos cocos pegados) con cápsula. Llega en las gotitas de saliva y en grupo a los alvéolos: " +
                    "avanza a saltos y te cae encima.";
                e.maxHealth = 18f; e.moveSpeed = 4.2f; e.aggroRange = 16f; e.jumpHeight = 3.6f;
                e.attackDamage = 9f; e.attackKnockback = 4f;
                e.tint = new Color(0.45f, 0.85f, 0.75f);
                e.visualScale = 0.9f;
            });

            Influenza = Enemy("Virus_Influenza", e =>
            {
                e.displayName = "Influenza";
                e.threat = ThreatType.Virus;
                e.behavior = EnemyBehavior.Diver;
                e.biologia = "Cubierta de espículas (hemaglutinina y neuraminidasa) con las que se clava en las células. " +
                    "Flota alto y se lanza en picada. El antiviral sí funciona (oseltamivir).";
                e.movement = EnemyMovement.Flying; e.flyHeight = 3.5f;
                e.maxHealth = 24f; e.moveSpeed = 3.4f;
                e.attackWindup = 0.55f; e.attackCooldown = 2.6f; e.attackDamage = 12f; e.attackKnockback = 5f;
                e.tint = new Color(0.55f, 0.65f, 1f);
                e.visualScale = 1.1f;
            });

            Tuberculosis = Enemy("Bacteria_Tuberculosis", e =>
            {
                e.displayName = "M. tuberculosis (resistente)";
                e.threat = ThreatType.Bacteria;
                e.behavior = EnemyBehavior.Tank;
                e.biologia = "Pared gruesa y cerosa (ácido micólico): solo la daña su antibiótico. El cuerpo la encierra en granulomas " +
                    "(se vuelve dura un rato) y de lejos tose gotitas que contagian.";
                e.weaknesses.Add(new EnemyWeakness { medication = Antiinflamatorio, result = Effectiveness.Ineffective });
                e.weaknesses.Add(new EnemyWeakness { medication = Mucolitico, result = Effectiveness.Ineffective });
                e.weaknesses.Add(new EnemyWeakness { medication = Antiparasitario, result = Effectiveness.Ineffective });
                e.maxHealth = 80f; e.moveSpeed = 1.6f; e.jumpHeight = 2f;
                e.attackDistance = 1.4f; e.attackWindup = 0.6f; e.attackCooldown = 2.2f; e.attackDamage = 15f; e.attackKnockback = 6f;
                e.projectileGravity = 12f; e.spreadShots = 3; e.spreadDegrees = 20f;
                e.projectileColor = new Color(0.85f, 0.75f, 0.6f);
                e.rangedPoseIdle = true;   // de lejos TOSE (se infla y suelta la nube), no embiste
                e.specialCooldown = 8f; e.specialDuration = 2.5f;
                e.knockbackResistance = 0.75f; e.linfocitoCharge = 3;
                e.tint = new Color(0.75f, 0.55f, 0.45f);
                e.visualScale = 1.4f;
            });

            // Bloqueos (GDD 5.12): usan la misma vida y tabla de efectividad que los enemigos.
            Mucosidad = Enemy("Bloqueo_Mucosidad", e =>
            {
                e.displayName = "Mucosidad";
                e.threat = ThreatType.Mucosidad;
                e.maxHealth = 60f;
                e.meleeDamageTakenMultiplier = 0.25f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 0;
                e.tint = new Color(0.78f, 0.92f, 0.3f, 0.92f); // amarillo verdoso, como gotea en el arte de referencia
            });

            Inflamacion = Enemy("Bloqueo_Inflamacion", e =>
            {
                e.displayName = "Inflamación";
                e.threat = ThreatType.Inflamacion;
                e.maxHealth = 80f;
                e.meleeDamageTakenMultiplier = 0.25f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 0;
                e.tint = new Color(0.88f, 0.22f, 0.42f);
            });

            // Jefe de la Tráquea: la reina de la tos ferina (camina y tose ondas por el piso).
            TosFerinaReina = Enemy("Jefe_TosFerina", e =>
            {
                e.displayName = "Reina de la tos ferina";
                e.threat = ThreatType.Bacteria;
                e.biologia = "Bordetella pertussis paraliza los cilios con su toxina y provoca ataques de tos en ráfaga. " +
                    "Al toser manda ondas por el piso: sáltalas.";
                e.maxHealth = 320f;
                e.meleeDamageTakenMultiplier = 0.6f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 5;
                e.tint = new Color(0.55f, 0.75f, 0.95f);
            });

            // Jefe de los Pulmones: colmena voladora de tuberculosis (estilo nave de Metal Slug).
            Colmena = Enemy("Jefe_Colmena", e =>
            {
                e.displayName = "Nave de tuberculosis";
                e.threat = ThreatType.Bacteria;
                e.movement = EnemyMovement.Flying; e.flyHeight = 4.5f;
                e.maxHealth = 420f;
                e.meleeDamageTakenMultiplier = 0.5f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 6;
                e.tint = new Color(0.82f, 0.68f, 0.45f);
            });
        }

        /// <summary>Sistema circulatorio: por ahora el coágulo de plaquetas (obstáculo de los niveles de vuelo).</summary>
        private void CreateCirculatoryEnemies()
        {
            Plaquetas = Enemy("Bloqueo_Plaquetas", e =>
            {
                e.displayName = "Coágulo (plaquetas)";
                e.threat = ThreatType.Coagulo;
                e.biologia = "Las plaquetas tapan las heridas pegándose unas a otras y la fibrina las amarra como una red. " +
                    "Dentro de un vaso sano, un coágulo estorba el paso de la sangre (trombosis). Se deshace con anticoagulante.";
                e.maxHealth = 50f;
                e.meleeDamageTakenMultiplier = 0f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 0;
                e.attackDamage = 8f;
                e.tint = new Color(0.8f, 0.6f, 0.9f);
            });

            // Tramo 3 de Arterias y venas: las ARTERIAS ENFERMAS (aterosclerosis).
            Ldl = Enemy("Lipido_LDL", e =>
            {
                e.displayName = "Colesterol LDL";
                e.threat = ThreatType.Inflamacion;
                e.biologia = "El colesterol \"malo\": bolitas de grasa envueltas en proteína que viajan por la sangre. Si hay demasiadas " +
                    "se meten en la pared de las arterias y forman placas. Cualquier cápsula las revienta.";
                e.movement = EnemyMovement.Flying; e.flyHeight = 2f;
                e.maxHealth = 6f; e.moveSpeed = 3f;
                e.attackDamage = 7f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 0;
                e.projectileColor = new Color(1f, 0.85f, 0.3f);
                e.tint = new Color(1f, 0.82f, 0.28f);
                e.visualScale = 0.7f;
                AllMedications(e, Effectiveness.Effective, null);
            });

            CelulaEspumosa = Enemy("Celula_Espumosa", e =>
            {
                e.displayName = "Célula espumosa";
                e.threat = ThreatType.Inflamacion;
                e.biologia = "Un macrófago que se comió tanto colesterol que quedó lleno de gotitas de grasa (por eso se ve espumoso). " +
                    "En vez de limpiar, se queda en la pared e inflama la placa. El antiinflamatorio le hace más.";
                e.movement = EnemyMovement.Flying; e.flyHeight = 2.5f;
                e.maxHealth = 14f; e.moveSpeed = 2.4f;
                e.attackDamage = 10f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 1;
                e.tint = new Color(1f, 0.93f, 0.78f);
                e.visualScale = 1.15f;
                AllMedications(e, Effectiveness.Partial, Antiinflamatorio);
            });

            PlacaColesterol = Enemy("Bloqueo_PlacaColesterol", e =>
            {
                e.displayName = "Placa de colesterol";
                e.threat = ThreatType.Inflamacion;
                e.biologia = "Aterosclerosis: el colesterol LDL se mete en la pared de la arteria, la inflama y forma una placa que la va " +
                    "cerrando. Con cada golpe se hace más chica; el antiinflamatorio le hace más.";
                e.maxHealth = 45f;
                e.meleeDamageTakenMultiplier = 0f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 0;
                e.attackDamage = 8f;
                e.tint = new Color(1f, 0.86f, 0.45f);
                AllMedications(e, Effectiveness.Partial, Antiinflamatorio);
            });

            // Minijefe del tramo 3: una placa enorme que casi tapa la arteria; a la mitad se rompe y le sale un coágulo.
            AteromaJefe = Enemy("Jefe_Ateroma", e =>
            {
                e.displayName = "Placa de ateroma";
                e.threat = ThreatType.Inflamacion;
                e.biologia = "Una placa de ateroma grande: grasa, células espumosas y una capa dura encima. Si se rompe, las plaquetas " +
                    "forman un coágulo sobre ella y la arteria se tapa: así empiezan muchos infartos.";
                e.maxHealth = 450f;
                e.meleeDamageTakenMultiplier = 0f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 6;
                e.attackDamage = 9f;
                e.projectileColor = new Color(1f, 0.85f, 0.3f);
                e.tint = new Color(1f, 0.86f, 0.45f);
                AllMedications(e, Effectiveness.Partial, Antiinflamatorio);
            });
        }

        /// <summary>Riñón (secreto, acuático): E. coli, Leptospira, cercarias de Schistosoma, Proteus, su colonia y el cálculo.</summary>
        private void CreateKidneyEnemies()
        {
            Leptospira = Enemy("Bacteria_Leptospira", e =>
            {
                e.displayName = "Leptospira";
                e.threat = ThreatType.Bacteria;
                e.behavior = EnemyBehavior.Diver;
                e.biologia = "Bacteria en espiral que vive en el agua de las inundaciones (la orina de las ratas la contamina). Entra por la piel " +
                    "o los ojos y DAÑA EL RIÑÓN (leptospirosis). Nada como taladro y se lanza contra ti. Antibiótico.";
                e.movement = EnemyMovement.Flying; e.flyHeight = 1.6f;
                e.maxHealth = 20f; e.moveSpeed = 3.8f;
                e.attackWindup = 0.45f; e.attackCooldown = 2.2f; e.attackDamage = 10f; e.attackKnockback = 5f;
                e.tint = new Color(0.35f, 0.85f, 0.85f);
            });
            Cercaria = Enemy("Parasito_Cercaria", e =>
            {
                e.displayName = "Cercaria (Schistosoma)";
                e.threat = ThreatType.Parasito;
                e.behavior = EnemyBehavior.Frenzy;
                e.biologia = "La larva del Schistosoma: nada en enjambre en agua dulce con su cola en forma de Y y se mete por la piel. El " +
                    "adulto vive en las venas de la VEJIGA y causa sangre en la orina (esquistosomiasis). Antiparasitario (praziquantel).";
                e.movement = EnemyMovement.Flying; e.flyHeight = 0.6f;
                e.maxHealth = 8f; e.moveSpeed = 4.8f;
                e.attackDistance = 1f; e.attackWindup = 0.25f; e.attackCooldown = 1.6f; e.attackDamage = 5f; e.attackKnockback = 2f;
                e.linfocitoCharge = 0;
                e.tint = new Color(1f, 0.85f, 0.75f);
                e.visualScale = 0.7f;
            });
            Proteus = Enemy("Bacteria_Proteus", e =>
            {
                e.displayName = "Proteus mirabilis";
                e.threat = ThreatType.Bacteria;
                e.behavior = EnemyBehavior.Shooter;
                e.biologia = "Bacteria de las infecciones urinarias que hace la orina menos ácida (con su enzima ureasa) y así se forman " +
                    "CRISTALES y piedras (cálculos de estruvita). Nada rápido y escupe cristales. Antibiótico.";
                e.movement = EnemyMovement.Flying; e.flyHeight = 2.2f;
                e.maxHealth = 22f; e.moveSpeed = 3.6f;
                e.attackType = EnemyAttackType.Ranged;
                e.attackDistance = 6f; e.attackWindup = 0.5f; e.attackCooldown = 2.4f; e.attackDamage = 7f;
                e.projectileSpeed = 6f; e.projectileColor = new Color(0.95f, 0.97f, 1f);
                e.tint = new Color(0.65f, 0.5f, 0.95f);
            });
            ProteusColonia = Enemy("Jefe_ProteusColonia", e =>
            {
                e.displayName = "Colonia de Proteus";
                e.threat = ThreatType.Bacteria;
                e.biologia = "Muchas bacterias Proteus pegadas en una BIOPELÍCULA: una capa de baba que las protege de los medicamentos y " +
                    "donde crecen cristales. Por eso las infecciones con biopelícula tardan más en curarse. Antibiótico.";
                e.maxHealth = 300f;
                e.meleeDamageTakenMultiplier = 0.5f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 4;
                e.attackDamage = 8f;
                e.projectileColor = new Color(0.95f, 0.97f, 1f);
                e.tint = new Color(0.7f, 0.55f, 0.95f);
            });
            EColi = Enemy("Bacteria_EColi", e =>
            {
                e.displayName = "E. coli";
                e.threat = ThreatType.Bacteria;
                e.biologia = "Vive normalmente en el intestino, pero si sube por las vías urinarias causa INFECCIÓN URINARIA (la más común). " +
                    "Antibiótico. Se previene tomando agua y con buena higiene.";
                e.maxHealth = 24f; e.moveSpeed = 3.4f;
                e.attackDistance = 1.6f; e.attackWindup = 0.5f; e.attackCooldown = 1.8f; e.attackDamage = 9f; e.attackKnockback = 5f;
                e.tint = new Color(0.95f, 0.8f, 0.45f);
            });
            CalculoJefe = Enemy("Jefe_Calculo", e =>
            {
                e.displayName = "Cálculo gigante";
                e.threat = ThreatType.Inflamacion;
                e.biologia = "Una piedra de sales (casi siempre calcio) que se forma en el riñón cuando falta agua. Duele muchísimo al " +
                    "bajar por el uréter. Se rompe con LITOTRICIA (ondas de choque). Se previene tomando agua.";
                e.maxHealth = 450f;
                e.meleeDamageTakenMultiplier = 1f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 6;
                e.attackDamage = 10f;
                e.tint = new Color(0.85f, 0.73f, 0.45f);
                // Lo que importa es pegarle en la grieta: ahí todas las cápsulas sirven.
                AllMedications(e, Effectiveness.Effective, null);
            });
        }

        /// <summary>El gran trombo (jefe del sistema) y los émbolos que suelta.</summary>
        private void CreateThrombusEnemies()
        {
            Embolo = Enemy("Coagulo_Embolo", e =>
            {
                e.displayName = "Émbolo";
                e.threat = ThreatType.Coagulo;
                e.biologia = "Un pedazo de coágulo que se suelta y viaja por la sangre hasta que tapa algo. Anticoagulante.";
                e.movement = EnemyMovement.Flying; e.flyHeight = 2f;
                e.maxHealth = 10f; e.moveSpeed = 3f;
                e.attackDamage = 9f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 0;
                e.tint = new Color(0.8f, 0.6f, 0.9f);
                e.visualScale = 0.9f;
            });
            GranTromboJefe = Enemy("Jefe_GranTrombo", e =>
            {
                e.displayName = "El gran trombo";
                e.threat = ThreatType.Coagulo;
                e.biologia = "Un coágulo enorme que se formó en el corazón (por la arritmia) y se soltó. Si llega al cerebro tapa una " +
                    "arteria: eso es una EMBOLIA (infarto cerebral). Solo lo deshace el anticoagulante.";
                e.maxHealth = 900f;
                e.meleeDamageTakenMultiplier = 0f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 8;
                e.attackDamage = 9f;
                e.tint = new Color(0.8f, 0.6f, 0.9f);
            });
        }

        /// <summary>Corazón: estreptococo viridans (endocarditis), sus vegetaciones en las válvulas y el T. cruzi.</summary>
        private void CreateHeartEnemies()
        {
            SViridans = Enemy("Bacteria_SViridans", e =>
            {
                e.displayName = "Streptococcus viridans";
                e.threat = ThreatType.Bacteria;
                e.biologia = "Bacteria que vive en la boca y, si entra a la sangre, se pega a las VÁLVULAS del corazón: eso es la " +
                    "ENDOCARDITIS. Por eso hay que lavarse los dientes. Antibiótico.";
                e.maxHealth = 28f; e.moveSpeed = 2.8f;
                e.attackDistance = 1.6f; e.attackWindup = 0.6f; e.attackCooldown = 2f; e.attackDamage = 10f; e.attackKnockback = 5f;
                e.tint = new Color(0.55f, 0.85f, 0.55f);
            });
            Vegetacion = Enemy("Bloqueo_Vegetacion", e =>
            {
                e.displayName = "Vegetación de la válvula";
                e.threat = ThreatType.Bacteria;
                e.biologia = "Un montón de bacterias, plaquetas y fibrina pegado a una válvula (endocarditis). Tapa el paso: antibiótico.";
                e.maxHealth = 70f;
                e.meleeDamageTakenMultiplier = 0f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 0;
                e.tint = new Color(0.85f, 0.85f, 0.6f);
            });
            TCruziJefe = Enemy("Jefe_TCruzi", e =>
            {
                e.displayName = "Trypanosoma cruzi";
                e.threat = ThreatType.Parasito;
                e.biologia = "El parásito de la enfermedad de CHAGAS. Lo pasa la chinche besucona (triatomino), común en México y " +
                    "Latinoamérica. Se esconde en el músculo del corazón y con los años causa arritmias.";
                e.maxHealth = 560f;
                e.meleeDamageTakenMultiplier = 0.6f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 6;
                e.attackDamage = 9f;
                e.projectileColor = new Color(0.55f, 0.85f, 0.95f);
                e.tint = new Color(0.45f, 0.7f, 0.85f);
                foreach (MedicationData m in new[] { Antiviral, Antiinflamatorio, Mucolitico, Anticonvulsivo, Anticoagulante })
                    if (m != null) e.weaknesses.Add(new EnemyWeakness { medication = m, result = Effectiveness.Partial });
            });
        }

        /// <summary>Bazo: el virus de Epstein-Barr y los linfocitos B que infecta.</summary>
        private void CreateSpleenEnemies()
        {
            LinfocitoInfectado = Enemy("Linfocito_Infectado", e =>
            {
                e.displayName = "Linfocito B infectado";
                e.threat = ThreatType.Virus;
                e.biologia = "Un linfocito B (de los tuyos) con el virus de Epstein-Barr adentro: ya no te ayuda. Cúralo con antiviral.";
                e.maxHealth = 28f;
                e.meleeDamageTakenMultiplier = 1f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 0;
                e.tint = new Color(0.7f, 0.4f, 0.85f);
            });
            EpsteinBarrJefe = Enemy("Jefe_EpsteinBarr", e =>
            {
                e.displayName = "Virus de Epstein-Barr";
                e.threat = ThreatType.Virus;
                e.biologia = "Causa la mononucleosis (\"la enfermedad del beso\"): fiebre, garganta inflamada, mucho cansancio y el " +
                    "bazo hinchado. Se mete a los linfocitos B. No hay medicina que lo cure: lo controlan los linfocitos T.";
                e.maxHealth = 520f;
                e.meleeDamageTakenMultiplier = 0.5f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 6;
                e.attackDamage = 9f;
                e.projectileColor = new Color(0.75f, 0.5f, 1f);
                e.tint = new Color(0.55f, 0.85f, 0.7f);
                // A tus balas casi no les hace caso (ni al antiviral): le ganan tus linfocitos T.
                AllMedications(e, Effectiveness.Partial, null);
            });
        }

        /// <summary>Tramos 5 y 6 de Arterias y venas: la MALARIA (Plasmodium) entra por la picadura de un mosquito.</summary>
        private void CreateMalariaEnemies()
        {
            Esporozoito = Enemy("Plasmodium_Esporozoito", e =>
            {
                e.displayName = "Esporozoíto";
                e.threat = ThreatType.Parasito;
                e.biologia = "La forma del Plasmodium que mete el mosquito Anopheles al picar: unos gusanitos que nadan rapidísimo " +
                    "por la sangre rumbo al hígado. El antiparasitario es lo mejor.";
                e.movement = EnemyMovement.Flying; e.flyHeight = 2f;
                e.maxHealth = 6f; e.moveSpeed = 3.5f;
                e.attackDamage = 7f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 0;
                e.tint = new Color(0.78f, 0.55f, 1f);
                e.visualScale = 0.75f;
            });
            Merozoito = Enemy("Plasmodium_Merozoito", e =>
            {
                e.displayName = "Merozoíto";
                e.threat = ThreatType.Parasito;
                e.biologia = "Lo que sale cuando el Plasmodium revienta un glóbulo rojo: muchos parásitos chiquitos que se meten a " +
                    "otros glóbulos. Cada vez que revientan juntos, da fiebre.";
                e.movement = EnemyMovement.Flying; e.flyHeight = 2f;
                e.maxHealth = 9f; e.moveSpeed = 2.6f;
                e.attackDamage = 8f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 1;
                e.tint = new Color(0.66f, 0.38f, 0.9f);
                e.visualScale = 0.8f;
            });
            PlasmodiumJefe = Enemy("Jefe_Plasmodium", e =>
            {
                e.displayName = "Plasmodium";
                e.threat = ThreatType.Parasito;
                e.biologia = "El parásito de la malaria. Vive dentro de los glóbulos rojos, se multiplica y los revienta; por eso da " +
                    "fiebre cada 2 o 3 días y anemia. Lo pasa el mosquito Anopheles. Antiparasitario.";
                e.maxHealth = 520f;
                e.meleeDamageTakenMultiplier = 0f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 6;
                e.attackDamage = 9f;
                e.projectileColor = new Color(0.8f, 0.5f, 1f);
                e.tint = new Color(0.62f, 0.3f, 0.85f);
                // Lo demás le hace poquito (la tabla ya da antiparasitario fuerte y antibiótico/antihistamínico parcial).
                foreach (MedicationData m in new[] { Antiviral, Antiinflamatorio, Mucolitico, Anticonvulsivo, Anticoagulante })
                    if (m != null) e.weaknesses.Add(new EnemyWeakness { medication = m, result = Effectiveness.Partial });
            });
        }

        /// <summary>Que todas las cápsulas le hagan 'result' (menos 'except', que usa la tabla de su amenaza).</summary>
        private void AllMedications(EnemyData e, Effectiveness result, MedicationData except)
        {
            foreach (MedicationData m in new[] { Antibiotico, Antiviral, Antiinflamatorio, Antihistaminico, Antiparasitario, Mucolitico, Anticonvulsivo, Anticoagulante })
                if (m != null && m != except) e.weaknesses.Add(new EnemyWeakness { medication = m, result = result });
        }

        private void CreateNervousEnemies()
        {
            // Sistema Nervioso (GDD: virus y bacterias entre neuronas sanas). Patógenos reales del sistema nervioso.
            Poliovirus = Enemy("Virus_Poliovirus", e =>
            {
                e.displayName = "Poliovirus";
                e.threat = ThreatType.Virus;
                e.behavior = EnemyBehavior.Paralyzer;
                e.biologia = "Ataca las neuronas motoras de la médula espinal y causa parálisis. Su RAYO te deja lento un momento: apunta (línea delgada) y dispara.";
                e.movement = EnemyMovement.Flying; e.flyHeight = 2.2f;
                e.maxHealth = 18f; e.moveSpeed = 3.1f;
                e.attackType = EnemyAttackType.Ranged;
                e.attackDistance = 5f; e.attackWindup = 0.4f; e.attackCooldown = 2.4f; e.attackDamage = 8f;
                e.slowOnHit = 0.5f; e.slowDuration = 1.6f;
                e.projectileColor = new Color(0.6f, 0.9f, 1f);
                e.tint = new Color(0.4f, 0.8f, 0.95f);
                e.visualScale = 0.85f;
            });

            Meningococo = Enemy("Bacteria_Meningococo", e =>
            {
                e.displayName = "N. meningitidis (meningitis)";
                e.threat = ThreatType.Bacteria;
                e.behavior = EnemyBehavior.Charger;
                e.biologia = "Diplococo que pasa a la sangre y a las meninges en pocas horas: de las infecciones más rápidas que hay. " +
                    "Se agazapa y cruza a toda velocidad.";
                e.maxHealth = 22f; e.moveSpeed = 3.9f;
                e.attackDistance = 4f; e.attackWindup = 0.55f; e.attackCooldown = 2.2f; e.attackDamage = 12f; e.attackKnockback = 6f;
                e.tint = new Color(0.95f, 0.45f, 0.65f);
            });

            Herpes = Enemy("Virus_Herpes", e =>
            {
                e.displayName = "Herpes simple (encefalitis)";
                e.threat = ThreatType.Virus;
                e.behavior = EnemyBehavior.Latent;
                e.biologia = "Se esconde latente en los ganglios nerviosos, donde el sistema inmune no lo ve, y se reactiva. " +
                    "Se vuelve invisible e inmune, se acomoda y reaparece disparando en ráfaga. Antiviral: el aciclovir.";
                e.weaknesses.Add(new EnemyWeakness { medication = Antiinflamatorio, result = Effectiveness.Ineffective });
                e.movement = EnemyMovement.Flying; e.flyHeight = 3f;
                e.maxHealth = 30f; e.moveSpeed = 2.6f;
                e.attackType = EnemyAttackType.Ranged;
                e.attackDistance = 5.5f; e.attackWindup = 0.45f; e.attackCooldown = 2.6f; e.attackDamage = 8f;
                e.burstCount = 3; e.specialCooldown = 6f; e.specialDuration = 2.2f;
                e.projectileSpeed = 7.5f;
                e.projectileColor = new Color(0.75f, 0.45f, 1f);   // morado, como las bolitas de su dibujo
                e.tint = new Color(0.85f, 0.4f, 0.95f);
                e.visualScale = 1.25f;
            });

            NeuronaHiperexcitada = Enemy("Neurona_Hiperexcitada", e =>
            {
                e.displayName = "Neurona hiperexcitada";
                e.threat = ThreatType.Hiperexcitacion;
                e.maxHealth = 40f;
                e.meleeDamageTakenMultiplier = 0f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 0;
                e.tint = new Color(1f, 0.25f, 0.3f);
            });

            // Jefe: cadena de viriones (Centipede). La raíz solo lleva la barra; cada virión tiene su vida.
            RabiaVirion = Enemy("Jefe_Rabia_Virion", e =>
            {
                e.displayName = "Virión de rabia";
                e.threat = ThreatType.Virus;
                e.movement = EnemyMovement.Flying;
                e.maxHealth = 35f;
                e.meleeDamageTakenMultiplier = 0.6f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 1;
                e.tint = new Color(0.45f, 0.4f, 0.55f);
            });

            RabiaJefe = Enemy("Jefe_Rabia", e =>
            {
                e.displayName = "Virus de la rabia (jefe)";
                e.threat = ThreatType.Virus;
                e.movement = EnemyMovement.Flying; e.flyHeight = 5.2f;
                e.maxHealth = 100f;
                e.meleeDamageTakenMultiplier = 0.5f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 6;
                e.tint = new Color(0.42f, 0.38f, 0.5f);
            });

            Rabia = Enemy("Virus_Rabia", e =>
            {
                e.displayName = "Virus de la rabia";
                e.threat = ThreatType.Virus;
                e.behavior = EnemyBehavior.Frenzy;
                e.biologia = "Viaja por los nervios hasta el cerebro y vuelve agresivo al que infecta: muerde varias veces seguidas. " +
                    "Con síntomas, los antivirales casi no sirven; lo que se intenta es calmar el cerebro con sedantes " +
                    "(protocolo de Milwaukee). Por eso el Anticonvulsivo es su debilidad.";
                e.weaknesses.Add(new EnemyWeakness { medication = Antiviral, result = Effectiveness.Partial });
                e.weaknesses.Add(new EnemyWeakness { medication = Anticonvulsivo, result = Effectiveness.Effective });
                e.movement = EnemyMovement.Flying; e.flyHeight = 1f;
                e.maxHealth = 16f; e.moveSpeed = 4.7f;
                e.attackDistance = 1.2f; e.attackWindup = 0.3f; e.attackCooldown = 1.8f; e.attackDamage = 8f; e.attackKnockback = 3f;
                e.tint = new Color(0.95f, 0.85f, 0.4f);
            });
        }

        private LevelData Level(string system, string file, string id, string name, string scene, bool formula, string objective)
        {
            return SetupUtils.LoadOrCreate<LevelData>($"{DataFolder}/Systems/{system}/{file}.asset", l =>
            {
                l.id = id;
                l.displayName = name;
                l.sceneName = scene;
                l.grantsFormula = formula;
                l.objective = objective;
            });
        }

        /// <summary>Como Level, pero si el asset ya existía sin escena (nivel "próximamente") le pone la escena y el objetivo.</summary>
        private LevelData PlayableLevel(string system, string file, string id, string name, string scene, bool formula, string objective)
        {
            LevelData level = Level(system, file, id, name, scene, formula, objective);
            if (string.IsNullOrEmpty(level.sceneName))
            {
                level.sceneName = scene;
                level.objective = objective;
                UnityEditor.EditorUtility.SetDirty(level);
            }
            return level;
        }

        private SystemData MakeSystem(string folder, string id, string name, string number, MedicationData regional,
            Color color, Vector2 mapPosition, string description, params LevelData[] levels)
        {
            return SetupUtils.LoadOrCreate<SystemData>($"{DataFolder}/Systems/{folder}/{folder}.asset", s =>
            {
                s.id = id;
                s.displayName = name;
                s.mapNumber = number;
                s.regionalMedication = regional;
                s.mapColor = color;
                s.mapPosition = mapPosition;
                s.description = description;
                s.levels = new List<LevelData>(levels);
            });
        }

        private void CreateCatalog()
        {
            DigestivoEstomago = Level("Digestivo", "01_Estomago", "digestivo_estomago", "Estómago", "Digestivo_Estomago", false,
                "Aprende a pelear y cruza el ácido gástrico eliminando a H. pylori.");
            DigestivoDelgado = Level("Digestivo", "02_IntestinoDelgado", "digestivo_delgado", "Intestino delgado", "Digestivo_IntestinoDelgado", false,
                "Bilis, vellosidades, la tenia y un río de quimo. Persigue a la lombriz hasta la válvula ileocecal.");
            DigestivoGrueso = Level("Digestivo", "03_IntestinoGrueso", "digestivo_grueso", "Intestino grueso", "Digestivo_IntestinoGrueso", true,
                "Sube por el colon, cuida la flora buena, cruza el lodo y acaba con la lombriz.");
            DigestivoBoca = Level("Digestivo", "99_Boca_Secreto", "digestivo_boca", "Boca", "Digestivo_Boca", false,
                "Nivel secreto: columpiate con el hilo dental, esquiva las muelas y derrota al Sarro.");
            if (!DigestivoBoca.hidden)
            {
                DigestivoBoca.hidden = true;
                DigestivoBoca.secretId = "digestivo_boca";
                UnityEditor.EditorUtility.SetDirty(DigestivoBoca);
            }

            SystemData digestivo = MakeSystem("Digestivo", "digestivo", "Sistema Digestivo", "I", Antiparasitario,
                new Color(0.95f, 0.45f, 0.55f), new Vector2(0.5f, 0.3f),
                "Los patógenos se ingirieron en cápsulas y se instalaron aquí primero.",
                DigestivoEstomago, DigestivoDelgado, DigestivoGrueso, DigestivoBoca);

            RespiratorioNariz = PlayableLevel("Respiratorio", "01_Nariz", "respiratorio_nariz", "Nariz", "Respiratorio_Nariz", false,
                "Rinovirus, polen y S. aureus. Conoce el Mucolítico: disuelve la mucosidad.");
            RespiratorioTraquea = PlayableLevel("Respiratorio", "02_Traquea", "respiratorio_traquea", "Tráquea", "Respiratorio_Traquea", false,
                "Tos ferina (Bordetella pertussis). La inflamación cierra el paso: Antiinflamatorio.");
            RespiratorioPulmones = PlayableLevel("Respiratorio", "03_Pulmones", "respiratorio_pulmones", "Pulmones", "Respiratorio_Pulmones", true,
                "Recorre los bronquiolos: neumonía, influenza y el foco de tuberculosis. Fórmula 2.");

            RespiratorioAmigdalas = Level("Respiratorio", "99_Amigdalas_Secreto", "respiratorio_amigdalas", "Amígdalas", "Respiratorio_Amigdalas", false,
                "Nivel secreto: carga antígenos al centro germinal, fabrica linfocitos B y drena el absceso.");
            if (!RespiratorioAmigdalas.hidden)
            {
                RespiratorioAmigdalas.hidden = true;
                RespiratorioAmigdalas.secretId = "respiratorio_amigdalas";
                UnityEditor.EditorUtility.SetDirty(RespiratorioAmigdalas);
            }

            SystemData respiratorio = MakeSystem("Respiratorio", "respiratorio", "Sistema Respiratorio", "II", Mucolitico,
                new Color(0.45f, 0.75f, 1f), new Vector2(0.5f, 0.6f),
                "La mucosidad atrapa a los patógenos y los protege del medicamento.",
                RespiratorioNariz, RespiratorioTraquea, RespiratorioPulmones, RespiratorioAmigdalas);
            if (!respiratorio.levels.Contains(RespiratorioAmigdalas))
            {
                respiratorio.levels.Add(RespiratorioAmigdalas);
                UnityEditor.EditorUtility.SetDirty(respiratorio);
            }

            NerviosoMedula = PlayableLevel("Nervioso", "01_Medula", "nervioso_medula", "Médula espinal", "Nervioso_Medula", false,
                "Poliovirus y meningococo suben por la médula. Esquiva las tormentas eléctricas.");
            NerviosoCerebro = PlayableLevel("Nervioso", "02_Cerebro", "nervioso_cerebro", "Cerebro", "Nervioso_Cerebro", false,
                "Elimina la infección sin dañar las neuronas sanas. Llega al hipocampo con el cerebro a salvo.");
            if (NerviosoCerebro.grantsFormula)
            {
                // La fórmula 3 ahora la da el jefe del Hipocampo.
                NerviosoCerebro.grantsFormula = false;
                UnityEditor.EditorUtility.SetDirty(NerviosoCerebro);
            }
            NerviosoHipocampo = PlayableLevel("Nervioso", "03_Hipocampo", "nervioso_hipocampo", "Hipocampo", "Nervioso_Hipocampo", true,
                "El virus de la rabia llegó al hipocampo. Derrótalo sin perder el cerebro. Fórmula 3.");

            NerviosoOjo = Level("Nervioso", "99_Ojo_Secreto", "nervioso_ojo", "Ojo", "Nervioso_Ojo", false,
                "Nivel secreto: que el cuerpo no te vea. Escóndete, no inflames el ojo y sorprende al Toxoplasma.");
            if (!NerviosoOjo.hidden)
            {
                NerviosoOjo.hidden = true;
                NerviosoOjo.secretId = "nervioso_ojo";
                UnityEditor.EditorUtility.SetDirty(NerviosoOjo);
            }

            SystemData nervioso = MakeSystem("Nervioso", "nervioso", "Sistema Nervioso", "III", Anticonvulsivo,
                new Color(0.7f, 0.5f, 1f), new Vector2(0.5f, 0.9f),
                "Tormentas eléctricas y tejido sano que hay que proteger.",
                NerviosoMedula, NerviosoCerebro, NerviosoHipocampo, NerviosoOjo);
            if (!nervioso.levels.Contains(NerviosoOjo))
            {
                nervioso.levels.Add(NerviosoOjo);
                UnityEditor.EditorUtility.SetDirty(nervioso);
            }
            if (!nervioso.levels.Contains(NerviosoHipocampo))
            {
                nervioso.levels.Add(NerviosoHipocampo);
                UnityEditor.EditorUtility.SetDirty(nervioso);
            }
            if (nervioso.regionalMedication == null)
            {
                nervioso.regionalMedication = Anticonvulsivo;
                UnityEditor.EditorUtility.SetDirty(nervioso);
            }

            // De menos vital a más vital: sin bazo se puede vivir, sin corazón no. Ver el plan en el proyecto.
            CirculatorioBazo = PlayableLevel("Circulatorio", "01_Bazo", "circulatorio_bazo", "Bazo", "Circulatorio_Bazo", false,
                "El filtro de la sangre: bandas de glóbulos, ranuras, macrófagos y la reserva de plaquetas. Jefe: el virus de Epstein-Barr.");
            CirculatorioArteriasVenas = PlayableLevel("Circulatorio", "02_ArteriasVenas", "circulatorio_arterias_venas", "Arterias y venas",
                "Circulatorio_ArteriasVenas", false,
                "Vuela en la nanocápsula: el ventrículo te dispara a la aorta. Avanza con cada latido y deshaz los coágulos con Anticoagulante.");
            CirculatorioCorazon = PlayableLevel("Circulatorio", "03_Corazon", "circulatorio_corazon", "Corazón", "Circulatorio_Corazon", true,
                "Válvulas al ritmo del latido, ventrículos que se aprietan, cuerdas tendinosas y el Trypanosoma cruzi (Chagas)."); 
            CirculatorioGranTrombo = PlayableLevel("Circulatorio", "04_GranTrombo", "circulatorio_gran_trombo", "El gran trombo",
                "Circulatorio_GranTrombo", false,
                "Jefe del sistema: persigue al coágulo por la carótida y deshazlo con anticoagulante antes de que llegue al cerebro.");
            CirculatorioRinon = Level("Circulatorio", "99_Rinon_Secreto", "circulatorio_rinon", "Riñón", "Circulatorio_Rinon", false,
                "Nivel secreto: sube la presión del glomérulo, nada en el asa de Henle, juega con el agua y rompe el cálculo gigante.");
            if (!CirculatorioRinon.hidden)
            {
                CirculatorioRinon.hidden = true;
                CirculatorioRinon.secretId = "circulatorio_rinon";
                UnityEditor.EditorUtility.SetDirty(CirculatorioRinon);
            }

            SystemData circulatorio = MakeSystem("Circulatorio", "circulatorio", "Sistema Circulatorio", "IV", Anticoagulante,
                new Color(0.9f, 0.2f, 0.25f), new Vector2(0.45f, 0.65f),
                "La Cepa Madre bombea variantes resistentes a todo el cuerpo.",
                CirculatorioBazo, CirculatorioArteriasVenas, CirculatorioCorazon, CirculatorioGranTrombo, CirculatorioRinon);
            if (circulatorio.regionalMedication != Anticoagulante || !circulatorio.levels.Contains(CirculatorioArteriasVenas))
            {
                circulatorio.regionalMedication = Anticoagulante;
                circulatorio.levels = new List<LevelData> { CirculatorioBazo, CirculatorioArteriasVenas, CirculatorioCorazon, CirculatorioGranTrombo };
                UnityEditor.EditorUtility.SetDirty(circulatorio);
            }
            if (!circulatorio.levels.Contains(CirculatorioRinon))
            {
                circulatorio.levels.Add(CirculatorioRinon);
                UnityEditor.EditorUtility.SetDirty(circulatorio);
            }

            Catalog = SetupUtils.LoadOrCreate<GameCatalog>($"{DataFolder}/Resources/{GameCatalog.ResourcePath}.asset", c =>
            {
                c.systems = new List<SystemData> { digestivo, respiratorio, nervioso, circulatorio };
            });
        }
            // ---------- Bestiario (fichas tipo Pokédex) ----------

        public Bestiary Bestiary;

        private const string EnemyArt = "Assets/_Project/Art/Enemigos";
        private const string BossArt = "Assets/_Project/Art/Jefes";

        private static BestiaryEntry Ficha(string system, string foundIn, string name, string scientific, string kind, string portrait,
            string disease, string facts, string treatment, string prevention, params EnemyData[] enemies)
        {
            return new BestiaryEntry
            {
                id = enemies.Length > 0 && enemies[0] != null ? enemies[0].name : name,
                system = system, foundIn = foundIn, name = name, scientific = scientific, kind = kind,
                portraitPath = portrait, disease = disease, facts = facts, treatment = treatment, prevention = prevention,
                enemies = enemies
            };
        }

        /// <summary>
        /// Textos de las fichas. Son datos reales y generales (sin dosis): el juego enseña qué es cada germen, no receta.
        /// La pantalla lo aclara: solo un médico puede diagnosticar y recetar.
        /// </summary>
        private void CreateBestiary()
        {
            Bestiary = SetupUtils.LoadOrCreate<Bestiary>($"{DataFolder}/Resources/{Bestiary.ResourcePath}.asset", null);
            Bestiary.table = Table;
            Bestiary.medications = new List<MedicationData> { Antibiotico, Antiviral, Antiparasitario, Antiinflamatorio, Antihistaminico, Mucolitico, Anticonvulsivo, Anticoagulante };
            const string D = "Digestivo", R = "Respiratorio", N = "Nervioso", C = "Circulatorio";
            const string CircArt = EnemyArt + "/Circulatorio";
            var list = new List<BestiaryEntry>
            {
                // ===== DIGESTIVO =====
                Ficha(D, "Estómago", "H. pylori", "Helicobacter pylori", "Bacteria", $"{EnemyArt}/Estomago/HPylori_Normal.png",
                    "Gastritis (estómago inflamado) y úlceras. Con los años aumenta el riesgo de cáncer de estómago.",
                    "Cerca de la mitad de las personas del mundo la tienen, casi siempre sin síntomas. Sobrevive al ácido del estómago fabricando ureasa, que convierte la urea en amoniaco y neutraliza el ácido a su alrededor. Sus descubridores, Barry Marshall y Robin Warren, ganaron el Premio Nobel en 2005; Marshall llegó a tomársela para demostrar que causaba gastritis.",
                    "Una combinación de dos antibióticos más un medicamento que baja el ácido del estómago (como el omeprazol), durante 10 a 14 días. Siempre la receta un médico.",
                    "Tomar agua limpia, lavarse las manos antes de comer y lavar bien la comida.",
                    HPylori, HPyloriMadre, HPyloriUreasa),
                Ficha(D, "Estómago", "Anisakis", "Anisakis simplex", "Parásito (gusano)", $"{EnemyArt}/Estomago/Anisakis_Arrastra.png",
                    "Anisakiasis: dolor fuerte de estómago, náuseas y a veces alergia, horas después de comer pescado crudo.",
                    "Es una larva de unos 2 cm que vive en peces y calamares. En las personas no puede terminar su ciclo de vida, pero intenta clavarse en la pared del estómago. Se encuentra en el sushi, el ceviche o el pescado mal cocido.",
                    "Casi siempre el médico la saca con un endoscopio (una camarita que entra por la boca). En algunos casos se usan antiparasitarios.",
                    "Cocinar bien el pescado o congelarlo bien frío (unos -20 °C por varios días) antes de comerlo crudo.",
                    Anisakis),
                Ficha(D, "Estómago (jefe)", "Bezoar", "Bezoar (fitobezoar / tricobezoar)", "Jefe · no es un germen", $"{BossArt}/Bezoar/Bezoar_Normal.png",
                    "Una bola de comida, fibras o pelo que no se digiere y se queda atorada en el estómago. Da llenura, dolor y vómito.",
                    "No es un ser vivo: se forma con cosas que el estómago no puede deshacer, como cáscaras y fibras duras (fitobezoar), pelo (tricobezoar) o chicle tragado muchas veces. En algunos casos los médicos han logrado disolver bezoares de fibras con bebidas especiales, ¡incluso con refresco de cola!",
                    "Según el tamaño: disolverlo con bebidas, sacarlo con endoscopio o, si es muy grande, con cirugía.",
                    "Masticar bien, no tragar chicle ni pelo y tomar suficiente agua.",
                    Bezoar),
                Ficha(D, "Intestino delgado (jefe)", "Tenia (solitaria)", "Taenia solium / Taenia saginata", "Parásito (gusano plano)", $"{BossArt}/Tenia/Cabeza_Normal.png",
                    "Teniasis: casi no da síntomas, a veces dolor de panza y pérdida de peso. Los huevos de la tenia del cerdo pueden causar cisticercosis (quistes en músculos y cerebro).",
                    "Puede medir varios metros: la de la res llega a más de 10 m. Su cabeza (escólex) tiene ventosas y ganchos para pegarse al intestino, y su cuerpo está hecho de segmentos (proglótidos) llenos de miles de huevos que se van soltando.",
                    "Antiparasitarios que la matan de una vez (como el praziquantel). Siempre los receta un médico.",
                    "Comer carne de cerdo y de res bien cocida y lavarse las manos después de ir al baño.",
                    Tenia, TeniaSegmento, Proglotido),
                Ficha(D, "Intestino delgado", "Giardia", "Giardia lamblia", "Parásito (protozoo)", $"{EnemyArt}/Delgado/Giardia_Normal.png",
                    "Giardiasis: diarrea, gases, cólicos y panza inflada.",
                    "Es una sola célula con forma de pera y parece tener cara: sus dos núcleos se ven como ojos. Se pega a la pared del intestino con un disco que funciona como ventosa. Sus quistes aguantan semanas en agua fría, por eso se contagia por agua sucia.",
                    "Antiparasitarios como el metronidazol, recetados por un médico, y mucha agua para no deshidratarse.",
                    "Tomar agua hervida o purificada y lavarse las manos.",
                    Giardia),
                Ficha(D, "Intestino delgado", "Rotavirus", "Rotavirus", "Virus", $"{EnemyArt}/Delgado/Rotavirus_Normal.png",
                    "Gastroenteritis: diarrea y vómito muy fuertes, sobre todo en bebés y niños pequeños.",
                    "Su nombre viene del latín 'rota', rueda, porque visto al microscopio parece una rueda. Es muy contagioso: unas pocas partículas bastan para enfermar a alguien.",
                    "No hay un medicamento que lo mate. Lo importante es no deshidratarse: suero oral y líquidos, y si es grave, ir al médico.",
                    "La vacuna contra el rotavirus (se pone a los bebés) y lavarse las manos.",
                    Rotavirus),
                Ficha(D, "Intestino delgado y grueso (jefe)", "Lombriz intestinal", "Ascaris lumbricoides", "Parásito (gusano)", $"{BossArt}/Lombriz/Cabeza_Sale.png",
                    "Ascariasis: dolor de panza y, en niños con muchas lombrices, desnutrición y bloqueo del intestino.",
                    "La hembra puede medir hasta 35 cm y poner unos 200 000 huevos al día. Sus larvas hacen un viaje increíble: salen del intestino, pasan por la sangre a los pulmones, suben a la garganta y se tragan de nuevo.",
                    "Antiparasitarios como el albendazol o el mebendazol, recetados por un médico.",
                    "Lavarse las manos, lavar frutas y verduras y usar baño (no hacer en la tierra).",
                    LombrizHuye, Lombriz),
                Ficha(D, "Intestino grueso", "Amiba", "Entamoeba histolytica", "Parásito (protozoo)", $"{EnemyArt}/Grueso/Entamoeba_Normal.png",
                    "Amebiasis: diarrea con sangre (disentería) y, si llega al hígado, un absceso hepático.",
                    "Cambia de forma estirando 'pies falsos' (pseudópodos). 'Histolytica' significa 'que disuelve tejido': rompe la pared del intestino y hasta se come glóbulos rojos.",
                    "Antiparasitarios como el metronidazol y luego otro que limpia los quistes del intestino. Siempre con receta médica.",
                    "Agua purificada, lavar bien frutas y verduras y lavarse las manos.",
                    Entamoeba),
                Ficha(D, "Intestino grueso", "C. difficile", "Clostridioides difficile", "Bacteria", $"{EnemyArt}/Grueso/CDifficile_Normal.png",
                    "Colitis: diarrea muy fuerte, sobre todo después de tomar antibióticos.",
                    "Aprovecha cuando los antibióticos matan a la flora buena del intestino y se queda con el lugar. Forma esporas tan duras que el gel con alcohol no las mata: hay que lavarse con agua y jabón.",
                    "Antibióticos especiales tomados por boca (como la vancomicina). Si regresa muchas veces, se puede hacer un trasplante de flora intestinal sana.",
                    "Usar antibióticos solo cuando el médico los receta y lavarse las manos con agua y jabón.",
                    CDifficile),
                Ficha(D, "Intestino grueso", "Oxiuro", "Enterobius vermicularis", "Parásito (gusano)", $"{EnemyArt}/Grueso/Oxiuro_Normal.png",
                    "Oxiuriasis: comezón en las pompis, sobre todo en la noche. Es la lombriz más común en niños.",
                    "Mide como 1 cm y parece un hilito blanco. La hembra sale de noche a poner sus huevos alrededor del ano; al rascarse, los huevos se quedan en las uñas y pasan a la boca o a otras personas.",
                    "Antiparasitarios (como el mebendazol) para TODA la familia a la vez, y repetir a las dos semanas, según indique el médico.",
                    "Uñas cortas, lavarse las manos, bañarse en la mañana y lavar sábanas y pijamas.",
                    Oxiuro, OxiuroHuevo),
                Ficha(D, "Intestino grueso (aliada)", "Flora intestinal", "Microbiota: Lactobacillus, Bifidobacterium y muchas más", "Aliada", $"{EnemyArt}/Grueso/Flora_Normal.png",
                    "¡No causa enfermedad! Al contrario: si se daña (por ejemplo con antibióticos), otros gérmenes como el C. difficile aprovechan.",
                    "En tu intestino viven billones de bacterias, casi tantas como células tiene tu cuerpo. Te ayudan a digerir la fibra, fabrican vitaminas como la K y ocupan el espacio para que los gérmenes malos no se instalen.",
                    "No se trata: se cuida.",
                    "Comer fibra (frutas, verduras, frijoles), alimentos fermentados como el yogur y no usar antibióticos sin receta.",
                    Flora),
                Ficha(D, "Boca (secreto)", "S. mutans", "Streptococcus mutans", "Bacteria", $"{EnemyArt}/Boca/SMutans_Normal.png",
                    "Caries: hoyitos en los dientes que duelen y pueden infectar el diente por dentro.",
                    "Se alimenta del azúcar que comes y lo convierte en ácido, que disuelve el esmalte de los dientes. Junto con otras bacterias forma la placa, una capa pegajosa sobre los dientes.",
                    "El dentista limpia la caries y la tapa con resina. El flúor ayuda a reparar el esmalte cuando la caries apenas empieza.",
                    "Cepillarse dos veces al día con pasta con flúor, usar hilo dental y comer menos dulces.",
                    SMutans),
                Ficha(D, "Boca (jefe)", "Sarro", "Cálculo dental", "Jefe · placa endurecida", $"{BossArt}/Sarro/Sarro_Normal.png",
                    "Inflama las encías (gingivitis): se ponen rojas y sangran. Si sigue, puede aflojar los dientes.",
                    "Es placa bacteriana que se endureció con los minerales de la saliva, como si fuera piedra. Una vez duro, el cepillo ya no lo quita.",
                    "Una limpieza con el dentista, que lo raspa con instrumentos especiales.",
                    "Cepillarse bien y usar hilo dental todos los días para que la placa no llegue a endurecerse.",
                    SarroJefe),

                // ===== RESPIRATORIO =====
                Ficha(R, "Nariz", "Rinovirus", "Rhinovirus", "Virus", $"{EnemyArt}/Respiratorio/Rinovirus_Normal.png",
                    "El resfriado común: nariz tapada, estornudos y dolor de garganta.",
                    "Hay más de 150 tipos distintos, por eso no existe una vacuna y te puedes resfriar varias veces al año. Le gusta la nariz porque ahí la temperatura es un poco más fría que en el resto del cuerpo.",
                    "No hay medicamento que lo mate y los antibióticos NO sirven contra virus. Se cura solo en unos días con descanso y líquidos.",
                    "Lavarse las manos, no tocarse la cara y taparse al estornudar.",
                    Rinovirus),
                Ficha(R, "Nariz", "Polen", "Granos de polen", "Alérgeno · no es un germen", $"{EnemyArt}/Respiratorio/Polen_Normal.png",
                    "Rinitis alérgica (fiebre del heno): estornudos, comezón en la nariz y en los ojos.",
                    "El polen es inofensivo, pero en algunas personas el sistema de defensa lo confunde con una amenaza y suelta histamina, la sustancia que causa los estornudos y la comezón.",
                    "Antihistamínicos, que bloquean a la histamina, o sprays para la nariz, según indique el médico.",
                    "En temporada de polen: cerrar ventanas, lavarse la cara al llegar a casa y evitar salir con mucho viento.",
                    Polen),
                Ficha(R, "Nariz", "S. aureus", "Staphylococcus aureus", "Bacteria", $"{EnemyArt}/Respiratorio/SAureus_Normal.png",
                    "Infecciones de la piel (granos, forúnculos), y a veces neumonía o intoxicación por comida.",
                    "Alrededor de 3 de cada 10 personas lo llevan en la nariz sin enfermarse. Su nombre viene del griego 'staphylé', racimo de uvas, por cómo se agrupan sus cocos. Algunas cepas se volvieron resistentes a muchos antibióticos (MRSA).",
                    "Antibióticos elegidos por el médico según la cepa y, si hay pus, drenarlo.",
                    "Lavarse las manos, limpiar y tapar las heridas y no compartir toallas.",
                    SAureus, SAureusCoco),
                Ficha(R, "Tráquea (y su reina, jefa)", "Tos ferina", "Bordetella pertussis", "Bacteria", $"{EnemyArt}/Respiratorio/Pertussis_Normal.png",
                    "Tos ferina: ataques de tos sin parar que terminan en un 'gallo' al tomar aire. Es muy peligrosa en bebés.",
                    "Su toxina paraliza los cilios de la tráquea, los pelitos que barren el moco. La tos puede durar semanas: en China la llaman 'la tos de los 100 días'.",
                    "Antibióticos (como la azitromicina), mejor si se empiezan pronto. Siempre con receta.",
                    "Vacunas: los bebés en su esquema y las embarazadas, para proteger al bebé desde que nace.",
                    Pertussis, TosFerinaReina),
                Ficha(R, "Pulmones", "Neumococo", "Streptococcus pneumoniae", "Bacteria", $"{EnemyArt}/Respiratorio/Neumococo_Normal.png",
                    "Neumonía, infecciones de oído (otitis), sinusitis y, a veces, meningitis.",
                    "Viaja en pareja (diplococo) dentro de una cápsula que lo esconde de las defensas. Hay alrededor de 100 tipos distintos de cápsula.",
                    "Antibióticos recetados por el médico, según qué tan grave sea.",
                    "La vacuna contra el neumococo, lavarse las manos y no fumar cerca de nadie.",
                    Neumococo),
                Ficha(R, "Pulmones", "Influenza", "Virus de la influenza", "Virus", $"{EnemyArt}/Respiratorio/Influenza_Normal.png",
                    "La gripe: fiebre alta, dolor de cuerpo, cansancio y tos. Es más fuerte que un resfriado.",
                    "Su superficie está cubierta de dos proteínas: la hemaglutinina (H) y la neuraminidasa (N). De ahí nombres como H1N1. Cambia un poco cada año, por eso la vacuna se renueva cada año.",
                    "Antivirales como el oseltamivir sirven si se empiezan en los primeros días, y siempre con receta. Descanso y líquidos.",
                    "La vacuna cada año, lavarse las manos y taparse al toser.",
                    Influenza),
                Ficha(R, "Pulmones (y su nave, jefa)", "Tuberculosis", "Mycobacterium tuberculosis", "Bacteria", $"{EnemyArt}/Respiratorio/Tuberculosis_Normal.png",
                    "Tuberculosis: sobre todo en los pulmones. Tos por semanas, fiebre, sudor en la noche y pérdida de peso.",
                    "Tiene una capa de cera (ácidos micólicos) que la vuelve muy resistente y crece muy lento: tarda casi un día en dividirse. El cuerpo intenta encerrarla en bolitas de defensas llamadas granulomas. Es una de las infecciones que más muertes causa en el mundo.",
                    "Varios antibióticos juntos durante por lo menos 6 meses, sin saltarse ninguna toma, porque si no se vuelve resistente.",
                    "La vacuna BCG (protege a los niños de las formas graves) y detectar y tratar a tiempo a quien está enfermo.",
                    Tuberculosis, Colmena),
                Ficha(R, "Amígdalas (secreto)", "Estreptococo de la garganta", "Streptococcus pyogenes", "Bacteria", $"{EnemyArt}/Respiratorio/SPyogenes_Normal.png",
                    "Faringitis o 'anginas': dolor fuerte de garganta, fiebre y amígdalas con puntitos blancos. También escarlatina.",
                    "Forma cadenitas de cocos. Si la faringitis no se trata, en raras ocasiones el cuerpo se confunde y ataca al corazón y las articulaciones (fiebre reumática).",
                    "Antibióticos como la penicilina o la amoxicilina, tomados todos los días que diga el médico.",
                    "Lavarse las manos y no compartir vasos ni cubiertos.",
                    SPyogenes),
                Ficha(R, "Amígdalas (jefe)", "Absceso periamigdalino", "Absceso periamigdalino", "Jefe · complicación", $"{BossArt}/Absceso/Absceso_Normal.png",
                    "Una bolsa de pus junto a la amígdala después de una infección de garganta. Duele mucho al tragar y cuesta abrir la boca.",
                    "Es una complicación: aparece cuando una faringitis se pasa del tejido de la amígdala a los alrededores. Como es una bolsa cerrada, los medicamentos entran con dificultad.",
                    "El médico lo drena (le saca el pus) y da antibióticos.",
                    "Tratar a tiempo y completa cualquier faringitis.",
                    AbscesoJefe),

                // ===== NERVIOSO =====
                Ficha(N, "Médula espinal", "Poliovirus", "Poliovirus", "Virus", $"{EnemyArt}/Nervioso/Poliovirus_Normal.png",
                    "Poliomielitis: en algunos casos ataca las neuronas de la médula que mueven los músculos y causa parálisis.",
                    "Gracias a las vacunas ya casi desapareció del mundo: el virus salvaje solo sigue circulando en muy pocos países. Es uno de los grandes logros de la medicina.",
                    "No hay medicamento que lo cure. Solo se atienden los síntomas y se hace rehabilitación.",
                    "La vacuna contra la polio (en gotas o inyectada).",
                    Poliovirus),
                Ficha(N, "Médula y cerebro", "Meningococo", "Neisseria meningitidis", "Bacteria", $"{EnemyArt}/Nervioso/Meningococo_Normal.png",
                    "Meningitis: inflamación de las membranas que cubren el cerebro. Fiebre, dolor de cabeza y cuello duro. Avanza en horas: es una emergencia.",
                    "Viaja en pareja (diplococo) con forma de dos frijolitos. Muchas personas lo llevan en la garganta sin enfermarse.",
                    "Antibióticos por la vena en el hospital, lo más rápido posible.",
                    "Las vacunas contra el meningococo.",
                    Meningococo),
                Ficha(N, "Cerebro y ojo", "Herpes", "Virus del herpes simple tipo 1", "Virus", $"{EnemyArt}/Nervioso/Herpes_Normal.png",
                    "Fuegos o herpes en los labios. En raros casos inflama el cerebro (encefalitis) o el ojo (queratitis).",
                    "Una vez que entra, se queda 'dormido' (latente) en los nervios para siempre y despierta con el estrés, la fiebre o el sol. Dos de cada tres personas menores de 50 años lo tienen.",
                    "Antivirales como el aciclovir lo calman, pero no lo sacan del cuerpo.",
                    "No compartir vasos ni dar besos cuando hay ampollas, y lavarse las manos.",
                    Herpes),
                Ficha(N, "Hipocampo (y su jefe)", "Rabia", "Virus de la rabia (Lyssavirus)", "Virus", $"{EnemyArt}/Nervioso/Rabia_Normal.png",
                    "Rabia: ataca el cerebro. Una vez que empiezan los síntomas casi siempre es mortal. Uno de sus síntomas es el miedo al agua (hidrofobia).",
                    "Tiene forma de bala. Entra por la mordida de un animal infectado (perros, murciélagos) y viaja despacio por los nervios hasta el cerebro: por eso hay tiempo para vacunarse después de la mordida.",
                    "Si te muerde un animal: lavar la herida con agua y jabón por 15 minutos e ir de inmediato al médico para la vacuna (y a veces anticuerpos).",
                    "Vacunar a perros y gatos y no tocar animales salvajes, sobre todo murciélagos.",
                    Rabia, RabiaJefe, RabiaVirion),
                Ficha(N, "Ojo (secreto, jefe)", "Toxoplasma", "Toxoplasma gondii", "Parásito (protozoo)", $"{BossArt}/Toxoplasma/Toxoplasma_Normal.png",
                    "Toxoplasmosis: casi siempre sin síntomas, pero en el ojo puede inflamar la retina, y es peligrosa en el embarazo.",
                    "Su hogar favorito es el gato, donde se reproduce. Se contagia por carne mal cocida o al limpiar la arena del gato. Cerca de un tercio de las personas del mundo lo tienen dormido en pequeños quistes.",
                    "Cuando hace falta, una combinación de antiparasitarios recetada por el médico.",
                    "Cocinar bien la carne, lavar frutas y verduras y lavarse las manos después de limpiar la arena del gato.",
                    ToxoplasmaJefe),

                // ===== CIRCULATORIO =====
                Ficha(C, "Aorta y arterias", "Coágulo (plaquetas)", "Trombo de plaquetas", "Coágulo · no es un germen", $"{CircArt}/Plaquetas_Normal.png",
                    "Trombosis: un coágulo que se forma DENTRO de un vaso y tapa el paso de la sangre.",
                    "Las plaquetas son pedacitos de células que tapan las heridas: sin ellas sangraríamos sin parar. El problema es cuando se juntan donde no hay herida.",
                    "Anticoagulantes o antiagregantes que receta el médico: hacen que la sangre no forme coágulos tan fácil.",
                    "Moverse: caminar y no pasar muchas horas sentado sin pararse, tomar agua y no fumar.",
                    Plaquetas),
                Ficha(C, "Arteria femoral", "Colesterol LDL", "Lipoproteína de baja densidad", "Grasa · no es un germen", $"{CircArt}/LDL_Normal.png",
                    "Aterosclerosis: el colesterol LDL se mete en la pared de las arterias y forma placas que las endurecen y las hacen angostas.",
                    "Le dicen 'colesterol malo'. El cuerpo sí necesita colesterol, pero el exceso de LDL se acumula. El 'bueno' (HDL) lo lleva de regreso al hígado.",
                    "Comer mejor y hacer ejercicio; si el médico lo indica, medicinas que bajan el colesterol.",
                    "Frutas, verduras y fibra; menos frituras y comida chatarra; ejercicio y no fumar.",
                    Ldl, PlacaColesterol),
                Ficha(C, "Arteria femoral (minijefe)", "Placa de ateroma", "Ateroma", "Jefe · placa de grasa", $"{BossArt}/Ateroma/Ateroma_Normal.png",
                    "La placa hace angosta la arteria y la sangre pasa poco. Si se rompe, encima se forma un coágulo que puede taparla toda (infarto).",
                    "Se forma con colesterol, células espumosas (macrófagos que se tragaron demasiada grasa), calcio y fibras. Crece en silencio durante años.",
                    "Medicinas y, si hace falta, el médico abre la arteria con un globito y una mallita (stent) o con cirugía.",
                    "Lo mismo que contra el LDL: comer bien, moverse, no fumar y cuidar la presión.",
                    AteromaJefe, CelulaEspumosa),
                Ficha(C, "Capilares y venas (y su jefe)", "Plasmodium (malaria)", "Plasmodium falciparum", "Parásito (protozoo)", $"{BossArt}/Plasmodium/Plasmodium_Normal.png",
                    "Paludismo o malaria: fiebre muy alta que va y viene, escalofríos y anemia. Puede ser grave, sobre todo en niños.",
                    "Lo pasa la picadura del mosquito Anopheles hembra. Primero se esconde en el hígado (esporozoíto) y luego se mete a los glóbulos rojos (merozoíto), se multiplica y los revienta: por eso la fiebre llega en oleadas.",
                    "Antipalúdicos (antiparasitarios) que receta el médico, lo antes posible.",
                    "Mosquiteros, repelente y manga larga; no dejar agua estancada. En zonas con malaria ya hay vacunas para niños, y al viajar el médico puede dar medicina preventiva.",
                    PlasmodiumJefe, Esporozoito, Merozoito),
                Ficha(C, "Bazo (jefe)", "Epstein-Barr", "Virus de Epstein-Barr (herpesvirus humano 4)", "Virus", $"{BossArt}/EpsteinBarr/EpsteinBarr_Normal.png",
                    "Mononucleosis ('la enfermedad del beso'): fiebre, garganta muy inflamada, ganglios grandes, mucho cansancio y a veces el bazo crece.",
                    "Infecta a los linfocitos B (unas defensas) y se queda dormido en ellos de por vida. Más de 9 de cada 10 adultos del mundo lo tienen. Con el bazo grande hay que evitar golpes y deportes de contacto unas semanas.",
                    "No hay un antiviral de rutina: reposo, líquidos y lo que indique el médico para la fiebre y el dolor.",
                    "No compartir vasos, cubiertos ni besos con alguien enfermo.",
                    EpsteinBarrJefe, LinfocitoInfectado),
                Ficha(C, "Corazón", "Estreptococo viridans", "Streptococcus del grupo viridans", "Bacteria", $"{CircArt}/SViridans_Normal.png",
                    "Endocarditis: infección de las válvulas del corazón. Forma 'vegetaciones' (grumos de bacterias, plaquetas y fibrina) que dañan la válvula y pueden soltar pedazos.",
                    "Vive normalmente en la boca. Si las encías sangran y hay una válvula dañada, puede entrar a la sangre y pegarse al corazón.",
                    "Antibióticos por la vena durante varias semanas en el hospital; a veces cirugía para cambiar la válvula.",
                    "Cuidar los dientes: cepillarse, usar hilo dental e ir al dentista.",
                    SViridans, Vegetacion),
                Ficha(C, "Corazón (jefe)", "Trypanosoma cruzi", "Trypanosoma cruzi", "Parásito (protozoo)", $"{BossArt}/TCruzi/TCruzi_Normal.png",
                    "Enfermedad de Chagas: al principio casi no da síntomas; años después puede agrandar y debilitar el corazón y causar arritmias.",
                    "Lo pasa la chinche besucona (triatomino): pica de noche y deja el parásito en su popó, que entra al rascarse. También pasa de mamá a bebé o por transfusiones. Es común en zonas rurales de América Latina.",
                    "Antiparasitarios que receta el médico; funcionan mejor si se dan pronto.",
                    "Enjarrar paredes y sellar las grietas de la casa, usar pabellón y revisar camas y rincones.",
                    TCruziJefe),
                Ficha(C, "Carótida (jefe del sistema)", "El gran trombo", "Trombo y émbolos", "Jefe · coágulo que viaja", $"{BossArt}/GranTrombo/GranTrombo_Normal.png",
                    "Embolia: un coágulo (o un pedazo de él) viaja por la sangre y tapa un vaso lejos de donde se formó. En el cerebro causa un infarto cerebral (EVC).",
                    "Señales de un EVC: la cara se ve chueca, un brazo no tiene fuerza, la persona habla raro. Si las ves, ¡pide ayuda y llama a emergencias de inmediato! Cada minuto cuenta.",
                    "En el hospital, lo antes posible: medicinas que disuelven el coágulo o sacarlo con un tubito (catéter).",
                    "Moverse, no fumar, cuidar la presión y el colesterol, y tomar los anticoagulantes si el médico los indicó.",
                    GranTromboJefe, Embolo),
                Ficha(C, "Riñón (secreto)", "E. coli", "Escherichia coli", "Bacteria", $"{CircArt}/EColi_Normal.png",
                    "Infección de vías urinarias: ardor al orinar y ganas de ir a cada rato. Si sube al riñón (pielonefritis) da fiebre y dolor de espalda.",
                    "Vive normalmente en el intestino y casi siempre es inofensiva. Es la causa más común de infección urinaria: llega a la uretra desde la piel de alrededor.",
                    "Antibióticos que receta el médico, tomando el tratamiento completo.",
                    "Tomar agua, no aguantarse las ganas de orinar y limpiarse de adelante hacia atrás.",
                    EColi),
                Ficha(C, "Riñón (secreto, jefe)", "Cálculo renal", "Litiasis renal", "Jefe · piedra de sales", $"{BossArt}/Calculo/Calculo_Normal.png",
                    "Piedras en el riñón: se forman con sales de la orina (casi siempre calcio). Si bajan por el uréter dan un dolor muy fuerte en la espalda.",
                    "Muchas salen solas tomando agua. Las grandes se rompen con ondas de choque desde afuera (litotricia) o se sacan con un tubito.",
                    "El médico quita el dolor y decide si hace falta romperla o sacarla.",
                    "Tomar suficiente AGUA todos los días y no comer demasiada sal.",
                    CalculoJefe),
                Ficha(C, "Riñón (secreto)", "Leptospira", "Leptospira interrogans", "Bacteria", $"{CircArt}/Leptospira_Normal.png",
                    "Leptospirosis: fiebre, dolor de músculos y ojos rojos. Si avanza puede dañar el riñón y el hígado.",
                    "Vive en el agua y el lodo contaminados con orina de ratas. Se contagia al nadar o caminar en charcos e inundaciones.",
                    "Antibióticos que receta el médico, lo antes posible.",
                    "No nadar ni jugar en agua de inundaciones o charcos sucios, usar botas y evitar las ratas.",
                    Leptospira),
                Ficha(C, "Riñón (secreto)", "Cercaria", "Schistosoma haematobium", "Parásito (gusano plano)", $"{CircArt}/Cercaria_Normal.png",
                    "Esquistosomiasis: sangre en la orina y daño en la vejiga. El gusano adulto vive en las venas de la vejiga.",
                    "Las cercarias son sus larvas: nadan en ríos y lagos de agua dulce de zonas tropicales y entran por la piel.",
                    "Un antiparasitario (praziquantel) que receta el médico.",
                    "No bañarse en ríos o lagos donde hay esquistosomiasis y tener baños limpios (que la orina no llegue al agua).",
                    Cercaria),
                Ficha(C, "Riñón (secreto)", "Proteus", "Proteus mirabilis", "Bacteria", $"{CircArt}/Proteus_Normal.png",
                    "Infección urinaria que puede formar CÁLCULOS de estruvita (cristales de la orina).",
                    "Su enzima (ureasa) hace la orina menos ácida y así se forman cristales. Puede pegarse en una biopelícula difícil de quitar.",
                    "Antibióticos que receta el médico; a veces también hay que quitar la piedra.",
                    "Tomar agua, no aguantarse las ganas de orinar y atender rápido las infecciones urinarias.",
                    Proteus),
                Ficha(C, "Riñón (secreto, minijefe)", "Colonia de Proteus", "Biopelícula de Proteus mirabilis", "Jefe · biopelícula", $"{BossArt}/ProteusColonia/ProteusColonia_Normal.png",
                    "Una biopelícula: muchas bacterias pegadas en una capa de baba que las protege de los medicamentos.",
                    "Las biopelículas se forman en sondas, piedras y dientes (¡la placa dental también es una!). Por eso esas infecciones cuestan más.",
                    "Antibióticos por más tiempo y, si hay una sonda o una piedra, cambiarla o quitarla.",
                    "Higiene, tomar agua y que las sondas se usen solo el tiempo necesario.",
                    ProteusColonia),
            };
            foreach (BestiaryEntry e in list)
            {
                e.boss = e.kind.StartsWith("Jefe") || (e.enemies.Length > 0 && e.enemies[0] != null && e.enemies[0].name.StartsWith("Jefe_"));
                e.ally = e.kind == "Aliada";
            }
            Bestiary.entries = list;
            AssignBestiaryPortraits(false);   // SceneSetup vuelve a correr DataSetup: sin esto se perdían los dibujos
            EditorUtility.SetDirty(Bestiary);
        }

        /// <summary>Pone el dibujo de cada ficha (después de PrefabSetup, que ya importó los PNG como sprites).</summary>
        public void AssignBestiaryPortraits(bool allowImport = true)
        {
            if (Bestiary == null) return;
            var missing = new List<string>();
            foreach (BestiaryEntry e in Bestiary.entries)
            {
                if (string.IsNullOrEmpty(e.portraitPath)) continue;
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(e.portraitPath);
                if (s == null && allowImport && AssetImporter.GetAtPath(e.portraitPath) is TextureImporter importer)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                    s = AssetDatabase.LoadAssetAtPath<Sprite>(e.portraitPath);
                }
                if (s != null) e.portrait = s;
                else if (allowImport)
                {
                    if (System.IO.File.Exists(e.portraitPath)) Debug.LogWarning($"[Bestiario] No se pudo cargar el dibujo de '{e.name}': {e.portraitPath}");
                    else missing.Add(e.name);
                }
            }
            if (missing.Count > 0)
                Debug.Log($"[Bestiario] {missing.Count} fichas todavía sin dibujo (se ven con silueta): {string.Join(", ", missing)}");
            EditorUtility.SetDirty(Bestiary);
        }
    }
}
