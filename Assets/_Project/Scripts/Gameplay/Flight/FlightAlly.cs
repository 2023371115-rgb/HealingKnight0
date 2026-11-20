using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// LINFOCITO que escolta a la nave (sale de un glóbulo blanco que atrapaste): vuela pegado a ella en su lugar de la
    /// formación y le dispara al enemigo más cercano que tenga enfrente, con el medicamento que traes (más débil que
    /// tus cápsulas). No recibe daño.
    /// </summary>
    public class FlightAlly : MonoBehaviour
    {
        private const float FireEvery = 1.05f, Range = 12f, DamageScale = 0.6f;
        // Lugar de cada uno respecto a la nave (en la pantalla): arriba atrás, abajo atrás y más atrás.
        private static readonly Vector2[] Slots = { new Vector2(-1.2f, 1.7f), new Vector2(-1.2f, -0.9f), new Vector2(-2.5f, 0.4f) };

        private PlayerController player;
        private int slot;
        private float fireTimer, phase;
        private Transform visual;

        public void Begin(PlayerController owner, int index)
        {
            player = owner;
            slot = index;
            phase = index * 2.1f;
            fireTimer = 0.6f + index * 0.35f;
            visual = new GameObject("Dibujo").transform;
            visual.SetParent(transform, false);
            Sprite art = CodeSprites.ResourceArt("Aliados/LinfocitoB");   // Art/Aliados/Resources/Aliados/LinfocitoB.png
            if (art != null) CodeSprites.AddArt(visual, art, 0.95f, 24, Vector2.zero, "Linfocito");
            else
            {
                CodeSprites.Add(visual, CodeSprites.Circle, new Color(0.12f, 0.05f, 0.12f), 23, Vector2.zero, Vector2.one * 0.9f, "Contorno");
                CodeSprites.Add(visual, CodeSprites.Circle, new Color(0.7f, 0.75f, 1f), 24, Vector2.zero, Vector2.one * 0.78f, "Celula");
                CodeSprites.Add(visual, CodeSprites.Circle, new Color(0.35f, 0.2f, 0.6f), 25, new Vector2(-0.03f, -0.02f), Vector2.one * 0.56f, "Nucleo");
                CodeSprites.Add(visual, CodeSprites.Circle, Color.white, 26, new Vector2(0.14f, 0.1f), Vector2.one * 0.12f, "Ojo");
            }
            CodeSprites.Add(transform, CodeSprites.Glow, new Color(0.7f, 0.85f, 1f, 0.35f), 22, Vector2.zero, Vector2.one * 1.6f, "Luz");
            transform.position = Target();
        }

        private Vector2 Target()
        {
            Vector2 ship = player.Position + new Vector2(0f, 0.6f * player.Size);
            Vector2 offset = Slots[Mathf.Clamp(slot, 0, Slots.Length - 1)] + new Vector2(0f, Mathf.Sin(Time.time * 2.2f + phase) * 0.25f);
            return ship + offset;   // el mundo no gira (la cámara sí): +X siempre es "adelante"
        }

        private void Update()
        {
            if (player == null) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector2 target = Target();
            Vector2 p = transform.position;
            // Si la nave se fue lejos de golpe (reaparecer), llega de un salto.
            p = (target - p).sqrMagnitude > 64f ? target : Vector2.Lerp(p, target, 1f - Mathf.Exp(-7f * dt));
            transform.position = p;
            if (visual != null) visual.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 3f + phase) * 8f);

            if (!player.IsFlying || !player.Health.IsAlive || player.Shooter == null) return;
            fireTimer -= dt;
            if (fireTimer > 0f) return;
            Transform enemy = Nearest(p, Vector2.right);
            if (enemy == null) { fireTimer = 0.2f; return; }
            fireTimer = FireEvery + Random.value * 0.2f;
            Hurtbox box = enemy.GetComponent<Hurtbox>();
            Vector2 aim = (box != null ? box.Center : (Vector2)enemy.position) - p;
            player.Shooter.FireFrom(p + aim.normalized * 0.5f, aim, DamageScale);
        }

        private static Transform Nearest(Vector2 from, Vector2 forward)
        {
            Transform best = null;
            float bestD = Range * Range;
            foreach (EnemyHealth e in EnemyHealth.Active)
            {
                if (e == null || !e.IsAlive) continue;
                Vector2 to = (Vector2)e.transform.position - from;
                if (Vector2.Dot(to, forward) < -0.5f) continue;   // los de atrás no
                float d = to.sqrMagnitude;
                if (d < bestD) { bestD = d; best = e.transform; }
            }
            return best;
        }
    }
}
