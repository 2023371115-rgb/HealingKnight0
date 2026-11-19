using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// MINIJEFE: la PLACA DE ATEROMA. Una placa de colesterol enorme pegada a la pared de abajo que tapa casi toda la
    /// arteria (arriba no cabe ni encogido). La pantalla se detiene hasta vencerla.
    /// - Escupe gotas de colesterol AL RITMO del latido (en abanico, hacia ti).
    /// - Cada rato suelta CÉLULAS ESPUMOSAS (macrófagos que se llenaron de grasa) que te persiguen.
    /// - A la mitad de vida se ROMPE: las plaquetas tapan la herida con un COÁGULO encima de ella. Mientras el coágulo
    ///   exista, tus balas no le hacen nada: disuélvelo con ANTICOAGULANTE. Vuelve a salir al rato.
    /// Biología: así pasa un infarto: la placa se rompe, se forma un coágulo encima y tapa la arteria.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class AtheromaBoss : MonoBehaviour, IFlightBlocker
    {
        [SerializeField] private float width = 8f;
        [SerializeField] private float height = 4.3f;
        [SerializeField] private string bossName = "PLACA DE ATEROMA";
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private EnemyHealth foamCellPrefab;
        [SerializeField] private EnemyData clotData;
        [SerializeField] private EffectivenessTable table;
        [SerializeField] private float foamEvery = 7f;
        [SerializeField] private float clotRegrow = 9f;
        [SerializeField] private float ruptureAt = 0.55f;

        private EnemyHealth health;
        private Collider2D solid;
        private ComponentPool<Projectile> pool;
        private Transform visual;
        private bool started, ruptured, ended;
        private float foamTimer, clotTimer;
        private int beats;
        private EnemyHealth clot;

        // IFlightBlocker: la pantalla se detiene con la placa del lado derecho.
        public float BlockX => transform.position.x - 0.2f;
        public bool Blocking => health != null && health.IsAlive;
        public int Branch => 0;
        public float PassX => float.MaxValue;   // un jefe: la pantalla espera aunque te le pongas enfrente

        public void Configure(float plaqueWidth, float plaqueHeight, Projectile shot, EnemyHealth foamCell, EnemyData clotEnemy, EffectivenessTable effectiveness)
        {
            width = plaqueWidth;
            height = plaqueHeight;
            projectilePrefab = shot;
            foamCellPrefab = foamCell;
            clotData = clotEnemy;
            table = effectiveness;
        }

        private Vector2 Top => (Vector2)transform.position + new Vector2(-width * 0.12f, height * 0.85f);
        /// <summary>Donde se rompe: la cara de enfrente (la que te mira), así el coágulo recibe tus balas antes que la placa.</summary>
        private Vector2 Front => (Vector2)transform.position + new Vector2(-width * 0.44f, height * 0.4f);
        private static readonly Vector2 ClotSize = new Vector2(2f, 2.6f);

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            solid = GetComponent<Collider2D>();
            visual = transform.Find("Dibujo");
            health.Died += OnDied;
            Hurtbox box = GetComponent<Hurtbox>();
            if (box != null) box.SetBox(new Vector2(0f, height * 0.5f), new Vector2(width * 0.75f, height));
        }

        private void OnEnable()
        {
            FlightBlockers.Add(this);
            Heartbeat.Beat += OnBeat;
        }

        private void OnDisable()
        {
            FlightBlockers.Remove(this);
            Heartbeat.Beat -= OnBeat;
        }

        private void OnDestroy() => pool?.Clear();

        private void Update()
        {
            if (health == null || !health.IsAlive) return;
            float dt = Time.deltaTime;
            FlightRoute route = FlightRoute.Current;
            if (!started)
            {
                if (route == null || route.View.xMax < transform.position.x + width * 0.5f) return;
                started = true;
                foamTimer = foamEvery * 0.6f;
                GameplayEvents.RaiseBossStarted(bossName, health);
                // (Corto: el aviso de "se rompió" llega a los pocos segundos y NODO los pone en fila.)
                GameplayEvents.RaiseMessage("¡La PLACA DE ATEROMA! Casi tapa la arteria: rómpela (ANTIINFLAMATORIO) y esquiva su grasa.");
            }

            // Late con el corazón (la sangre la golpea).
            if (visual != null) visual.localScale = Vector3.one * (1f + Heartbeat.SurgeNow * 0.025f);

            foamTimer -= dt;
            if (foamTimer <= 0f)
            {
                foamTimer = ruptured ? foamEvery * 0.75f : foamEvery;
                SpawnFoamCells(ruptured ? 3 : 2);
            }

            // Se rompe a la mitad de la vida y le sale un coágulo encima.
            if (!ruptured && health.Health01 <= ruptureAt)
            {
                ruptured = true;
                GameplayEvents.RaiseMessage("¡Se ROMPIÓ y le salió un COÁGULO! Mientras esté, tus balas no le hacen nada: ¡ANTICOAGULANTE al coágulo!");
                Sfx.EffectAt("Coagulo_Disuelve", Front, 0.6f);
                GrowClot();
            }
            if (ruptured && clot == null && !health.Shelled)
            {
                clotTimer -= dt;
                if (clotTimer <= 0f) GrowClot();
            }
            if (ruptured && clot == null && health.Shelled) health.Shelled = false;
        }

        private void OnBeat(int count)
        {
            if (!started || health == null || !health.IsAlive) return;
            beats++;
            if (beats % 2 != 0) return;   // cada dos latidos (rota dispara más gotas, no más seguido)
            PlayerController player = PlayerController.Current;
            if (player == null || projectilePrefab == null) return;
            if (pool == null) pool = new ComponentPool<Projectile>(projectilePrefab, 6, 30);
            Vector2 from = Top;
            Vector2 dir = (player.Position + new Vector2(0f, 0.6f) - from).normalized;
            int shots = ruptured ? 5 : 3;
            float spread = ruptured ? 50f : 34f;
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
                    range = 30f,
                    damage = 9f,
                    knockback = 2f,
                    radius = 0.24f,
                    sourceTeam = Team.Enemy,
                    targetTeam = Team.Player,
                    color = new Color(1f, 0.85f, 0.3f),
                    groundMask = 0
                }, finished => pool.Release(finished));
            }
            Sfx.PlayAt(Sfx.EnemySound(health.Data, "Disparo"), from, CombatAudio.Enemies);
        }

        /// <summary>Células espumosas: salen de la placa y te persiguen un rato.</summary>
        private void SpawnFoamCells(int count)
        {
            if (foamCellPrefab == null) return;
            FlightRoute route = FlightRoute.Current;
            if (route == null) return;
            for (int i = 0; i < count; i++)
            {
                // Salen de la cara de enfrente de la placa, una arriba de otra.
                Vector2 world = (Vector2)transform.position + new Vector2(-width * 0.5f - 0.4f - i * 0.5f, 1.1f + i * 1.3f);
                EnemyHealth e = Instantiate(foamCellPrefab, new Vector3(world.x, world.y, 0f), Quaternion.identity);
                var spec = new FlightWaveSpec { prefab = foamCellPrefab, pattern = FlightPattern.Chase, speed = 2.3f + i * 0.3f, hoverTime = 7f };
                e.gameObject.AddComponent<FlightEnemy>().Begin(null, spec, world - route.CameraPosition, Random.value * 10f);
            }
        }

        /// <summary>El coágulo encima de la placa rota: la protege hasta que lo disuelvas.</summary>
        private void GrowClot()
        {
            if (clotData == null) return;
            var go = new GameObject("CoaguloSobreLaPlaca");
            go.transform.position = (Vector3)Front;
            var art = new GameObject("Dibujo").transform;
            art.SetParent(go.transform, false);
            // Plaquetas amontonadas en un óvalo, amarradas con hilos de fibrina.
            for (int ring = 0; ring < 2; ring++)
            {
                int n = ring == 0 ? 9 : 4;
                for (int i = 0; i < n; i++)
                {
                    float a = (i * 360f / n + ring * 30f) * Mathf.Deg2Rad;
                    float r = ring == 0 ? 1f : 0.45f;
                    Vector2 p = new Vector2(Mathf.Cos(a) * ClotSize.x * 0.34f * r, Mathf.Sin(a) * ClotSize.y * 0.36f * r);
                    CodeSprites.Add(art, CodeSprites.Circle, new Color(0.32f, 0.12f, 0.36f), 30, p, new Vector2(0.85f, 0.72f), "Plaqueta");
                    CodeSprites.Add(art, CodeSprites.Circle, new Color(0.82f, 0.62f, 0.92f), 31, p, new Vector2(0.7f, 0.58f), "Plaqueta");
                }
            }
            for (int i = 0; i < 4; i++)
            {
                SpriteRenderer f = CodeSprites.Add(art, CodeSprites.Square, new Color(1f, 0.95f, 0.8f, 0.85f), 32,
                    new Vector2(0f, -0.75f + i * 0.5f), new Vector2(ClotSize.x * 1.05f, 0.07f), "Fibrina");
                f.transform.localRotation = Quaternion.Euler(0f, 0f, -25f + i * 16f);
            }
            clot = go.AddComponent<EnemyHealth>();
            clot.Configure(clotData, table);
            go.AddComponent<Hurtbox>().SetBox(Vector2.zero, ClotSize);
            go.AddComponent<ClotGate>().Configure(false, ClotSize, null);
            clot.Died += OnClotGone;
            health.Shelled = true;
        }

        private void OnClotGone(EnemyHealth _)
        {
            clot = null;
            clotTimer = clotRegrow;
            if (ended) return;   // se deshizo junto con la placa
            if (health != null) health.Shelled = false;
            GameplayEvents.RaiseMessage("¡Coágulo fuera! Ahora sí: ¡dale a la placa!");
        }

        private void OnDied(EnemyHealth _)
        {
            if (ended) return;
            ended = true;
            if (solid != null) solid.enabled = false;
            if (clot != null) clot.Kill();
            GameplayEvents.RaiseBossEnded();
            GameplayEvents.RaiseMessage("¡La placa se deshizo y la sangre vuelve a pasar! En la vida real se previene comiendo bien, " +
                "haciendo ejercicio y sin fumar; las estatinas bajan el colesterol.");
        }
    }
}
