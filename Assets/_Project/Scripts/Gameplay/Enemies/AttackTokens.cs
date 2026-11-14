using System.Collections.Generic;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// Limita cuántos enemigos atacan al mismo tiempo. Los demás esperan a cierta distancia (moviéndose).
    /// Es el truco clásico de los beat 'em up para que pelear contra un grupo se sienta justo.
    /// Cada enemigo que tiene ficha la "renueva" cada cuadro mientras te persigue; si deja de hacerlo
    /// (se quedó atorado lejos, se murió, está fuera de pantalla), la ficha se libera sola para que otro ataque.
    /// </summary>
    public static class AttackTokens
    {
        public static int MaxConcurrentAttackers = 3;
        /// <summary>Segundos sin renovar para que una ficha se libere sola.</summary>
        public static float StaleTime = 1.5f;

        private static readonly Dictionary<object, float> holders = new Dictionary<object, float>();
        private static readonly List<object> stale = new List<object>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            holders.Clear();
            MaxConcurrentAttackers = 3;
            StaleTime = 1.5f;
        }

        public static bool Has(object owner) => holders.ContainsKey(owner);

        public static bool TryAcquire(object owner)
        {
            float now = Time.time;
            if (holders.ContainsKey(owner))
            {
                holders[owner] = now;
                return true;
            }
            if (holders.Count >= MaxConcurrentAttackers) Prune(now);
            if (holders.Count >= MaxConcurrentAttackers) return false;
            holders[owner] = now;
            return true;
        }

        public static void Release(object owner) => holders.Remove(owner);

        private static void Prune(float now)
        {
            stale.Clear();
            foreach (KeyValuePair<object, float> h in holders)
            {
                bool gone = h.Key is Object unityObject && unityObject == null;   // destruido
                if (gone || now - h.Value > StaleTime) stale.Add(h.Key);
            }
            foreach (object o in stale) holders.Remove(o);
        }
    }
}
