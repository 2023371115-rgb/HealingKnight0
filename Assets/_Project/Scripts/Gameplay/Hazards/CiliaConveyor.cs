using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// ESCALERA DE CILIOS (tráquea): los cilios barren el moco hacia arriba, hacia la garganta, para sacarlo.
    /// En este tramo el piso es una banda que te empuja HACIA ATRÁS mientras estés parado en ella
    /// (saltar o ir por plataformas te libra). Por la banda ruedan bolas de moco hacia ti:
    /// sáltalas o disuélvelas con mucolítico (o antihistamínico). Si te pegan: daño, te frenan y te arrastran.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class CiliaConveyor : MonoBehaviour
    {
        [SerializeField] private float minX;
        [SerializeField] private float maxX = 20f;
        [SerializeField] private float floorY;
        [SerializeField] private float pushSpeed = 3.2f;
        [SerializeField] private int direction = -1;
        [SerializeField] private float ballInterval = 2.4f;
        [SerializeField] private float ballSpeed = 4.5f;
        [SerializeField] private float ballDamage = 6f;
        [SerializeField] private Sprite ballArt;
        [SerializeField] private Color cilia = new Color(0.82f, 0.78f, 1f, 0.9f);
        [SerializeField] private Color mucus = new Color(0.72f, 0.88f, 0.95f, 1f);
        [SerializeField] private Color outline = new Color(0.1f, 0.14f, 0.32f, 1f);

        private class Ball { public Transform root, art; public float x; public bool alive; }

        private readonly List<Ball> balls = new List<Ball>();
        private readonly List<Transform> hairs = new List<Transform>();
        private float timer;

        public void Configure(float from, float to, float groundY, float push, float period, Sprite ball)
        {
            minX = from;
            maxX = to;
            floorY = groundY;
            pushSpeed = push;
            ballInterval = period;
            ballArt = ball;
        }

        private void Start()
        {
            timer = 1f;
            // Fila de cilios que se mecen en ola (en el sentido en que empujan).
            for (float x = minX + 0.2f; x < maxX; x += 0.45f)
            {
                Transform h = new GameObject("Cilio").transform;
                h.SetParent(transform, false);
                h.position = new Vector3(x, floorY, 0f);
                SpriteRenderer r = CodeSprites.Add(h, CodeSprites.Square, cilia, -60, new Vector2(0f, 0.28f), new Vector2(0.08f, 0.56f));
                r.transform.localPosition = new Vector3(0f, 0.28f, 0f);
                CodeSprites.Add(h, CodeSprites.Circle, cilia, -60, new Vector2(0f, 0.56f), new Vector2(0.14f, 0.14f));
                hairs.Add(h);
            }
            // Película de moco encima de los cilios.
            CodeSprites.Add(transform, CodeSprites.Square, new Color(mucus.r, mucus.g, mucus.b, 0.25f), -59,
                new Vector2((minX + maxX) * 0.5f - transform.position.x, floorY - transform.position.y + 0.3f), new Vector2(maxX - minX, 0.22f));
        }

        private bool OnBelt(PlayerController p)
        {
            Vector2 f = p.Position;
            return f.x >= minX && f.x <= maxX && f.y < floorY + 0.25f && p.Motor.IsGrounded;
        }

        private void FixedUpdate()
        {
            PlayerController player = PlayerController.Current;
            if (player == null || player.Motor == null || !player.Health.IsAlive) return;
            if (OnBelt(player)) player.Motor.Body.position += new Vector2(direction * pushSpeed * Time.fixedDeltaTime, 0f);
        }

        private void Update()
        {
            float dt = Time.deltaTime, t = Time.time;
            for (int i = 0; i < hairs.Count; i++)
                hairs[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 7f + i * 0.6f * -direction) * 25f + direction * -10f);

            PlayerController player = PlayerController.Current;
            bool near = player != null && player.Position.x > minX - 10f && player.Position.x < maxX + 4f;

            timer -= dt;
            if (timer <= 0f && near)
            {
                timer = ballInterval * Random.Range(0.8f, 1.2f);
                Spawn();
            }

            DissolveWithMedicine();
            foreach (Ball b in balls)
            {
                if (!b.alive) continue;
                b.x += direction * ballSpeed * dt;
                b.root.position = new Vector3(b.x, floorY, 0f);
                b.art.Rotate(0f, 0f, -direction * -ballSpeed * 90f * dt);
                if (b.x < minX - 0.5f || b.x > maxX + 0.5f) { Kill(b); continue; }
                if (player != null && player.Health.IsAlive && Mathf.Abs(player.Position.x - b.x) < 0.75f && player.Position.y < floorY + 1.1f)
                {
                    DamageInfo info = DamageInfo.Create(ballDamage, DamageKind.Contact, Team.Enemy, b.root.position);
                    info.knockback = 4f;
                    player.Health.ApplyDamage(info);
                    player.ApplySlow(0.5f, 1.2f);
                    Kill(b);
                }
            }
        }

        private void DissolveWithMedicine()
        {
            List<Projectile> shots = Projectile.Flying;
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                Projectile p = shots[i];
                if (p == null || p.SourceTeam != Team.Player || p.Medication == null) continue;
                string med = p.Medication.name;
                if (!med.Contains("Mucolitico") && !med.Contains("Antihistaminico")) continue;
                foreach (Ball b in balls)
                {
                    if (!b.alive || Vector2.Distance(p.Position, (Vector2)b.root.position + Vector2.up * 0.5f) > 0.7f) continue;
                    Sfx.EffectAt("Impacto_Mucolitico", b.root.position, 0.6f);
                    Kill(b);
                    p.Consume();
                    break;
                }
            }
        }

        private void Spawn()
        {
            Ball b = balls.Find(x => !x.alive);
            if (b == null)
            {
                b = new Ball { root = new GameObject("BolaMoco").transform };
                b.root.SetParent(transform, true);
                b.art = new GameObject("Dibujo").transform;
                b.art.SetParent(b.root, false);
                b.art.localPosition = new Vector3(0f, ballArt != null ? 0.32f : 0.5f, 0f);
                if (ballArt != null) CodeSprites.Art(b.art, ballArt, 1f, 20, Vector2.zero);
                else
                {
                    CodeSprites.Add(b.art, CodeSprites.Circle, outline, 19, Vector2.zero, new Vector2(1.04f, 1f));
                    CodeSprites.Add(b.art, CodeSprites.Circle, mucus, 20, Vector2.zero, new Vector2(0.9f, 0.86f));
                    CodeSprites.Add(b.art, CodeSprites.Circle, new Color(1f, 1f, 1f, 0.7f), 21, new Vector2(-0.2f, 0.2f), new Vector2(0.26f, 0.16f));
                    CodeSprites.Add(b.art, CodeSprites.Circle, new Color(0.6f, 0.75f, 0.4f, 0.8f), 21, new Vector2(0.15f, -0.1f), new Vector2(0.18f, 0.18f));
                }
                balls.Add(b);
            }
            b.alive = true;
            b.x = direction < 0 ? maxX : minX;
            b.root.position = new Vector3(b.x, floorY, 0f);
            b.root.gameObject.SetActive(true);
        }

        private static void Kill(Ball b)
        {
            b.alive = false;
            b.root.gameObject.SetActive(false);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.6f, 0.8f, 1f, 0.8f);
            Gizmos.DrawLine(new Vector3(minX, floorY + 0.3f, 0f), new Vector3(maxX, floorY + 0.3f, 0f));
        }
    }
}
