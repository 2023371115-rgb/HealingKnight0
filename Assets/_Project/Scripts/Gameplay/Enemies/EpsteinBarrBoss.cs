using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Enemies
{
    /// <summary>
    /// JEFE del Bazo: el VIRUS DE EPSTEIN-BARR (mononucleosis, "la enfermedad del beso"). Flota sobre la arena.
    /// - Te ayudan 4 LINFOCITOS B: le disparan anticuerpos al virus.
    /// - Cada rato el virus apunta a uno (un rayo morado) y lo INFECTA: se vuelve morado y te ataca a TI.
    ///   Dispárale hasta curarlo (el ANTIVIRAL le hace más): al curarse, sus LINFOCITOS T se enojan y le pegan un
    ///   golpazo al virus.
    /// - A tus balas el virus casi no les hace caso (parcial): lo que de verdad le gana es el sistema inmune.
    /// - Dispara anillos de partículas virales; a media vida, más seguido.
    /// Biología: el VEB se mete justo a los linfocitos B. No hay medicina que lo cure: lo controlan los linfocitos T.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class EpsteinBarrBoss : MonoBehaviour
    {
        [SerializeField] private string bossName = "VIRUS DE EPSTEIN-BARR";
        [SerializeField] private Transform visual;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private EnemyData infectedData;
        [SerializeField] private EffectivenessTable table;
        [SerializeField] private float hover = 5.6f;
        [SerializeField] private float infectEvery = 7f;
        [SerializeField] private float shotEvery = 2.6f;
        [SerializeField] private float strikeDamage = 50f;
        [SerializeField] private int allies = 4;

        private EnemyHealth health;
        private ComponentPool<Projectile> pool;
        private readonly List<InfectableLymphocyte> cells = new List<InfectableLymphocyte>();
        private float floorY, t, infectTimer, shotTimer, aimTimer;
        private InfectableLymphocyte aiming;
        private LineRenderer beam;
        private bool dead, toldHalf, toldAll;
        private int cures;

        public EnemyData InfectedData => infectedData;
        public EffectivenessTable Table => table;

        private void Awake() => health = GetComponent<EnemyHealth>();
        private void OnEnable() => health.Died += OnDied;
        private void OnDisable() => health.Died -= OnDied;

        private void OnDestroy()
        {
            if (!dead) GameplayEvents.RaiseBossEnded();
            pool?.Clear();
            foreach (InfectableLymphocyte c in cells) if (c != null) Destroy(c.gameObject);
        }

        private void Start()
        {
            floorY = transform.position.y;
            if (projectilePrefab != null) pool = new ComponentPool<Projectile>(projectilePrefab, 10, 40);
            GameplayEvents.RaiseBossStarted(bossName, health);
            GameplayEvents.RaiseMessage("¡El virus de EPSTEIN-BARR (mononucleosis)! Tus LINFOCITOS B te ayudan... pero el virus los INFECTA. " +
                "Si uno se pone morado, ¡CÚRALO a balazos (antiviral) y sus linfocitos T le pegarán al virus!");
            float minX = WalkableArea.MinX + 2.5f, maxX = WalkableArea.MaxX - 2.5f;
            for (int i = 0; i < allies; i++)
            {
                float x = Mathf.Lerp(minX, maxX, (i + 0.5f) / allies);
                var go = new GameObject("LinfocitoB");
                go.transform.position = new Vector3(x, floorY + 2f, 0f);
                InfectableLymphocyte c = go.AddComponent<InfectableLymphocyte>();
                c.Setup(this, new Vector2(x, floorY + 1.8f + (i % 2) * 1.2f));
                cells.Add(c);
            }
            var beamGo = new GameObject("RayoInfeccion");
            beamGo.transform.SetParent(transform, false);
            beam = beamGo.AddComponent<LineRenderer>();
            beam.positionCount = 2;
            beam.startWidth = 0.18f;
            beam.endWidth = 0.08f;
            beam.material = new Material(Shader.Find("Sprites/Default"));
            beam.startColor = new Color(0.8f, 0.4f, 1f, 0.9f);
            beam.endColor = new Color(0.8f, 0.4f, 1f, 0.4f);
            beam.sortingOrder = 50;
            beam.enabled = false;
            infectTimer = infectEvery * 0.6f;
            shotTimer = shotEvery;
        }

        private void Update()
        {
            if (dead) return;
            float dt = Time.deltaTime;
            t += dt;
            bool half = health.Health01 <= 0.5f;
            if (half && !toldHalf)
            {
                toldHalf = true;
                GameplayEvents.RaiseMessage("¡El virus se desespera! Infecta más rápido. Sigue curando a tus linfocitos.");
            }

            // Flota de un lado a otro (en forma de ocho), arriba de la arena.
            float minX = WalkableArea.MinX + 3f, maxX = WalkableArea.MaxX - 3f;
            float x = Mathf.Lerp(minX, maxX, 0.5f + 0.5f * Mathf.Sin(t * 0.45f));
            float y = floorY + hover + Mathf.Sin(t * 0.9f) * 1.2f;
            transform.position = new Vector3(x, y, 0f);
            if (visual != null)
            {
                visual.localRotation = Quaternion.Euler(0f, 0f, t * 30f);
                visual.localScale = Vector3.one * (1f + Mathf.Sin(t * 4f) * 0.05f);
            }

            shotTimer -= dt;
            if (shotTimer <= 0f)
            {
                shotTimer = half ? shotEvery * 0.7f : shotEvery;
                Ring(half ? 9 : 7);
            }

            if (aiming != null)
            {
                // Rayo de aviso: apunta un segundo y luego infecta.
                aimTimer -= dt;
                beam.enabled = true;
                beam.SetPosition(0, transform.position);
                beam.SetPosition(1, aiming.transform.position);
                if (aimTimer <= 0f)
                {
                    beam.enabled = false;
                    if (aiming != null && !aiming.Infected) aiming.Infect();
                    aiming = null;
                    Sfx.EffectAt("Colmena_Enjambre", transform.position, 0.7f);
                    CheckAllInfected();
                }
                return;
            }
            infectTimer -= dt;
            if (infectTimer <= 0f)
            {
                infectTimer = half ? infectEvery * 0.7f : infectEvery;
                var healthy = cells.FindAll(c => c != null && !c.Infected);
                if (healthy.Count > 0)
                {
                    aiming = healthy[Random.Range(0, healthy.Count)];
                    aimTimer = 1f;
                }
            }
        }

        private void CheckAllInfected()
        {
            if (toldAll) return;
            if (cells.TrueForAll(c => c == null || c.Infected))
            {
                toldAll = true;
                GameplayEvents.RaiseMessage("¡Todos tus linfocitos están infectados! Cúralos con ANTIVIRAL: sin ellos no le puedes ganar.");
            }
        }

        private void Ring(int count)
        {
            if (pool == null) return;
            float start = Random.value * 360f;
            for (int i = 0; i < count; i++)
            {
                float a = (start + i * 360f / count) * Mathf.Deg2Rad;
                Projectile p = pool.Get();
                p.Launch(new ProjectileShot
                {
                    origin = transform.position, direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), speed = 4.5f, range = 22f,
                    damage = 9f, knockback = 2.5f, radius = 0.26f, sourceTeam = Team.Enemy, targetTeam = Team.Player,
                    color = new Color(0.75f, 0.5f, 1f), groundMask = 1 << PhysicsLayers.Ground
                }, done => pool.Release(done));
            }
            Sfx.PlayAt(Sfx.EnemySound(health.Data, "Disparo"), transform.position, CombatAudio.Enemies);
        }

        /// <summary>Un linfocito se curó: sus linfocitos T atacan al virus.</summary>
        public void Cured(InfectableLymphocyte cell)
        {
            cures++;
            if (dead) return;
            var go = new GameObject("LinfocitoT");
            go.transform.position = cell.transform.position;
            go.AddComponent<TCellStrike>().Launch(health, strikeDamage);
            if (cures == 1)
                GameplayEvents.RaiseMessage("¡Lo curaste! Llegaron los LINFOCITOS T y le dieron un golpazo al virus. Así es como el cuerpo le gana de verdad.");
        }

        private void OnDied(EnemyHealth _)
        {
            dead = true;
            if (beam != null) beam.enabled = false;
            foreach (InfectableLymphocyte c in cells) if (c != null) c.CureSilently();
            GameplayEvents.RaiseBossEnded();
            GameplayEvents.RaiseMessage("¡Virus controlado! En la vida real la mononucleosis no tiene medicina que la cure: descanso, líquidos y " +
                "tu sistema inmune. No compartas vasos ni besos si alguien la tiene.");
        }
    }

    /// <summary>
    /// LINFOCITO B de la arena del Epstein-Barr. Sano: flota en su lugar y le dispara anticuerpos al virus. Infectado:
    /// se pone morado, te persigue un poco y te dispara; tiene una "capa" de infección (EnemyHealth) que hay que bajarle
    /// a balazos para curarlo.
    /// </summary>
    public class InfectableLymphocyte : MonoBehaviour
    {
        private EpsteinBarrBoss boss;
        private Vector2 home;
        private SpriteRenderer body, nucleus;
        private Transform eyes, angry;
        private EnemyHealth infection;
        private float timer, seed;

        public bool Infected => infection != null;

        public void Setup(EpsteinBarrBoss owner, Vector2 homePos)
        {
            boss = owner;
            home = homePos;
        }

        private void Start()
        {
            seed = Random.value * 10f;
            timer = 1.6f + seed * 0.1f;
            Sprite c = CodeSprites.Circle;
            CodeSprites.Add(transform, c, new Color(0.2f, 0.1f, 0.35f), 30, Vector2.zero, Vector2.one * 1.05f, "Contorno");
            body = CodeSprites.Add(transform, c, new Color(0.65f, 0.75f, 1f), 31, Vector2.zero, Vector2.one * 0.92f, "Cuerpo");
            nucleus = CodeSprites.Add(transform, c, new Color(0.45f, 0.4f, 0.9f), 32, new Vector2(0.05f, -0.04f), Vector2.one * 0.6f, "Nucleo");
            eyes = new GameObject("Ojos").transform;
            eyes.SetParent(transform, false);
            foreach (float ex in new[] { -0.13f, 0.15f })
            {
                CodeSprites.Add(eyes, c, Color.white, 33, new Vector2(ex, 0.08f), new Vector2(0.15f, 0.18f), "Ojo");
                CodeSprites.Add(eyes, c, new Color(0.15f, 0.1f, 0.3f), 34, new Vector2(ex + 0.01f, 0.06f), new Vector2(0.07f, 0.1f), "Pupila");
            }
            angry = new GameObject("Enojado").transform;
            angry.SetParent(transform, false);
            foreach (float ex in new[] { -0.13f, 0.15f })
                CodeSprites.Add(angry, c, new Color(1f, 0.2f, 0.25f), 35, new Vector2(ex, 0.07f), new Vector2(0.12f, 0.12f), "OjoRojo");
            angry.gameObject.SetActive(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime, tt = Time.time + seed;
            PlayerController player = PlayerController.Current;
            Vector2 target = home + new Vector2(Mathf.Sin(tt * 0.8f) * 0.8f, Mathf.Sin(tt * 1.7f) * 0.3f);
            if (Infected && player != null)
                target = Vector2.Lerp(home, player.Position + new Vector2(Mathf.Sin(tt) * 3f, 2.5f), 0.35f);
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-3f * dt));
            if (infection != null) infection.transform.position = transform.position;

            timer -= dt;
            if (timer > 0f) return;
            if (!Infected)
            {
                timer = 1.6f;
                if (boss == null) return;
                EnemyHealth h = boss.GetComponent<EnemyHealth>();
                if (h == null || !h.IsAlive) return;
                var go = new GameObject("Anticuerpo");
                go.transform.position = transform.position;
                go.AddComponent<Antibody>().Launch(h, 4f);
            }
            else if (player != null && player.Health.IsAlive)
            {
                timer = 1.7f;
                Vector2 dir = (player.Position + Vector2.up * 0.8f - (Vector2)transform.position).normalized;
                var go = new GameObject("Particula");
                go.transform.position = transform.position;
                go.AddComponent<SimpleShot>().Launch(dir * 6f, 8f);
            }
        }

        /// <summary>El virus lo infectó: se pone morado y le sale una capa de infección que hay que bajarle.</summary>
        public void Infect()
        {
            if (Infected || boss == null || boss.InfectedData == null) return;
            var go = new GameObject("Infeccion");
            go.transform.position = transform.position;
            go.layer = PhysicsLayers.Enemy;
            // (un dibujo vacío para el destello: el dibujo de verdad es el del linfocito)
            infection = go.AddComponent<EnemyHealth>();
            infection.Configure(boss.InfectedData, boss.Table);
            go.AddComponent<Hurtbox>().SetBox(Vector2.zero, new Vector2(1.1f, 1.1f));
            infection.Died += OnCured;
            body.color = new Color(0.7f, 0.4f, 0.85f);
            nucleus.color = new Color(0.4f, 0.1f, 0.5f);
            eyes.gameObject.SetActive(false);
            angry.gameObject.SetActive(true);
            timer = 1.2f;
        }

        private void OnCured(EnemyHealth _)
        {
            infection = null;
            Heal();
            if (boss != null) boss.Cured(this);
        }

        public void CureSilently()
        {
            if (infection != null)
            {
                infection.Died -= OnCured;
                Destroy(infection.gameObject);
                infection = null;
            }
            Heal();
        }

        private void Heal()
        {
            if (body == null) return;
            body.color = new Color(0.65f, 0.75f, 1f);
            nucleus.color = new Color(0.45f, 0.4f, 0.9f);
            eyes.gameObject.SetActive(true);
            angry.gameObject.SetActive(false);
            timer = 1.6f;
        }
    }

    /// <summary>LINFOCITO T que sale disparado contra el virus y le pega un golpazo al llegar.</summary>
    public class TCellStrike : MonoBehaviour
    {
        private EnemyHealth target;
        private float damage, life = 3f;

        public void Launch(EnemyHealth enemy, float amount)
        {
            target = enemy;
            damage = amount;
            Sprite c = CodeSprites.Circle;
            CodeSprites.Add(transform, CodeSprites.Glow, new Color(0.4f, 1f, 0.6f, 0.5f), 36, Vector2.zero, Vector2.one * 2f, "Brillo");
            CodeSprites.Add(transform, c, new Color(0.1f, 0.3f, 0.15f), 37, Vector2.zero, Vector2.one * 0.95f, "Contorno");
            CodeSprites.Add(transform, c, new Color(0.55f, 0.95f, 0.6f), 38, Vector2.zero, Vector2.one * 0.82f, "Cuerpo");
            CodeSprites.Add(transform, c, new Color(0.25f, 0.6f, 0.35f), 39, new Vector2(0.05f, -0.04f), Vector2.one * 0.5f, "Nucleo");
            foreach (float ex in new[] { -0.12f, 0.14f })
                CodeSprites.Add(transform, c, Color.white, 40, new Vector2(ex, 0.1f), new Vector2(0.14f, 0.1f), "OjoEnojado");
            Sfx.Effect("Jugador_Dash", 0.6f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f || target == null || !target.IsAlive) { Destroy(gameObject); return; }
            Vector2 p = transform.position, d = (Vector2)target.transform.position - p;
            if (d.magnitude < 0.8f)
            {
                target.ApplyDamage(DamageInfo.Create(damage, DamageKind.Projectile, Team.Player, p));
                Sfx.EffectAt("Coagulo_Disuelve", p, 0.8f);
                Destroy(gameObject);
                return;
            }
            transform.position = p + d.normalized * 14f * dt;
            transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 20f) * 0.08f);
        }
    }

    /// <summary>Bala sencilla (de un linfocito infectado): vuela derecho y lastima al jugador.</summary>
    public class SimpleShot : MonoBehaviour
    {
        private Vector2 velocity;
        private float damage, life = 3.5f;

        public void Launch(Vector2 v, float amount)
        {
            velocity = v;
            damage = amount;
            CodeSprites.Add(transform, CodeSprites.Circle, new Color(0.3f, 0.1f, 0.4f), 30, Vector2.zero, Vector2.one * 0.42f, "Contorno");
            CodeSprites.Add(transform, CodeSprites.Circle, new Color(0.8f, 0.5f, 1f), 31, Vector2.zero, Vector2.one * 0.32f, "Bala");
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f) { Destroy(gameObject); return; }
            transform.position += (Vector3)(velocity * dt);
            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            Hurtbox box = player.GetComponent<Hurtbox>();
            if (box == null || !box.OverlapsCircle(transform.position, 0.25f)) return;
            DamageInfo info = DamageInfo.Create(damage, DamageKind.Projectile, Team.Enemy, transform.position);
            info.knockback = 2f;
            box.Damageable.ApplyDamage(info);
            Destroy(gameObject);
        }
    }
}
