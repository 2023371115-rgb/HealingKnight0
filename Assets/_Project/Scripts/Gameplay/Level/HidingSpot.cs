using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// ESCONDITE (Ojo): si estás dentro de su rectángulo y AGACHADO, nadie te ve. Las "moscas volantes"
    /// (manchitas que flotan en el humor vítreo) son escondites que se mueven y te tapan aunque no te agaches.
    /// </summary>
    public class HidingSpot : MonoBehaviour
    {
        private static readonly List<HidingSpot> all = new List<HidingSpot>();

        [SerializeField] private float width = 2f;
        [SerializeField] private float height = 1.6f;
        [SerializeField] private bool fullCover;          // tapa aunque no te agaches (moscas volantes)
        [SerializeField] private Vector2 drift;           // amplitud del movimiento (moscas volantes)
        [SerializeField] private float period = 6f;

        private Vector2 home;
        private float seed;

        public void Configure(float w, float h, bool full, Vector2 move, float cycle)
        {
            width = w; height = h; fullCover = full; drift = move; period = cycle;
        }

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        public static bool Covers(PlayerController p)
        {
            foreach (HidingSpot h in all)
            {
                Vector2 c = h.transform.position;
                Vector2 f = p.Position;
                if (Mathf.Abs(f.x - c.x) > h.width * 0.5f || f.y < c.y - 0.3f || f.y > c.y + h.height) continue;
                if (h.fullCover || p.IsCrouching) return true;
            }
            return false;
        }

        private void Start()
        {
            home = transform.position;
            seed = Random.value * 10f;
            Sprite c = CodeSprites.Circle;
            if (fullCover)
            {
                // Mosca volante: mancha translúcida con hilitos.
                CodeSprites.Add(transform, c, new Color(0.25f, 0.3f, 0.45f, 0.55f), 20, new Vector2(0f, height * 0.5f), new Vector2(width * 1.1f, height * 1.1f));
                CodeSprites.Add(transform, c, new Color(0.4f, 0.45f, 0.6f, 0.45f), 21, new Vector2(-width * 0.15f, height * 0.6f), new Vector2(width * 0.6f, height * 0.6f));
                CodeSprites.Add(transform, CodeSprites.Square, new Color(0.3f, 0.35f, 0.5f, 0.5f), 20, new Vector2(width * 0.5f, height * 0.3f), new Vector2(width * 0.6f, 0.08f));
            }
            else
            {
                // Pliegue de tejido (arbusto): te escondes agachado detrás.
                for (int i = 0; i < 3; i++)
                {
                    float x = (i - 1) * width * 0.3f;
                    CodeSprites.Add(transform, c, new Color(0.35f, 0.12f, 0.2f), 18, new Vector2(x, height * 0.35f), new Vector2(width * 0.5f, height * 0.9f));
                    CodeSprites.Add(transform, c, new Color(0.85f, 0.45f, 0.55f), 19, new Vector2(x, height * 0.38f), new Vector2(width * 0.42f, height * 0.8f));
                }
            }
        }

        private void Update()
        {
            if (drift == Vector2.zero) return;
            float t = (Time.time + seed) / period * Mathf.PI * 2f;
            transform.position = home + new Vector2(Mathf.Sin(t) * drift.x, Mathf.Sin(t * 1.7f) * drift.y);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * height * 0.5f, new Vector3(width, height, 0f));
        }
    }
}
