using System.Collections.Generic;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>El antígeno en el piso o dando vueltas alrededor del jugador.</summary>
    public class Antigen : MonoBehaviour
    {
        private Transform visual;
        private float vy, cooldown, life = 40f;
        private bool grounded;

        private void Start()
        {
            visual = new GameObject("Dibujo").transform;
            visual.SetParent(transform, false);
            Sprite c = CodeSprites.Circle, sq = CodeSprites.Square;
            CodeSprites.Add(visual, c, new Color(1f, 0.9f, 0.4f, 0.35f), 25, Vector2.zero, new Vector2(1.2f, 1.2f));
            CodeSprites.Add(visual, c, new Color(0.3f, 0.08f, 0.15f), 26, Vector2.zero, new Vector2(0.66f, 0.66f));
            CodeSprites.Add(visual, c, new Color(1f, 0.55f, 0.35f), 27, Vector2.zero, new Vector2(0.54f, 0.54f));
            // "Picos" del antígeno (lo que reconocen los anticuerpos).
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f;
                SpriteRenderer spike = CodeSprites.Add(visual, sq, new Color(1f, 0.85f, 0.3f), 26, Vector2.zero, new Vector2(0.12f, 0.32f));
                spike.transform.localRotation = Quaternion.Euler(0f, 0f, a);
                spike.transform.localPosition = Quaternion.Euler(0f, 0f, a) * new Vector3(0f, 0.36f, 0f);
            }
        }

        public void Drop()
        {
            grounded = false;
            vy = 3f;
            cooldown = 0.8f;
            life = 25f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            AntigenSystem sys = AntigenSystem.Current;
            PlayerController p = PlayerController.Current;
            if (visual != null) visual.Rotate(0f, 0f, 60f * dt);
            if (sys != null && p != null && sys.IsCarried(this))
            {
                // Dan vueltas alrededor del jugador, a la altura del pecho (se ven, pero no estorban).
                int i = sys.IndexOf(this);
                float a = Time.time * 2.2f + i * Mathf.PI * 2f / Mathf.Max(1, sys.MaxCarried);
                Vector2 target = p.Position + new Vector2(Mathf.Cos(a) * 1f, 1.2f + Mathf.Sin(a) * 0.45f);
                transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-12f * dt));
                transform.localScale = Vector3.one * 0.7f;
                return;
            }
            transform.localScale = Vector3.one;

            if (!grounded)
            {
                vy -= 14f * dt;
                Vector3 pos = transform.position + Vector3.up * vy * dt;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, Mathf.Max(0.05f, -vy * dt) + 0.35f, 1 << Movement.PhysicsLayers.Ground);
                if (vy < 0f && hit.collider != null) { pos.y = hit.point.y + 0.35f; grounded = true; }
                transform.position = pos;
            }
            life -= dt;
            if (life <= 0f) { Destroy(gameObject); return; }
            if (cooldown > 0f) { cooldown -= dt; return; }
            if (p != null && p.Health.IsAlive && sys != null && Vector2.Distance(p.Position + Vector2.up * 0.9f, transform.position) < 1.1f)
                sys.TryCarry(this);
        }
    }
}
