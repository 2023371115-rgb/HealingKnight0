using System;
using System.Collections.Generic;

namespace HealingKnight.Core.Progression
{
    /// <summary>Lo que se guarda en disco. Se serializa con JsonUtility.</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public bool prologueSeen;
        public List<string> completedLevels = new List<string>();
        public List<string> formulas = new List<string>();
        public List<string> secrets = new List<string>();

        // Reservado por si después se agregan estrellas o puntaje por nivel.
        public List<LevelRecord> records = new List<LevelRecord>();

        public bool IsLevelCompleted(string levelId)
        {
            return !string.IsNullOrEmpty(levelId) && completedLevels.Contains(levelId);
        }

        public void MarkLevelCompleted(string levelId)
        {
            if (!string.IsNullOrEmpty(levelId) && !completedLevels.Contains(levelId))
                completedLevels.Add(levelId);
        }

        public bool HasSecret(string secretId)
        {
            return !string.IsNullOrEmpty(secretId) && secrets != null && secrets.Contains(secretId);
        }

        /// <summary>Devuelve true si es la primera vez que se encuentra.</summary>
        public bool AddSecret(string secretId)
        {
            if (string.IsNullOrEmpty(secretId)) return false;
            if (secrets == null) secrets = new List<string>();
            if (secrets.Contains(secretId)) return false;
            secrets.Add(secretId);
            return true;
        }

        public void AddFormula(string systemId)
        {
            if (!string.IsNullOrEmpty(systemId) && !formulas.Contains(systemId))
                formulas.Add(systemId);
        }
    }

    [Serializable]
    public class LevelRecord
    {
        public string levelId;
        public float bestTime;
        public int stars;
    }
}
