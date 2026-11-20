using System.Collections.Generic;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>Algo que detiene la pantalla del vuelo mientras exista (un coágulo que tapa todo, un minijefe).</summary>
    public interface IFlightBlocker
    {
        /// <summary>La X donde está (su orilla izquierda): la pantalla no la deja pasar del centro + un poco.</summary>
        float BlockX { get; }
        /// <summary>Si ahorita está deteniendo la pantalla.</summary>
        bool Blocking { get; }
        /// <summary>0 = siempre; 1/2 = solo en esa rama de la bifurcación.</summary>
        int Branch { get; }
        /// <summary>Si la nave ya va más adelante que esto, ya no detiene la pantalla (un jefe: float.MaxValue).</summary>
        float PassX { get; }
    }

    /// <summary>La lista de lo que detiene la pantalla (la revisa FlightRoute cada cuadro).</summary>
    public static class FlightBlockers
    {
        private static readonly List<IFlightBlocker> all = new List<IFlightBlocker>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => all.Clear();

        public static void Add(IFlightBlocker b) { if (b != null && !all.Contains(b)) all.Add(b); }
        public static void Remove(IFlightBlocker b) => all.Remove(b);

        /// <summary>
        /// ¿Algo detiene la pantalla en 'stopX'? Solo cuenta lo que la nave no ha pasado (PassX) y de la rama en la que
        /// vas.
        /// </summary>
        public static bool Blocks(float stopX, float shipX, int routeBranch)
        {
            for (int i = all.Count - 1; i >= 0; i--)
            {
                IFlightBlocker b = all[i];
                Object unityObject = b as Object;
                if (b == null || (!ReferenceEquals(unityObject, null) && unityObject == null)) { all.RemoveAt(i); continue; }
                if (!b.Blocking || b.BlockX > stopX || shipX > b.PassX) continue;
                if (b.Branch != 0 && b.Branch != routeBranch) continue;
                return true;
            }
            return false;
        }
    }
}
