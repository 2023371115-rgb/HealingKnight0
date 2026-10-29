using System.Collections.Generic;
using UnityEngine;

namespace HealingKnight.Core.Progression
{
    /// <summary>
    /// Lista ordenada de todos los sistemas del juego. Es la fuente de verdad del mapa del cuerpo.
    /// Vive en una carpeta Resources para poder cargarse desde cualquier escena.
    /// </summary>
    [CreateAssetMenu(menuName = "Healing Knight/Catálogo del juego", fileName = "GameCatalog")]
    public class GameCatalog : ScriptableObject
    {
        public const string ResourcePath = "GameCatalog";

        public List<SystemData> systems = new List<SystemData>();

        public SystemData FindSystemOf(LevelData level)
        {
            if (level == null) return null;
            foreach (SystemData system in systems)
            {
                if (system != null && system.levels.Contains(level)) return system;
            }
            return null;
        }

        public LevelData FindLevelByScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return null;
            foreach (SystemData system in systems)
            {
                if (system == null) continue;
                foreach (LevelData level in system.levels)
                {
                    if (level != null && level.sceneName == sceneName) return level;
                }
            }
            return null;
        }

        /// <summary>Nivel que sigue dentro del mismo sistema, o null si es el último.</summary>
        public LevelData NextLevelInSystem(LevelData level)
        {
            SystemData system = FindSystemOf(level);
            if (system == null) return null;
            int index = system.levels.IndexOf(level);
            return index >= 0 && index + 1 < system.levels.Count ? system.levels[index + 1] : null;
        }
    }
}
