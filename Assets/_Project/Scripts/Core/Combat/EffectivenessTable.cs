using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealingKnight.Core.Combat
{
    /// <summary>
    /// Tabla de efectividad amenaza × medicamento (GDD 5.7).
    /// Cualquier combinación que no esté en la lista cuenta como Ineficaz.
    /// Se edita desde el Inspector: no hay que tocar código para balancear.
    /// </summary>
    [CreateAssetMenu(menuName = "Healing Knight/Tabla de efectividad", fileName = "EffectivenessTable")]
    public class EffectivenessTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public ThreatType threat;
            public MedicationData medication;
            public Effectiveness result = Effectiveness.Effective;
        }

        [Header("Multiplicadores de daño")]
        [Range(0f, 2f)] public float effectiveMultiplier = 1f;
        [Range(0f, 1f)] public float partialMultiplier = 0.25f;
        [Range(0f, 1f)] public float ineffectiveMultiplier = 0f;

        [Header("Combinaciones (lo que no aparece es Ineficaz)")]
        public List<Entry> entries = new List<Entry>();

        public Effectiveness Evaluate(ThreatType threat, MedicationData medication)
        {
            if (medication == null) return Effectiveness.Ineffective;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e != null && e.threat == threat && e.medication == medication)
                    return e.result;
            }

            return Effectiveness.Ineffective;
        }

        public float GetMultiplier(Effectiveness effectiveness)
        {
            switch (effectiveness)
            {
                case Effectiveness.Effective: return effectiveMultiplier;
                case Effectiveness.Partial: return partialMultiplier;
                default: return ineffectiveMultiplier;
            }
        }

        public float GetMultiplier(ThreatType threat, MedicationData medication)
        {
            return GetMultiplier(Evaluate(threat, medication));
        }

        /// <summary>Agrega o reemplaza una combinación. Lo usan la herramienta de setup y las pruebas.</summary>
        public void Set(ThreatType threat, MedicationData medication, Effectiveness result)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].threat == threat && entries[i].medication == medication)
                {
                    entries[i].result = result;
                    return;
                }
            }

            entries.Add(new Entry { threat = threat, medication = medication, result = result });
        }
    }
}
