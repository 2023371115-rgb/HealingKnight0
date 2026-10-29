using System.Collections.Generic;
using HealingKnight.Core.Combat2D;
using HealingKnight.Core.Damage;
using UnityEngine;

namespace HealingKnight.Gameplay.Combat
{
    /// <summary>
    /// Zona donde se puede golpear a un personaje (rectángulo relativo a su posición, que son los pies).
    /// Se registra en una lista global para que disparos, cuchillazos y explosiones encuentren objetivos
    /// sin depender de capas de física. Debe estar junto al componente que implementa IDamageable.
    /// </summary>
    public class Hurtbox : MonoBehaviour
    {
        private static readonly List<Hurtbox> all = new List<Hurtbox>();
        public static IReadOnlyList<Hurtbox> All => all;

        [Tooltip("Centro del rectángulo respecto a los pies.")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 0.9f);
        [SerializeField] private Vector2 size = new Vector2(0.8f, 1.8f);

        private IDamageable damageable;
        private Vector2 baseOffset;
        private Vector2 baseSize;

        public IDamageable Damageable => damageable;
        public Team Team => damageable != null ? damageable.Team : Team.Neutral;
        public bool IsAlive => damageable != null && damageable.IsAlive;
        public Rect WorldRect => HitMath.FromCenter((Vector2)transform.position + offset, size);
        public Vector2 Center => (Vector2)transform.position + offset;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => all.Clear();

        private void Awake()
        {
            damageable = GetComponent<IDamageable>();
            baseOffset = offset;
            baseSize = size;
            if (damageable == null)
                Debug.LogError($"[Hurtbox] {name} no tiene un componente IDamageable.", this);
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        /// <summary>Encogerse: el cuerpo se hace más bajo (los disparos altos pasan por encima).</summary>
        public void SetHeightFactor(float factor)
        {
            size = new Vector2(baseSize.x, baseSize.y * factor);
            offset = new Vector2(baseOffset.x, size.y * 0.5f + (baseOffset.y - baseSize.y * 0.5f));
        }

        /// <summary>Para enemigos que se mueven por script (jefe): coloca el rectángulo en otra parte.</summary>
        public void SetOffset(Vector2 newOffset) => offset = newOffset;

        public bool OverlapsCircle(Vector2 center, float radius) => HitMath.CircleOverlapsRect(center, radius, WorldRect);
        public bool OverlapsRect(Rect rect) => HitMath.RectsOverlap(rect, WorldRect);

        public static void QueryCircle(Team targetTeam, Vector2 center, float radius, List<Hurtbox> results)
        {
            results.Clear();
            for (int i = 0; i < all.Count; i++)
            {
                Hurtbox h = all[i];
                if (h != null && h.Team == targetTeam && h.IsAlive && h.OverlapsCircle(center, radius)) results.Add(h);
            }
        }

        public static void QueryRect(Team targetTeam, Rect rect, List<Hurtbox> results)
        {
            results.Clear();
            for (int i = 0; i < all.Count; i++)
            {
                Hurtbox h = all[i];
                if (h != null && h.Team == targetTeam && h.IsAlive && h.OverlapsRect(rect)) results.Add(h);
            }
        }

        public static Hurtbox FirstInCircle(Team targetTeam, Vector2 center, float radius)
        {
            for (int i = 0; i < all.Count; i++)
            {
                Hurtbox h = all[i];
                if (h != null && h.Team == targetTeam && h.IsAlive && h.OverlapsCircle(center, radius)) return h;
            }
            return null;
        }

        private void OnDrawGizmosSelected()
        {
            Rect r = WorldRect;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(r.center, new Vector3(r.width, r.height, 0f));
        }
    }
}
