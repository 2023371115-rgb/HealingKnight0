using System.Collections.Generic;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Pooling;
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
    /// MINIJEFE del Riñón: la COLONIA DE PROTEUS, una pared de BIOPELÍCULA (baba con bacterias pegadas y cristales).
    /// - No se mueve: tapa el túbulo. Dispara cristales en abanico hacia ti.
    /// - Suelta bacterias Proteus (máximo 3 a la vez).
    /// - A la mitad de la vida se enoja: dispara más seguido y le caen CRISTALES desde arriba (sombra = aviso).
    /// - La biopelícula la protege: se REGENERA poco a poco si dejas de pegarle (así de difícil es curar una biopelícula).
    /// Lo que recibe daño es su cara (el centro). Biología: las biopelículas protegen a las bacterias de los medicamentos;
    /// el Proteus además forma cristales (estruvita) y piedras: por eso viene antes del cálculo gigante.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class ProteusColonyBoss : MonoBehaviour
    {
        [SerializeField] private string bossName = "COLONIA DE PROTEUS";
        [SerializeField] private float height = 6f, width = 3.4f;
        [SerializeField] private Projectile crystalShot;
        [SerializeField] private EnemyHealth proteusPrefab;
        [SerializeField] private float regenPerSecond = 3f;

        private class Shard { public Transform t, shadow; public float x, y, wait; public bool alive; }

        private EnemyHealth health;
        private BossPoses poses;
        private Transform art;
        private ComponentPool<Projectile> pool;
        private readonly List<EnemyHealth> minions = new List<EnemyHealth>();
        private readonly List<Shard> shards = new List<Shard>();
        private float t, shootTimer = 2f, spawnTimer = 5f, rainTimer = 3f, lastHit, contactCooldown, lastHealth;
        private bool angry, dead;

        public void Configure(Projectile shot, EnemyHealth minion, float wallHeight, float wallWidth)
        {
            crystalShot = shot; proteusPrefab = minion; height = wallHeight; width = wallWidth;
        }

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            poses = GetComponent<BossPoses>();
            if (poses != null && !poses.HasArt) poses = null;
            health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (!dead) GameplayEvents.RaiseBossEnded();
            foreach (Shard s in shards) { if (s.t != null) Destroy(s.t.gameObject); if (s.shadow != null) Destroy(s.shadow.gameObject); }
        }

        private void Start()
        {
            art = new GameObject("Dibujo").transform;
            art.SetParent(transform, false);
            if (poses != null)
            {
                // El dibujo dispara hacia la derecha: se voltea para que mire hacia ti (llegas por la izquierda).
                SpriteRenderer drawn = poses.Build(art, new Vector2(0f, 0f));
                drawn.flipX = true;
                health.SetFlashRenderers(drawn);
            }
            else BuildFigure();
            lastHealth = health.Health01;
            GameplayEvents.RaiseBossStarted(bossName, health);
            GameplayEvents.RaiseMessage("¡Una COLONIA DE PROTEUS! Es una BIOPELÍCULA: muchas bacterias pegadas en una baba que las protege. " +
                "Si dejas de dispararle se REGENERA: no le des descanso. Antibiótico, a la CARA.");
        }

        /// <summary>La pared de baba con figuras (mientras no hay dibujo en Art/Jefes/ProteusColonia).</summary>
        private void BuildFigure()
        {
            Color line = new Color(0.2f, 0.1f, 0.3f), slime = new Color(0.65f, 0.5f, 0.92f), light = new Color(0.82f, 0.72f, 1f);
            CodeSprites.Add(art, CodeSprites.Square, line, 10, new Vector2(0f, height * 0.5f), new Vector2(width + 0.25f, height + 0.2f), "Contorno");
            CodeSprites.Add(art, CodeSprites.Square, slime, 11, new Vector2(0f, height * 0.5f), new Vector2(width, height), "Baba");
            CodeSprites.Add(art, CodeSprites.Circle, line, 10, new Vector2(0f, height), new Vector2(width + 0.25f, 1.6f), "TopeContorno");
            CodeSprites.Add(art, CodeSprites.Circle, slime, 11, new Vector2(0f, height), new Vector2(width, 1.4f), "Tope");
            var rnd = new System.Random(3);
            for (int i = 0; i < 14; i++)
            {
                Vector2 p = new Vector2(((float)rnd.NextDouble() - 0.5f) * width * 0.8f, 0.4f + (float)rnd.NextDouble() * (height - 0.6f));
                CodeSprites.Add(art, CodeSprites.Circle, new Color(0.45f, 0.3f, 0.75f), 12, p, new Vector2(0.55f, 0.22f), "Bacteria")
                    .transform.localRotation = Quaternion.Euler(0f, 0f, (float)rnd.NextDouble() * 180f);
            }
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                Transform c = CodeSprites.Add(art, CodeSprites.Square, Color.white, 13, new Vector2(side * width * 0.5f, 1f + i * 0.85f), new Vector2(0.7f, 0.22f), "Cristal").transform;
                c.localRotation = Quaternion.Euler(0f, 0f, side * 25f);
            }
            CodeSprites.Add(art, CodeSprites.Circle, light, 12, new Vector2(0f, height * 0.55f), new Vector2(width * 0.75f, 2.4f), "Cara");
            foreach (int s in new[] { -1, 1 })
            {
                CodeSprites.Add(art, CodeSprites.Circle, Color.white, 14, new Vector2(s * 0.55f, height * 0.6f), new Vector2(0.6f, 0.65f), "Ojo");
                CodeSprites.Add(art, CodeSprites.Circle, line, 15, new Vector2(s * 0.5f - 0.1f, height * 0.58f), Vector2.one * 0.28f, "Pupila");
                CodeSprites.Add(art, CodeSprites.Square, line, 15, new Vector2(s * 0.55f, height * 0.6f + 0.45f), new Vector2(0.6f, 0.12f), "Ceja")
                    .transform.localRotation = Quaternion.Euler(0f, 0f, s * -18f);
            }
            CodeSprites.Add(art, CodeSprites.Square, line, 15, new Vector2(0f, height * 0.42f), new Vector2(0.9f, 0.14f), "Boca");
        }

        private Vector2 Face => (Vector2)transform.position + new Vector2(0f, height * 0.55f);

        private void Update()
        {
            float dt = Time.deltaTime;
            UpdateShards(dt);
            if (dead) return;
            t += dt;
            if (contactCooldown > 0f) contactCooldown -= dt;

            // ¿Le pegaron? (para la regeneración)
            if (health.Health01 < lastHealth - 0.0001f) lastHit = t;
            // La biopelícula se rehace si la dejas en paz.
            if (t - lastHit > 2.5f && health.Health01 < 1f) health.Heal(regenPerSecond * dt);
            lastHealth = health.Health01;

            if (!angry && health.Health01 < 0.5f)
            {
                angry = true;
                if (poses != null) poses.SetAngry(true);
                Flight.SpeedBanner.Show("¡SE ENOJÓ!", new Color(0.8f, 0.6f, 1f), 1f);
                GameplayEvents.RaiseMessage("¡La colonia suelta CRISTALES desde arriba! Fíjate en las sombras.");
            }

            // Respira (la baba sube y baja).
            art.localScale = new Vector3(1f + Mathf.Sin(t * 2f) * 0.03f, 1f + Mathf.Sin(t * 2f + 1f) * 0.03f, 1f);

            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;

            shootTimer -= dt;
            if (shootTimer <= 0f) { shootTimer = angry ? 1.8f : 2.6f; Shoot(player); }

            spawnTimer -= dt;
            if (spawnTimer <= 0f) { spawnTimer = angry ? 6f : 8f; SpawnMinion(); }

            if (angry)
            {
                rainTimer -= dt;
                if (rainTimer <= 0f) { rainTimer = 2.2f; DropShard(player.Position.x + Random.Range(-2f, 2f)); }
            }

            // Tocar la baba duele.
            Vector2 c = player.Motor.Center, me = transform.position;
            if (contactCooldown <= 0f && Mathf.Abs(c.x - me.x) < width * 0.5f + 0.3f && c.y > me.y - 0.5f && c.y < me.y + height + 0.5f)
            {
                DamageInfo info = DamageInfo.Create(10f, DamageKind.Contact, Team.Enemy, me + new Vector2(0f, c.y - me.y));
                info.knockback = 9f;
                player.Health.ApplyDamage(info);
                contactCooldown = 1f;
            }
        }

        private void Shoot(PlayerController player)
        {
            if (crystalShot == null) return;
            if (pool == null) pool = new ComponentPool<Projectile>(crystalShot, 6, 30);
            if (poses != null) poses.Show(BossPoses.Pose.Attack, 0.6f);
            Vector2 from = Face + new Vector2(-width * 0.5f, 0f);
            Vector2 dir = (player.Motor.Center - from).normalized;
            int shots = angry ? 5 : 3;
            float spread = angry ? 56f : 36f;
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            for (int i = 0; i < shots; i++)
            {
                float a = (baseAngle - spread * 0.5f + spread * i / (shots - 1)) * Mathf.Deg2Rad;
                Projectile p = pool.Get();
                p.Launch(new ProjectileShot
                {
                    origin = from,
                    direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a)),
                    speed = 5.5f,
                    range = 18f,
                    damage = 8f,
                    knockback = 2f,
                    radius = 0.22f,
                    sourceTeam = Team.Enemy,
                    targetTeam = Team.Player,
                    color = new Color(0.95f, 0.97f, 1f),
                    groundMask = 0
                }, finished => pool.Release(finished));
            }
            Sfx.PlayAt(Sfx.EnemySound(health.Data, "Disparo"), from, CombatAudio.Enemies);
        }

        private void SpawnMinion()
        {
            minions.RemoveAll(m => m == null || !m.IsAlive);
            if (proteusPrefab == null || minions.Count >= 3) return;
            Vector3 at = (Vector3)Face + new Vector3(-width * 0.5f - 0.8f, Random.Range(-1.5f, 1.5f), 0f);
            EnemyHealth e = Instantiate(proteusPrefab, at, Quaternion.identity);
            minions.Add(e);
            Sfx.EffectAt("Coagulo_Disuelve", at, 0.4f);
        }

        // ---- Cristales que caen desde arriba (con sombra de aviso) ----

        private void DropShard(float x)
        {
            x = Mathf.Clamp(x, WalkableArea.MinX + 1f, transform.position.x - width);
            float floor = transform.position.y;
            var s = new Shard { x = x, y = floor + 11f, wait = 0.9f, alive = true, t = new GameObject("CristalQueCae").transform, shadow = new GameObject("Sombra").transform };
            CodeSprites.Add(s.t, CodeSprites.Square, new Color(0.3f, 0.25f, 0.35f), 40, Vector2.zero, new Vector2(0.45f, 1.2f), "Contorno");
            CodeSprites.Add(s.t, CodeSprites.Square, Color.white, 41, Vector2.zero, new Vector2(0.3f, 1.05f), "Cristal");
            CodeSprites.Add(s.shadow, CodeSprites.Circle, new Color(0f, 0f, 0f, 0.35f), 39, Vector2.zero, new Vector2(1.2f, 0.3f), "Sombra");
            s.shadow.position = new Vector3(x, floor + 0.05f, 0f);
            s.t.position = new Vector3(x, s.y, 0f);
            shards.Add(s);
        }

        private void UpdateShards(float dt)
        {
            PlayerController player = PlayerController.Current;
            float floor = transform.position.y;
            foreach (Shard s in shards)
            {
                if (!s.alive) continue;
                if (s.wait > 0f) { s.wait -= dt; s.shadow.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 20f) * 0.1f); continue; }
                s.y -= 9f * dt;   // en el agua cae despacio
                s.t.position = new Vector3(s.x, s.y, 0f);
                bool hitFloor = s.y < floor + 0.5f;
                bool hitPlayer = player != null && player.Health.IsAlive && Mathf.Abs(player.Motor.Center.x - s.x) < 0.55f && Mathf.Abs(player.Motor.Center.y - s.y) < 0.9f;
                if (hitPlayer)
                {
                    DamageInfo info = DamageInfo.Create(9f, DamageKind.Contact, Team.Enemy, new Vector2(s.x, s.y));
                    info.knockback = 4f;
                    player.Health.ApplyDamage(info);
                }
                if (hitFloor || hitPlayer)
                {
                    s.alive = false;
                    s.t.gameObject.SetActive(false);
                    s.shadow.gameObject.SetActive(false);
                    Sfx.EffectAt("Coagulo_Disuelve", new Vector2(s.x, floor), 0.35f);
                }
            }
        }

        private void OnDied(EnemyHealth _)
        {
            dead = true;
            if (poses != null) poses.Show(BossPoses.Pose.Defeated);
            foreach (EnemyHealth m in minions) if (m != null && m.IsAlive) Destroy(m.gameObject);
            Camera cam = Camera.main;
            if (cam != null && cam.TryGetComponent(out GameCamera gc)) gc.Shake(0.3f, 0.3f);
            GameplayEvents.RaiseBossEnded();
            GameplayEvents.RaiseMessage("¡Biopelícula deshecha! El Proteus forma CRISTALES en la orina... y los cristales se juntan en PIEDRAS. " +
                "Adelante hay una muy grande.");
        }
    }
}
