using UnityEngine;

namespace HealingKnight.Gameplay.Movement
{
    /// <summary>
    /// Límites horizontales del nivel y, cuando hay una arena activa, de la arena
    /// (la pantalla se "cierra" y no puedes avanzar hasta derrotar las oleadas, como en Metal Slug).
    /// También guarda la altura mínima: caer por debajo cuenta como caída al vacío.
    /// </summary>
    public static class WalkableArea
    {
        private static Rect levelBounds = new Rect(-1000f, -50f, 2000f, 100f);
        private static bool hasLock;
        private static float lockMinX;
        private static float lockMaxX;

        public static Rect LevelBounds => levelBounds;
        public static bool IsLocked => hasLock;

        public static float MinX => hasLock ? Mathf.Max(lockMinX, levelBounds.xMin) : levelBounds.xMin;
        public static float MaxX => hasLock ? Mathf.Min(lockMaxX, levelBounds.xMax) : levelBounds.xMax;

        /// <summary>Por debajo de esta Y se considera caída al vacío.</summary>
        public static float KillY => levelBounds.yMin;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            levelBounds = new Rect(-1000f, -50f, 2000f, 100f);
            hasLock = false;
        }

        public static void SetLevelBounds(Rect bounds)
        {
            levelBounds = bounds;
            hasLock = false;
        }

        public static void Lock(float minX, float maxX)
        {
            hasLock = true;
            lockMinX = minX;
            lockMaxX = maxX;
        }

        public static void Unlock()
        {
            hasLock = false;
        }
    }
}
