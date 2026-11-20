using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// REMOLINO (várices): la sangre estancada da vueltas y te arrastra en círculo (y un poco hacia el centro).
    /// </summary>
    public class FlowSwirl : MonoBehaviour
    {
        [SerializeField] private float radius = 3f;
        [SerializeField] private float strength = 5f;
        [SerializeField] private bool clockwise = true;

        private Transform[] arms;
        private float spin;

        public void Configure(float swirlRadius, float force, bool turnClockwise)
        {
            radius = swirlRadius;
            strength = force;
            clockwise = turnClockwise;
        }

        private void Start()
        {
            arms = new Transform[10];
            for (int i = 0; i < arms.Length; i++)
            {
                SpriteRenderer r = CodeSprites.Add(transform, CodeSprites.Square, new Color(0.7f, 0.15f, 0.25f, 0.45f), -6, Vector2.zero,
                    new Vector2(radius * (0.35f + 0.06f * i), 0.07f), "Remolino");
                arms[i] = r.transform;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float dir = clockwise ? -1f : 1f;
            spin += dt * 140f * dir;
            if (arms != null)
                for (int i = 0; i < arms.Length; i++)
                {
                    float a = (spin + i * 36f) * Mathf.Deg2Rad;
                    float r = radius * (0.25f + 0.07f * i);
                    arms[i].localPosition = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                    arms[i].localRotation = Quaternion.Euler(0f, 0f, (spin + i * 36f) + 90f);
                }

            PlayerController player = PlayerController.Current;
            if (player == null || player.Flight == null || !player.Health.IsAlive) return;
            Vector2 d = (player.Position + new Vector2(0f, 0.6f)) - (Vector2)transform.position;
            float dist = d.magnitude;
            if (dist > radius || dist < 0.01f) return;
            Vector2 tangent = new Vector2(-d.y, d.x) / dist * dir;
            float k = 1f - dist / radius;
            player.Flight.AddPush((tangent * strength - d / dist * strength * 0.3f) * (0.4f + k));
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.8f, 0.3f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
