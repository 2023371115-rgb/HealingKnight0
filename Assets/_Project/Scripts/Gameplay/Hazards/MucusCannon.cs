using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// CAÑÓN DE MOCO: una glándula de la mucosa de la nariz que lanza bolas de moco en arco.
    /// Biología: la mucosa produce moco todo el tiempo para atrapar polvo, polen y microbios.
    /// - Si una bola te pega, se te embarra: un poco de daño y te frena.
    /// - Si cae al piso (o a una plataforma), se queda pegada un rato y SIRVE DE PLATAFORMA (pegajosa: te frena un poco).
    /// - Apunta hacia donde estás si te tiene a tiro; si no, lanza hacia el frente.
    /// Se dibuja por código (o con los sprites 'glandArt' y 'ballArt').
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class MucusCannon : MonoBehaviour
    {
        [SerializeField] private float interval = 2.6f;
        [SerializeField] private float offset;
        [SerializeField] private float range = 13f;
        [SerializeField] private float flightTime = 1.1f;
        [SerializeField] private float gravity = 18f;
        [SerializeField] private int direction = -1;          // hacia dónde lanza si no te ve
        [SerializeField] private float damage = 5f;
        [SerializeField] private float stickTime = 3.5f;
        [SerializeField] private Sprite glandArt;
        [SerializeField] private Sprite ballArt;
        [SerializeField] private Color mucus = new Color(0.78f, 0.88f, 0.32f, 1f);
        [SerializeField] private Color outline = new Color(0.22f, 0.26f, 0.06f, 1f);
        [SerializeField] private Color gland = new Color(0.82f, 0.42f, 0.62f, 1f);

        private class Ball
        {
            public Transform root, art;
            public BoxCollider2D floor;
            public Vector2 velocity;
            public float life;
            public bool flying, stuck;
        }

        private readonly List<Ball> balls = new List<Ball>();
        private float timer;
        private Transform glandVisual;
        private int groundMask;

        public void Configure(float period, float startOffset, int facing, Sprite glandSprite, Sprite ballSprite)
        {
            interval = period;
            offset = startOffset;
            direction = facing;
            glandArt = glandSprite;
            ballArt = ballSprite;
        }

        private void Start()
        {
            groundMask = 1 << PhysicsLayers.Ground;
            timer = interval + offset;
            BuildGland();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            timer -= dt;
            if (timer <= 0f)
            {
                timer = interval;
                Fire();
            }
            PlayerController player = PlayerController.Current;
            foreach (Ball b in balls) StepBall(b, player, dt);
        }

        /// <summary>El mucolítico (o el antihistamínico) disuelve las bolas: en el aire o pegadas.</summary>
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
                    if (!b.flying && !b.stuck) continue;
                    Vector2 c = (Vector2)b.root.position + Vector2.up * 0.2f;
                    if (Vector2.Distance(c, p.Position) > (b.stuck ? 1.05f : 0.7f)) continue;
                    Sfx.EffectAt("Impacto_Mucolitico", c, 0.6f);
                    Pop(b);
                    p.Consume();
                    break;
                }
            }
        }

        private void Update()
        {
            DissolveWithMedicine();   // antes que el disparo choque con la bola (que es "piso") y desaparezca
            // La glándula se infla antes de disparar (aviso).
            if (glandVisual == null) return;
            float k = timer < 0.5f ? 1f - timer / 0.5f : 0f;
            glandVisual.localScale = new Vector3(1f + k * 0.15f, 1f + k * 0.3f, 1f);
        }

        private void Fire()
        {
            Vector2 from = (Vector2)transform.position + Vector2.up * 0.9f;
            PlayerController player = PlayerController.Current;
            Vector2 target;
            if (player != null && player.Health.IsAlive && Mathf.Abs(player.Position.x - from.x) <= range)
                target = player.Position + Vector2.up * 0.6f + new Vector2(player.Motor.Velocity.x * 0.4f, 0f);
            else target = from + new Vector2(direction * 6f, -0.9f);

            float t = flightTime;
            Vector2 v = new Vector2((target.x - from.x) / t, (target.y - from.y + 0.5f * gravity * t * t) / t);

            Ball b = balls.Find(x => !x.flying && !x.stuck) ?? Create();
            b.root.gameObject.SetActive(true);
            b.root.position = from;
            b.velocity = v;
            b.flying = true;
            b.stuck = false;
            b.life = 0f;
            b.floor.enabled = false;
            b.art.localScale = Vector3.one;
            b.art.localRotation = Quaternion.identity;
            Sfx.EffectAt("Acido_Burbujeo", from, 0.5f);
        }

        private void StepBall(Ball b, PlayerController player, float dt)
        {
            if (b.flying)
            {
                Vector2 p = b.root.position;
                b.velocity.y -= gravity * dt;
                Vector2 next = p + b.velocity * dt;
                b.art.Rotate(0f, 0f, -b.velocity.x * 40f * dt);

                // ¿Le pegó al jugador?
                if (player != null && player.Health.IsAlive && Vector2.Distance(next, player.Position + Vector2.up * 0.9f) < 0.85f)
                {
                    DamageInfo info = DamageInfo.Create(damage, DamageKind.Contact, Team.Enemy, next);
                    info.knockback = 3f;
                    player.Health.ApplyDamage(info);
                    player.ApplySlow(0.5f, 1.6f);
                    Pop(b);
                    return;
                }

                // ¿Cayó en algo sólido? Se queda pegada.
                RaycastHit2D hit = Physics2D.Linecast(p, next, groundMask);
                if (hit.collider != null && b.velocity.y < 0f && hit.collider != b.floor)
                {
                    b.flying = false;
                    b.stuck = true;
                    b.life = stickTime;
                    b.root.position = hit.point + Vector2.up * 0.02f;
                    b.floor.enabled = true;
                    b.art.localRotation = Quaternion.identity;
                    b.art.localScale = new Vector3(1.6f, 0.7f, 1f);
                    Sfx.EffectAt("Alveolo_Desinfla", hit.point, 0.4f);
                    return;
                }
                b.root.position = next;
                b.life += dt;
                if (b.life > 6f) Pop(b);
                return;
            }

            if (!b.stuck) return;
            b.life -= dt;
            // Pegajosa: si estás encima te frena un poco.
            if (player != null && player.Motor != null)
            {
                Vector2 feet = player.Position;
                Vector2 top = (Vector2)b.root.position + Vector2.up * 0.35f;
                if (Mathf.Abs(feet.x - top.x) < 0.8f && Mathf.Abs(feet.y - top.y) < 0.2f) player.ApplySlow(0.75f, 0.2f);
            }
            float s = b.life < 0.6f ? Mathf.Max(0.05f, b.life / 0.6f) : 1f;
            b.art.localScale = new Vector3(1.6f * s, 0.7f * s, 1f);
            if (b.life <= 0f) Pop(b);
        }

        private void Pop(Ball b)
        {
            b.flying = b.stuck = false;
            b.floor.enabled = false;
            b.root.gameObject.SetActive(false);
        }

        private Ball Create()
        {
            var root = new GameObject("BolaMoco");
            root.transform.SetParent(transform, true);
            root.layer = PhysicsLayers.Ground;
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.3f, 0.25f);
            box.offset = new Vector2(0f, 0.22f);
            box.usedByEffector = true;
            var eff = root.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 160f;
            box.enabled = false;

            var art = new GameObject("Dibujo").transform;
            art.SetParent(root.transform, false);
            art.localPosition = new Vector3(0f, 0.2f, 0f);
            if (ballArt != null) CodeSprites.Art(art, ballArt, 0.8f, 25, Vector2.zero);
            else
            {
                Sprite c = CodeSprites.Circle;
                CodeSprites.Add(art, c, outline, 24, Vector2.zero, new Vector2(0.82f, 0.78f));
                CodeSprites.Add(art, c, mucus, 25, Vector2.zero, new Vector2(0.7f, 0.66f));
                CodeSprites.Add(art, c, new Color(1f, 1f, 0.85f, 0.85f), 26, new Vector2(-0.14f, 0.14f), new Vector2(0.2f, 0.12f));
            }
            var b = new Ball { root = root.transform, art = art, floor = box };
            balls.Add(b);
            return b;
        }

        private void BuildGland()
        {
            glandVisual = new GameObject("Glandula").transform;
            glandVisual.SetParent(transform, false);
            if (glandArt != null) { CodeSprites.Art(glandVisual, glandArt, 1.8f, -4, new Vector2(0f, 0.4f)); return; }
            Sprite c = CodeSprites.Circle;
            CodeSprites.Add(glandVisual, c, outline, -6, new Vector2(0f, 0.35f), new Vector2(1.9f, 1.3f));
            CodeSprites.Add(glandVisual, c, gland, -5, new Vector2(0f, 0.35f), new Vector2(1.76f, 1.16f));
            CodeSprites.Add(glandVisual, c, outline, -4, new Vector2(0f, 0.75f), new Vector2(0.6f, 0.36f));
            CodeSprites.Add(glandVisual, c, mucus, -3, new Vector2(0f, 0.74f), new Vector2(0.48f, 0.26f));
            CodeSprites.Add(glandVisual, c, new Color(1f, 0.85f, 0.92f, 0.7f), -3, new Vector2(-0.45f, 0.6f), new Vector2(0.4f, 0.18f));
        }
    }
}
