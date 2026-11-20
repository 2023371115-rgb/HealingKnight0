using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>Lugar donde el aire no te avienta (te agarras de los pelos nasales o te cubres tras un anillo de cartílago).</summary>
    public class WindAnchor : MonoBehaviour
    {
        private static readonly List<WindAnchor> all = new List<WindAnchor>();
        [SerializeField] private float halfWidth = 0.9f;
        [SerializeField] private float height = 3.5f;

        public void Configure(float width, float h) { halfWidth = width * 0.5f; height = h; }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        public static bool IsSheltered(Vector2 feet)
        {
            foreach (WindAnchor a in all)
            {
                Vector2 p = a.transform.position;
                if (Mathf.Abs(feet.x - p.x) <= a.halfWidth && feet.y >= p.y - 0.3f && feet.y <= p.y + a.height) return true;
            }
            return false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.6f, 1f, 0.6f, 0.6f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * height * 0.5f, new Vector3(halfWidth * 2f, height, 0f));
        }
    }
}
