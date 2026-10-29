using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Progression;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.EditorTools
{
    /// <summary>Crea los ScriptableObjects base con los valores del GDD. Si ya existen, no los toca.</summary>
    internal class DataSetup
    {
        public const string DataFolder = "Assets/_Project/Data";

        public MedicationData Antibiotico, Antiviral, Antiinflamatorio, Antihistaminico, Antiparasitario, Mucolitico, Anticonvulsivo;
        public EffectivenessTable Table;
        public PlayerConfig PlayerConfig;
        public EnemyData HPylori, Giardia, Rotavirus, Entamoeba, CDifficile, Oxiuro, SMutans, Lombriz;
        public EnemyData Rinovirus, Polen, SAureus, Pertussis, Neumococo, Influenza, Tuberculosis;
        public EnemyData Mucosidad, Inflamacion, Colmena;
        public EnemyData SAureusCoco, OxiuroHuevo;
        public EnemyData Poliovirus, Meningococo, Herpes, Rabia, NeuronaHiperexcitada, RabiaJefe, RabiaVirion;
        public GameCatalog Catalog;
        public LevelData DigestivoEstomago, DigestivoDelgado, DigestivoGrueso, DigestivoBoca;
        public LevelData RespiratorioNariz, RespiratorioTraquea, RespiratorioPulmones;
        public LevelData NerviosoMedula, NerviosoCerebro, NerviosoHipocampo;

        public void Run()
        {
            CreateMedications();
            CreateAudio();
            CreateEffectivenessTable();
            CreatePlayerConfig();
            CreateEnemies();
            CreateRespiratoryEnemies();
            CreateNervousEnemies();
            CreateOffspring();
            CreateCatalog();
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
                // Altura del hombro del caballero (Art/Player): 1.15 de pie; agachado el cuerpo se aplasta al 55 %.
                c.muzzleHeight = 1.15f;
                c.crouchMuzzleHeight = 0.65f;
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
                e.tint = new Color(0.5f, 0.85f, 0.4f);
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

            // Jefe de los Pulmones: colmena voladora de tuberculosis (estilo nave de Metal Slug).
            Colmena = Enemy("Jefe_Colmena", e =>
            {
                e.displayName = "Colmena de tuberculosis";
                e.threat = ThreatType.Bacteria;
                e.movement = EnemyMovement.Flying; e.flyHeight = 4.5f;
                e.maxHealth = 420f;
                e.meleeDamageTakenMultiplier = 0.5f;
                e.knockbackResistance = 1f; e.hurtStunTime = 0f; e.linfocitoCharge = 6;
                e.tint = new Color(0.82f, 0.68f, 0.45f);
            });
        }

        private void CreateNervousEnemies()
        {
            // Sistema Nervioso (GDD: virus y bacterias entre neuronas sanas). Patógenos reales del sistema nervioso.
            Poliovirus = Enemy("Virus_Poliovirus", e =>
            {
                e.displayName = "Poliovirus";
                e.threat = ThreatType.Virus;
                e.behavior = EnemyBehavior.Paralyzer;
                e.biologia = "Ataca las neuronas motoras de la médula espinal y causa parálisis. Sus disparos te dejan lento un momento.";
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
                e.projectileColor = new Color(1f, 0.55f, 0.9f);
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
                "Elimina Giardia y rotavirus. La lombriz aparece... y huye.");
            DigestivoGrueso = Level("Digestivo", "03_IntestinoGrueso", "digestivo_grueso", "Intestino grueso", "Digestivo_IntestinoGrueso", true,
                "Amebas, C. difficile y oxiuros. Acaba con la lombriz.");
            DigestivoBoca = Level("Digestivo", "99_Boca_Secreto", "digestivo_boca", "Boca", "Digestivo_Boca", false,
                "Nivel secreto: caries y placa bacteriana.");
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

            SystemData respiratorio = MakeSystem("Respiratorio", "respiratorio", "Sistema Respiratorio", "II", Mucolitico,
                new Color(0.45f, 0.75f, 1f), new Vector2(0.5f, 0.6f),
                "La mucosidad atrapa a los patógenos y los protege del medicamento.",
                RespiratorioNariz, RespiratorioTraquea, RespiratorioPulmones);

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

            SystemData nervioso = MakeSystem("Nervioso", "nervioso", "Sistema Nervioso", "III", Anticonvulsivo,
                new Color(0.7f, 0.5f, 1f), new Vector2(0.5f, 0.9f),
                "Tormentas eléctricas y tejido sano que hay que proteger.",
                NerviosoMedula, NerviosoCerebro, NerviosoHipocampo);
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

            SystemData circulatorio = MakeSystem("Circulatorio", "circulatorio", "Sistema Circulatorio", "IV", null,
                new Color(0.9f, 0.2f, 0.25f), new Vector2(0.45f, 0.65f),
                "La Cepa Madre bombea variantes resistentes a todo el cuerpo.",
                Level("Circulatorio", "01_Venas", "circulatorio_venas", "Venas y arterias", "", false, ""),
                Level("Circulatorio", "02_Corazon", "circulatorio_corazon", "Corazón", "", true, ""));

            Catalog = SetupUtils.LoadOrCreate<GameCatalog>($"{DataFolder}/Resources/{GameCatalog.ResourcePath}.asset", c =>
            {
                c.systems = new List<SystemData> { digestivo, respiratorio, nervioso, circulatorio };
            });
        }
    }
}
