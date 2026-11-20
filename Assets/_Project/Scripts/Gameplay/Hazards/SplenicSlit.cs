using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// CORDÓN DEL BAZO (ranura): una pared de células muy juntas que solo deja pasar a las células flexibles. Se abre
    /// un momentito cada 'period' segundos (antes brilla): cruza con DASH justo cuando se abra. Si se cierra contigo
    /// adentro, te escupe para atrás.
    /// Biología: los glóbulos rojos viejos y tiesos no caben por las ranuras y ahí los atrapan los macrófagos.
    /// </summary>
    public class SplenicSlit : MonoBehaviour
    {
        [SerializeField] private float height = 9f;
        [SerializeField] private float thickness = 2.2f;
        [SerializeField] private float period = 2.4f;
        [SerializeField] private float openTime = 0.5f;
        [SerializeField] private float phase;
        [SerializeField] private float damage = 8f;

        private BoxCollider2D wall;
        private Transform left, right, glow;
        private SpriteRenderer glowRenderer;
        private float t;
        private bool wasOpen, told;

        public void Configure(float wallHeight, float wallThickness, float cycle, float window, float startPhase)
        {
            height = wallHeight; thickness = wallThickness; period = cycle; openTime = window; phase = startPhase;
        }

        private void Start()
        {
            gameObject.layer = PhysicsLayers.Ground;
            wall = gameObject.AddComponent<BoxCollider2D>();
            wall.size = new Vector2(thickness, height);
            wall.offset = new Vector2(0f, height * 0.5f);
            left = Half("Izquierda", -1f);
            right = Half("Derecha", 1f);
            glow = CodeSprites.Add(transform, CodeSprites.Glow, new Color(1f, 0.9f, 0.6f, 0f), 2, new Vector2(0f, height * 0.5f), new Vector2(1.4f, height), "Brillo").transform;
            glowRenderer = glow.GetComponent<SpriteRenderer>();
            t = phase;
        }

        /// <summary>Media pared: una columna de células (círculos rojo oscuro) apiladas.</summary>
        private Transform Half(string name, float side)
        {
            var h = new GameObject(name).transform;
            h.SetParent(transform, false);
            float w = thickness * 0.5f;
            CodeSprites.Add(h, CodeSprites.Square, new Color(0.38f, 0.06f, 0.12f), 0, new Vector2(side * w * 0.5f, height * 0.5f), new Vector2(w, height), "Relleno");
            for (float y = 0.4f; y < height; y += 0.8f)
            {
                CodeSprites.Add(h, CodeSprites.Circle, new Color(0.25f, 0.03f, 0.08f), 1, new Vector2(side * w * 0.5f, y), new Vector2(w * 1.05f, 0.85f), "Contorno");
                CodeSprites.Add(h, CodeSprites.Circle, new Color(0.7f, 0.2f, 0.3f), 1, new Vector2(side * w * 0.5f, y), new Vector2(w * 0.9f, 0.7f), "Celula");
            }
            return h;
        }

        private void Update()
        {
            if (wall == null) return;
            float dt = Time.deltaTime;
            t += dt;
            float k = Mathf.Repeat(t, period);
            bool open = k < openTime;
            // Brilla medio segundo antes de abrirse (aviso del ritmo).
            float warn = Mathf.Clamp01(1f - (period - k) / 0.6f);
            if (glowRenderer != null) glowRenderer.color = new Color(1f, 0.9f, 0.6f, open ? 0.8f : warn * 0.5f);
            float gap = open ? 0.45f : 0f;
            if (left != null) left.localPosition = Vector3.Lerp(left.localPosition, new Vector3(-gap, 0f, 0f), 1f - Mathf.Exp(-25f * dt));
            if (right != null) right.localPosition = Vector3.Lerp(right.localPosition, new Vector3(gap, 0f, 0f), 1f - Mathf.Exp(-25f * dt));
            wall.enabled = !open;

            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            float dx = player.Position.x - transform.position.x;
            if (!told && Mathf.Abs(dx) < 7f)
            {
                told = true;
                GameplayEvents.RaiseMessage("CORDONES del bazo: ranuras por donde solo pasan las células flexibles. Cuando BRILLE y se abra, ¡cruza con DASH ({dash})!");
            }
            // Se cerró contigo adentro: te escupe hacia atrás.
            if (wasOpen && !open && Mathf.Abs(dx) < thickness * 0.5f + 0.3f && player.Position.y < transform.position.y + height)
            {
                DamageInfo info = DamageInfo.Create(damage, DamageKind.Environment, Team.Neutral, transform.position);
                info.knockback = 0f;
                player.Health.ApplyDamage(info);
                Vector2 back = new Vector2(transform.position.x - thickness * 0.5f - 0.8f, player.Position.y);
                player.Motor.Teleport(back);
            }
            wasOpen = open;
        }
    }
}
