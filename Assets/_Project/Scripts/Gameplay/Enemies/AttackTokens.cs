using System.Collections.Generic;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// Limita cuántos enemigos atacan al mismo tiempo. Los demás esperan a cierta distancia.
    /// Es el truco clásico de los beat 'em up para que pelear contra un grupo se sienta justo.
    /// </summary>
    public static class AttackTokens
    {
        public static int MaxConcurrentAttackers = 2;
        private static readonly HashSet<object> holders = new HashSet<object>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            holders.Clear();
            MaxConcurrentAttackers = 2;
        }

        public static bool Has(object owner) => holders.Contains(owner);

        public static bool TryAcquire(object owner)
        {
            if (holders.Contains(owner)) return true;
            if (holders.Count >= MaxConcurrentAttackers) return false;
            holders.Add(owner);
            return true;
        }

        public static void Release(object owner) => holders.Remove(owner);
    }
}
