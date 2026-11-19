using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Flow;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// JEFE SECRETO del Riñón: el CÁLCULO GIGANTE (una piedra de calcio en la pelvis renal).
    /// - No se le dispara a lo loco: las balas REBOTAN en la piedra. Solo le hace daño pegarle en la GRIETA que brilla
    ///   (LITOTRICIA: ondas de choque que rompen la piedra justo en sus grietas). Las grietas van en orden.
    /// - Cada vez que se rompe una grieta se le cae un pedazo: piedritas que ruedan hacia ti (sáltalas).
    /// - Da saltos y al caer manda ondas por el piso (sáltalas).
    /// Premio: DEPURACIÓN (lo que te alenta te dura la mitad, en todo el juego).
    /// Biología: los cálculos se forman cuando hay poca agua y muchas sales en la orina. Se previenen tomando AGUA.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class KidneyStoneBoss : MonoBehaviour
    {
        [SerializeField] private string bossName = "CÁLCULO GIGANTE";
        [SerializeField] private string rewardSecret = "premio_depuracion";
        [SerializeField] private float radius = 2.6f;
        [SerializeField] private float slamEvery = 4.5f;

        private class Crack { public Vector2 local; public Transform glow; }
        private class Pebble { public Transform t; public float x, dir; public bool alive; }
        private class Wave { public Transform t; public float x; public int dir; public bool hit, alive; }

        private readonly List<Crack> cracks = new List<Crack>();
        private readonly List<Pebble> pebbles = new List<Pebble>();
        private readonly List<Wave> waves = new List<Wave>();
        private EnemyHealth health;
        private Hurtbox box;
        private Transform art;
        private int current;
        private float floorY, t, slamTimer, hop = -1f, contactCooldown, tinkCooldown;
        private bool dead, toldTink;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            box = GetComponent<Hurtbox>();
            health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (!dead) GameplayEvents.RaiseBossEnded();
            foreach (Pebble p in pebbles) if (p.t != null) Destroy(p.t.gameObject);
            foreach (Wave w in waves) if (w.t != null) Destroy(w.t.gameObject);
        }

        private void Start()
        {
            floorY = transform.position.y;
            art = new GameObject("Dibujo").transform;
            art.SetParent(transform, false);
            art.localPosition = new Vector3(0f, radius, 0f);
            CodeSprites.Add(art, CodeSprites.Circle, new Color(0.3f, 0.22f, 0.1f), 10, Vector2.zero, Vector2.one * radius * 2.1f, "Contorno");
            CodeSprites.Add(art, CodeSprites.Circle, new Color(0.85f, 0.73f, 0.45f), 11, Vector2.zero, Vector2.one * radius * 2f, "Piedra");
            var rnd = new System.Random(5);
            for (int i = 0; i < 12; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = (float)rnd.NextDouble() * radius * 0.75f;
                CodeSprites.Add(art, CodeSprites.Circle, new Color(0.7f, 0.6f, 0.32f), 12, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r,
                    Vector2.one * (0.4f + (float)rnd.NextDouble() * 0.5f), "Cristal");
            }
            foreach (int side in new[] { -1, 1 })
            {
                CodeSprites.Add(art, CodeSprites.Circle, Color.white, 13, new Vector2(side * 0.7f, 0.5f), new Vector2(0.7f, 0.75f), "Ojo");
                CodeSprites.Add(art, CodeSprites.Circle, new Color(0.2f, 0.12f, 0.05f), 14, new Vector2(side * 0.65f, 0.45f), Vector2.one * 0.32f, "Pupila");
                CodeSprites.Add(art, CodeSprites.Square, new Color(0.3f, 0.22f, 0.1f), 14, new Vector2(side * 0.7f, 1f), new Vector2(0.7f, 0.14f), "Ceja")
                    .transform.localRotation = Quaternion.Euler(0f, 0f, side * -18f);
            }
            // Tres grietas: abajo a la izquierda, arriba y abajo a la derecha (en ese orden).
            foreach (Vector2 c in new[] { new Vector2(-1.7f, -0.9f), new Vector2(0.2f, 1.9f), new Vector2(1.7f, -0.8f) })
            {
                var crack = new Crack { local = c };
                Transform g = new GameObject("Grieta").transform;
                g.SetParent(art, false);
                g.localPosition = c;
                CodeSprites.Add(g, CodeSprites.Glow, new Color(0.5f, 0.9f, 1f, 0.8f), 15, Vector2.zero, Vector2.one * 1.8f, "Brillo");
                for (int k = 0; k < 3; k++)
                    CodeSprites.Add(g, CodeSprites.Square, new Color(0.15f, 0.1f, 0.05f), 16, new Vector2(-0.2f + k * 0.2f, (k - 1) * 0.15f), new Vector2(0.5f, 0.07f), "Raya")
                        .transform.localRotation = Quaternion.Euler(0f, 0f, -40f + k * 35f);
                crack.glow = g;
                cracks.Add(crack);
            }
            GameplayEvents.RaiseBossStarted(bossName, health);
            GameplayEvents.RaiseMessage("¡El CÁLCULO GIGANTE! Las balas rebotan en la piedra: pégale SOLO en la GRIETA que brilla. Así funciona la " +
                "LITOTRICIA: ondas de choque que rompen el cálculo por sus grietas.");
            slamTimer = slamEvery * 0.7f;
            UpdateCracks();
        }

        private void UpdateCracks()
        {
            for (int i = 0; i < cracks.Count; i++) cracks[i].glow.gameObject.SetActive(i == current);
        }

        private Vector2 CrackWorld => (Vector2)art.position + (Vector2)(art.localRotation * (Vector3)(cracks[Mathf.Min(current, cracks.Count - 1)].local * art.localScale.x));

        private void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            UpdatePebbles(dt);
            UpdateWaves(dt);
            if (dead) return;
            if (contactCooldown > 0f) contactCooldown -= dt;
            if (tinkCooldown > 0f) tinkCooldown -= dt;

            // ¿Ya se rompió la grieta de ahorita? (cada una es un tercio de la vida)
            int should = Mathf.Clamp(Mathf.FloorToInt((1f - health.Health01) * 3f), 0, 2);
            if (should > current) BreakChunk(should);

            // Salto y golpe: se agacha, brinca y al caer manda ondas.
            slamTimer -= dt;
            PlayerController player = PlayerController.Current;
            if (hop < 0f && slamTimer <= 0f) { hop = 0f; slamTimer = Mathf.Lerp(slamEvery * 0.6f, slamEvery, health.Health01); }
            float y = floorY, sx = 1f, sy = 1f;
            if (hop >= 0f)
            {
                hop += dt;
                if (hop < 0.5f) { sy = 1f - hop * 0.3f; sx = 1f + hop * 0.2f; }
                else if (hop < 1.3f)
                {
                    float k = (hop - 0.5f) / 0.8f;
                    y = floorY + Mathf.Sin(k * Mathf.PI) * 4.5f;
                    if (player != null)
                    {
                        Vector3 p = transform.position;
                        p.x = Mathf.MoveTowards(p.x, Mathf.Clamp(player.Position.x, WalkableArea.MinX + radius + 1f, WalkableArea.MaxX - radius - 1f), 6f * dt);
                        transform.position = p;
                    }
                }
                else
                {
                    Slam();
                    hop = -1f;
                }
            }
            transform.position = new Vector3(transform.position.x, y, 0f);
            float shrink = Mathf.Lerp(0.7f, 1f, health.Health01);
            art.localScale = new Vector3(sx, sy, 1f) * shrink;
            art.localPosition = new Vector3(0f, radius * shrink * sy, 0f);
            art.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.3f) * 6f);

            // Solo la grieta que brilla recibe balas; en lo demás rebotan.
            if (box != null) box.SetBox(CrackWorld - (Vector2)transform.position, new Vector2(1.1f, 1.1f));
            Vector2 center = (Vector2)art.position;
            float r = radius * shrink;
            for (int i = Projectile.Flying.Count - 1; i >= 0; i--)
            {
                Projectile pr = Projectile.Flying[i];
                if (pr == null || pr.SourceTeam != Team.Player) continue;
                Vector2 pp = pr.Position;
                if ((pp - center).sqrMagnitude > r * r || (pp - CrackWorld).sqrMagnitude < 0.7f) continue;
                pr.Consume();
                if (tinkCooldown <= 0f) { tinkCooldown = 0.15f; Sfx.EffectAt("Impacto_Anticoagulante", pp, 0.4f); }
                if (!toldTink)
                {
                    toldTink = true;
                    GameplayEvents.RaiseMessage("¡Rebotó! Apunta a la GRIETA que brilla.");
                }
            }
            TryContact(player, center, r);
        }

        private void BreakChunk(int next)
        {
            current = next;
            UpdateCracks();
            SpeedBannerShow("¡CRACK!");
            Sfx.EffectAt("Coagulo_Disuelve", transform.position, 1f);
            Camera cam = Camera.main;
            if (cam != null && cam.TryGetComponent(out GameCamera gc)) gc.Shake(0.3f, 0.3f);
            for (int i = 0; i < 3; i++) SpawnPebble(i % 2 == 0 ? -1f : 1f, i * 0.6f);
            if (next == 1) GameplayEvents.RaiseMessage("¡Se le cayó un pedazo! Salta las piedritas que ruedan. Ahora la grieta de ARRIBA.");
        }

        private static void SpeedBannerShow(string text) => Flight.SpeedBanner.Show(text, new Color(0.6f, 0.95f, 1f), 0.9f);

        private void SpawnPebble(float dir, float offset)
        {
            var p = new Pebble { t = new GameObject("Piedrita").transform, x = transform.position.x + dir * (radius + 0.6f + offset), dir = dir, alive = true };
            CodeSprites.Add(p.t, CodeSprites.Circle, new Color(0.3f, 0.22f, 0.1f), 30, new Vector2(0f, 0.45f), Vector2.one * 0.95f, "Contorno");
            CodeSprites.Add(p.t, CodeSprites.Circle, new Color(0.85f, 0.73f, 0.45f), 31, new Vector2(0f, 0.45f), Vector2.one * 0.8f, "Piedra");
            pebbles.Add(p);
        }

        private void UpdatePebbles(float dt)
        {
            PlayerController player = PlayerController.Current;
            foreach (Pebble p in pebbles)
            {
                if (!p.alive) continue;
                p.x += p.dir * 5.5f * dt;
                p.t.position = new Vector3(p.x, floorY, 0f);
                p.t.localRotation = Quaternion.Euler(0f, 0f, -p.x * 100f * p.dir);
                if (p.x < WalkableArea.MinX - 1f || p.x > WalkableArea.MaxX + 1f) { p.alive = false; p.t.gameObject.SetActive(false); continue; }
                if (player == null || !player.Health.IsAlive) continue;
                if (Mathf.Abs(player.Position.x - p.x) < 0.6f && player.Position.y < floorY + 0.9f)
                {
                    DamageInfo info = DamageInfo.Create(10f, DamageKind.Contact, Team.Enemy, p.t.position);
                    info.knockback = 6f;
                    player.Health.ApplyDamage(info);
                    p.alive = false;
                    p.t.gameObject.SetActive(false);
                }
            }
        }

        private void Slam()
        {
            Sfx.EffectAt("Tos_Fuerte", transform.position, 0.9f);
            Camera cam = Camera.main;
            if (cam != null && cam.TryGetComponent(out GameCamera gc)) gc.Shake(0.25f, 0.25f);
            foreach (int dir in new[] { -1, 1 })
            {
                var w = new Wave { t = new GameObject("Onda").transform, x = transform.position.x + dir * radius, dir = dir, alive = true };
                CodeSprites.Add(w.t, CodeSprites.Circle, new Color(0.35f, 0.25f, 0.1f, 0.9f), 40, new Vector2(0f, 0.4f), new Vector2(0.9f, 0.9f), "Contorno");
                CodeSprites.Add(w.t, CodeSprites.Circle, new Color(0.95f, 0.85f, 0.55f), 41, new Vector2(0f, 0.4f), new Vector2(0.7f, 0.7f), "Onda");
                waves.Add(w);
            }
        }

        private void UpdateWaves(float dt)
        {
            PlayerController player = PlayerController.Current;
            foreach (Wave w in waves)
            {
                if (!w.alive) continue;
                w.x += w.dir * 9f * dt;
                w.t.position = new Vector3(w.x, floorY, 0f);
                if (w.x < WalkableArea.MinX - 1f || w.x > WalkableArea.MaxX + 1f) { w.alive = false; w.t.gameObject.SetActive(false); continue; }
                if (w.hit || player == null || !player.Health.IsAlive) continue;
                if (Mathf.Abs(player.Position.x - w.x) < 0.55f && player.Position.y < floorY + 0.8f)
                {
                    w.hit = true;
                    DamageInfo info = DamageInfo.Create(11f, DamageKind.Contact, Team.Enemy, new Vector2(w.x, floorY));
                    info.knockback = 6f;
                    player.Health.ApplyDamage(info);
                }
            }
        }

        private void TryContact(PlayerController player, Vector2 center, float r)
        {
            if (player == null || contactCooldown > 0f || !player.Health.IsAlive) return;
            if (Vector2.Distance(player.Motor.Center, center) > r + 0.4f) return;
            DamageInfo info = DamageInfo.Create(14f, DamageKind.Contact, Team.Enemy, center);
            info.knockback = 8f;
            player.Health.ApplyDamage(info);
            contactCooldown = 1f;
        }

        private void OnDied(EnemyHealth _)
        {
            dead = true;
            GameplayEvents.RaiseBossEnded();
            bool isNew = !string.IsNullOrEmpty(rewardSecret) && GameSession.FindSecret(rewardSecret);
            GameplayEvents.RaiseMessage("¡Cálculo hecho polvo! Los cálculos se previenen tomando mucha AGUA." +
                (isNew ? " PREMIO: DEPURACIÓN. Ahora lo que te alenta te dura la MITAD en todo el juego (el riñón limpia la sangre)." : ""));
        }
    }
}
