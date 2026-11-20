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

        private BossPoses poses;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            poses = GetComponent<BossPoses>();
            if (poses != null && !poses.HasArt) poses = null;
        }

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
                // La figura gira (es una cápside); el dibujo solo se mece para que su cara no dé vueltas.
                visual.localRotation = Quaternion.Euler(0f, 0f, poses != null ? Mathf.Sin(t * 1.3f) * 8f : t * 30f);
                visual.localScale = Vector3.one * (1f + Mathf.Sin(t * 4f) * 0.05f);
            }
            if (poses != null) poses.SetAngry(half);

            shotTimer -= dt;
            if (shotTimer <= 0f)
            {
                shotTimer = half ? shotEvery * 0.7f : shotEvery;
                Ring(half ? 9 : 7);
                if (poses != null) poses.Show(BossPoses.Pose.Attack);
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
            if (poses != null) poses.Show(BossPoses.Pose.Defeated);
            if (beam != null) beam.enabled = false;
            foreach (InfectableLymphocyte c in cells) if (c != null) c.CureSilently();
            GameplayEvents.RaiseBossEnded();
            GameplayEvents.RaiseMessage("¡Virus controlado! En la vida real la mononucleosis no tiene medicina que la cure: descanso, líquidos y " +
                "tu sistema inmune. No compartas vasos ni besos si alguien la tiene.");
        }
    }
}
