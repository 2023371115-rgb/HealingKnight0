using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// MISIL de la nave (en vuelo reemplaza a la bomba, {bomba}): sale hacia adelante, acelera y BUSCA al enemigo más
    /// cercano que tenga enfrente. Al tocarlo (o a una pared, o al acabarse el combustible) revienta y suelta el
    /// medicamento que traes en un área, como la bomba: cada enemigo recibe daño según su tabla de efectividad. La
    /// explosión también borra las balas enemigas que alcanza.
    /// Se arma con figuras en código (no necesita prefab).
    /// </summary>
    public class ShipMissile : MonoBehaviour
    {
        private const float StartSpeed = 7f, TopSpeed = 19f, Acceleration = 26f, TurnRate = 320f;
        private const float Fuel = 2.6f, HitRadius = 0.45f, SeekRange = 16f, BlastTime = 0.32f;

        private float damage, blastRadius, age, speed, retarget, blastTimer = -1f;
        private MedicationData medication;
        private LayerMask groundMask;
        private Vector2 heading;
        private Transform target;
        private Transform flame;
        private SpriteRenderer blast, blastRing;
        private GameObject body;
        private readonly List<Hurtbox> hits = new List<Hurtbox>();

        /// <summary>Dispara un misil desde la nariz de la nave del jugador.</summary>
        public static ShipMissile Launch(PlayerController player, float damage, float radius, MedicationData med, LayerMask ground)
        {
            Vector2 forward = Vector2.right;   // la nave siempre dispara hacia +X (aunque la cámara esté girada)
            Vector2 origin = player.Position + new Vector2(0f, 0.55f * player.Size) + forward * 1.3f * player.Size;
            var go = new GameObject("MisilDeMedicamento");
            go.transform.position = origin;
            var m = go.AddComponent<ShipMissile>();
            m.damage = damage;
            m.blastRadius = radius;
            m.medication = med;
            m.groundMask = ground;
            m.heading = forward.normalized;
            m.speed = StartSpeed + (player.Flight != null ? Mathf.Max(0f, Vector2.Dot(player.Flight.Velocity, forward)) : 0f);
            m.Build(med != null ? med.color : Color.white);
            Sfx.EffectAt("Nave_Despegue", origin, 0.45f);
            return m;
        }

        private void Build(Color color)
        {
            body = new GameObject("Cuerpo");
            body.transform.SetParent(transform, false);
            Transform b = body.transform;
            Color outline = new Color(0.12f, 0.05f, 0.12f);
            // Cohetito con forma de cápsula: punta blanca, cuerpo del color del medicamento, aletas y fuego atrás.
            CodeSprites.Add(b, CodeSprites.Square, outline, 40, new Vector2(-0.05f, 0f), new Vector2(0.72f, 0.36f), "Contorno");
            CodeSprites.Add(b, CodeSprites.Circle, outline, 40, new Vector2(0.31f, 0f), new Vector2(0.4f, 0.36f), "ContornoPunta");
            CodeSprites.Add(b, CodeSprites.Square, color, 41, new Vector2(-0.08f, 0f), new Vector2(0.6f, 0.24f), "Cuerpo");
            CodeSprites.Add(b, CodeSprites.Circle, Color.white, 41, new Vector2(0.3f, 0f), new Vector2(0.3f, 0.24f), "Punta");
            CodeSprites.Add(b, CodeSprites.Square, outline, 39, new Vector2(-0.33f, 0.17f), new Vector2(0.2f, 0.16f), "AletaArriba");
            CodeSprites.Add(b, CodeSprites.Square, outline, 39, new Vector2(-0.33f, -0.17f), new Vector2(0.2f, 0.16f), "AletaAbajo");
            CodeSprites.Add(b, CodeSprites.Circle, new Color(1f, 1f, 1f, 0.5f), 42, new Vector2(0f, 0.06f), new Vector2(0.4f, 0.06f), "Brillo");
            flame = new GameObject("Fuego").transform;
            flame.SetParent(b, false);
            flame.localPosition = new Vector3(-0.42f, 0f, 0f);
            CodeSprites.Add(flame, CodeSprites.Glow, new Color(1f, 0.75f, 0.3f, 0.95f), 38, new Vector2(-0.2f, 0f), new Vector2(0.7f, 0.4f), "Llama");
            CodeSprites.Add(flame, CodeSprites.Glow, new Color(1f, 1f, 0.85f, 1f), 38, new Vector2(-0.08f, 0f), new Vector2(0.3f, 0.2f), "Centro");
            blast = CodeSprites.Add(transform, CodeSprites.Glow, color, 45, Vector2.zero, Vector2.one, "Explosion");
            blastRing = CodeSprites.Add(transform, CodeSprites.Ring, Color.white, 46, Vector2.zero, Vector2.one, "Onda");
            blast.enabled = blastRing.enabled = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (blastTimer >= 0f) { UpdateBlast(dt); return; }

            age += dt;
            speed = Mathf.Min(TopSpeed, speed + Acceleration * dt);
            retarget -= dt;
            if (retarget <= 0f || target == null || !target.gameObject.activeInHierarchy) { retarget = 0.15f; target = FindTarget(); }

            // Gira hacia el objetivo (después de un instante, para que primero salga derecho).
            if (target != null && age > 0.1f)
            {
                Vector2 to = (Vector2)TargetPoint(target) - (Vector2)transform.position;
                if (to.sqrMagnitude > 0.001f)
                {
                    float angle = Vector2.SignedAngle(heading, to);
                    float step = Mathf.Clamp(angle, -TurnRate * dt, TurnRate * dt);
                    heading = Quaternion.Euler(0f, 0f, step) * heading;
                }
            }
            Vector2 next = (Vector2)transform.position + heading * speed * dt;
            transform.position = next;
            body.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg);
            if (flame != null) flame.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 45f) * 0.18f);

            if (Hurtbox.FirstInCircle(Team.Enemy, next, HitRadius) != null
                || (age > 0.08f && Physics2D.OverlapCircle(next, 0.15f, groundMask) != null)
                || age > Fuel || OutOfView(next))
                Explode();
        }

        private static bool OutOfView(Vector2 p)
        {
            FlightRoute route = FlightRoute.Current;
            if (route == null) return false;
            Rect v = route.View;
            return p.x < v.xMin - 3f || p.x > v.xMax + 3f || p.y < v.yMin - 3f || p.y > v.yMax + 3f;
        }

        /// <summary>El enemigo vivo más cercano que esté ENFRENTE del misil (no se da la vuelta por uno de atrás).</summary>
        private Transform FindTarget()
        {
            Transform best = null;
            float bestScore = float.MaxValue;
            Vector2 p = transform.position;
            foreach (EnemyHealth e in EnemyHealth.Active)
            {
                if (e == null || !e.IsAlive) continue;
                Vector2 to = (Vector2)TargetPoint(e.transform) - p;
                float d = to.magnitude;
                if (d > SeekRange || d < 0.01f) continue;
                float facing = Vector2.Dot(to / d, heading);
                if (facing < 0.1f) continue;
                float score = d * (1.6f - facing);   // prefiere lo que tiene más de frente
                if (score < bestScore) { bestScore = score; best = e.transform; }
            }
            return best;
        }

        private static Vector3 TargetPoint(Transform t)
        {
            Hurtbox h = t.GetComponent<Hurtbox>();
            return h != null ? (Vector3)h.Center : t.position;
        }

        private void Explode()
        {
            blastTimer = 0f;
            Vector2 at = transform.position;
            Sfx.EffectAt("Bomba_Explota", at);
            if (body != null) body.SetActive(false);
            blast.enabled = blastRing.enabled = true;

            Hurtbox.QueryCircle(Team.Enemy, at, blastRadius, hits);
            foreach (Hurtbox h in hits)
            {
                if (h == null || h.Damageable == null) continue;
                DamageInfo info = DamageInfo.Create(damage, DamageKind.Projectile, Team.Player, at);
                info.knockback = 4f;
                info.medication = medication;
                h.Damageable.ApplyDamage(info);
            }
            // La onda también se lleva las balas enemigas cercanas.
            for (int i = Projectile.Flying.Count - 1; i >= 0; i--)
            {
                Projectile p = Projectile.Flying[i];
                if (p != null && p.SourceTeam == Team.Enemy && ((Vector2)p.Position - at).sqrMagnitude < blastRadius * blastRadius) p.Consume();
            }
        }

        private void UpdateBlast(float dt)
        {
            blastTimer += dt;
            float k = Mathf.Clamp01(blastTimer / BlastTime);
            float size = blastRadius * 2.2f * Mathf.Lerp(0.5f, 1.15f, Mathf.Sqrt(k));
            blast.transform.localScale = Vector3.one * size;
            blastRing.transform.localScale = Vector3.one * size * 0.9f;
            Color c = blast.color; c.a = 0.85f * (1f - k); blast.color = c;
            Color r = blastRing.color; r.a = 1f - k; blastRing.color = r;
            if (blastTimer >= BlastTime) Destroy(gameObject);
        }
    }
}
