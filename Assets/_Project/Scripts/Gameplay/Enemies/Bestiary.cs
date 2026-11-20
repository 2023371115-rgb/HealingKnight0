using System;
using System.Collections.Generic;
using HealingKnight.Core.Combat;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// BESTIARIO (tipo Pokédex): una ficha por cada patógeno del juego con datos REALES (enfermedad, curiosidades,
    /// tratamiento de verdad, prevención) y qué medicamento le gana en el juego. Vive en Resources/Bestiario.
    /// Lo llena DataSetup (textos) y ProjectSetup (dibujos). La ficha se desbloquea al ver al patógeno.
    /// </summary>
    [CreateAssetMenu(menuName = "Healing Knight/Bestiario", fileName = "Bestiario")]
    public class Bestiary : ScriptableObject
    {
        public const string ResourcePath = "Bestiario";

        public List<BestiaryEntry> entries = new List<BestiaryEntry>();
        public List<MedicationData> medications = new List<MedicationData>();
        public EffectivenessTable table;

        private Dictionary<EnemyData, BestiaryEntry> byEnemy;
        private static Bestiary cached;

        public static Bestiary Load()
        {
            if (cached == null) cached = Resources.Load<Bestiary>(ResourcePath);
            return cached;
        }

        /// <summary>La ficha a la que pertenece un enemigo (incluye variantes: segmentos, jefes, crías...).</summary>
        public BestiaryEntry Find(EnemyData enemy)
        {
            if (enemy == null) return null;
            if (byEnemy == null)
            {
                byEnemy = new Dictionary<EnemyData, BestiaryEntry>();
                foreach (BestiaryEntry e in entries)
                    if (e != null && e.enemies != null)
                        foreach (EnemyData d in e.enemies)
                            if (d != null && !byEnemy.ContainsKey(d)) byEnemy.Add(d, e);
            }
            return byEnemy.TryGetValue(enemy, out BestiaryEntry entry) ? entry : null;
        }

        /// <summary>Qué tan bien le va cada medicamento del juego a esta ficha (según su enemigo principal).</summary>
        public Effectiveness Evaluate(BestiaryEntry entry, MedicationData medication)
        {
            if (entry == null || entry.enemies == null || entry.enemies.Length == 0 || entry.enemies[0] == null) return Effectiveness.Ineffective;
            return entry.enemies[0].Evaluate(table, medication);
        }
    }

    [Serializable]
    public class BestiaryEntry
    {
        public string id;                 // = nombre del EnemyData principal
        public string name;               // nombre común
        public string scientific;         // nombre científico
        public string kind;               // Bacteria, Virus, Parásito, Alérgeno, Aliada, Complicación...
        public string system;             // Digestivo, Respiratorio, Nervioso
        public string foundIn;            // dónde sale en el juego
        public bool boss;
        public bool ally;
        public EnemyData[] enemies;       // el principal primero; luego variantes
        [TextArea(2, 6)] public string disease;
        [TextArea(3, 8)] public string facts;
        [TextArea(2, 6)] public string treatment;
        [TextArea(2, 6)] public string prevention;
        [TextArea(2, 6)] public string gameTip;   // si está vacío se usa la "biología" del enemigo
        public Sprite portrait;
        public string portraitPath;       // de dónde sale el dibujo (lo asigna el editor)
    }
}
