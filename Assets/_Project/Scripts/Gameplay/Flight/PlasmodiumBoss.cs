using System.Collections.Generic;
using HealingKnight.Core.Combat;
using HealingKnight.Core.Damage;
using HealingKnight.Core.Pooling;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.CameraSystem;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Flight
{
    /// <summary>
    /// JEFE de Arterias y venas: el PLASMODIUM (malaria), en la vena cava. La pantalla se detiene.
    /// - Un anillo de GLÓBULOS ROJOS da vueltas; el parásito está ESCONDIDO dentro de uno. Ese glóbulo tiene un
    ///   anillito morado adentro (así se ve de verdad en el microscopio: "forma de anillo").
    /// - Los glóbulos sanos atrapan tus balas. Si le pegas varias veces al infectado, lo revientas antes.
    /// - Cada rato le da FIEBRE al cuerpo: el glóbulo infectado REVIENTA solo, suelta MEROZOÍTOS (te persiguen) y el
    ///   Plasmodium queda al descubierto unos segundos: ¡ahí dale con ANTIPARASITARIO! Luego se mete a otro glóbulo.
    /// Biología: el Plasmodium (lo pasa el mosquito Anopheles) se mete a los glóbulos rojos, se multiplica y los
    /// revienta todos a la vez; por eso la malaria da fiebre cada 2 o 3 días y anemia.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class PlasmodiumBoss : MonoBehaviour, IFlightBlocker
    {
        [SerializeField] private string bossName = "PLASMODIUM";
        [SerializeField] private float ringRadius = 3.3f;
        [SerializeField] private int cellCount = 7;
        [SerializeField] private float cellRadius = 1.15f;
        [SerializeField] private float feverEvery = 11f;
        [SerializeField] private float exposedTime = 5.5f;
        [SerializeField] private int cracksToBurst = 4;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private EnemyHealth merozoitePrefab;

        private class RedCell
        {
            public Transform t;
            public Transform ringMark;
            public float angle, regrow, pop;
            public bool alive;
        }

        private readonly List<RedCell> cells = new List<RedCell>();
        private EnemyHealth health;
        private Hurtbox box;
        private Transform parasite;
        private ComponentPool<Projectile> pool;
        private int infected = -1, cracks, beats, bursts;
        private bool started, exposed, ended, toldHealthy, toldHide;
        private float orbit, feverTimer, exposeTimer;
        private Vector2 parasitePos;

        // La pantalla se detiene con el anillo un poco arriba del centro (con la cámara girada: arriba = adelante).
        public float BlockX
        {
            get
            {
                FlightRoute route = FlightRoute.Current;
                float hw = route != null ? route.HalfWidth : 5.4f;
                return transform.position.x - 2f + hw * 0.45f;
            }
        }
        public bool Blocking => health != null && health.IsAlive;
        public int Branch => 0;
        public float PassX => float.MaxValue;

        public void Configure(Projectile shot, EnemyHealth merozoite)
        {
            projectilePrefab = shot;
            merozoitePrefab = merozoite;
        }

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            box = GetComponent<Hurtbox>();
            health.Died += OnDied;
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

        private void Start()
        {
            for (int i = 0; i < cellCount; i++)
            {
                var c = new RedCell { angle = i * 360f / cellCount, alive = true };
                c.t = new GameObject("GloboRojo").transform;
                c.t.SetParent(transform, false);
                CodeSprites.Add(c.t, CodeSprites.Circle, new Color(0.35f, 0.02f, 0.06f), 10, Vector2.zero, Vector2.one * cellRadius * 2.1f, "Contorno");
                CodeSprites.Add(c.t, CodeSprites.Circle, new Color(0.88f, 0.16f, 0.2f), 11, Vector2.zero, Vector2.one * cellRadius * 2f, "Disco");
                CodeSprites.Add(c.t, CodeSprites.Circle, new Color(0.7f, 0.08f, 0.12f), 12, Vector2.zero, Vector2.one * cellRadius, "Centro");
                CodeSprites.Add(c.t, CodeSprites.Circle, new Color(1f, 0.6f, 0.6f, 0.6f), 13, new Vector2(-0.35f, 0.4f) * cellRadius, new Vector2(0.5f, 0.25f) * cellRadius, "Brillo");
                // El anillito morado (solo se ve en el infectado).
                c.ringMark = new GameObject("Anillo").transform;
                c.ringMark.SetParent(c.t, false);
                CodeSprites.Add(c.ringMark, CodeSprites.Ring, new Color(0.55f, 0.2f, 0.75f), 14, new Vector2(0.15f, -0.1f), Vector2.one * cellRadius * 0.75f, "AnilloMorado");
                CodeSprites.Add(c.ringMark, CodeSprites.Circle, new Color(0.4f, 0.1f, 0.55f), 15, new Vector2(0.15f + cellRadius * 0.3f, -0.1f), Vector2.one * 0.22f, "Punto");
                c.ringMark.gameObject.SetActive(false);
                cells.Add(c);
            }

            // El parásito al descubierto: un "esquizonte" morado lleno de merozoítos, con ojos enojados.
            parasite = new GameObject("Plasmodium").transform;
            parasite.SetParent(transform, false);
            CodeSprites.Add(parasite, CodeSprites.Glow, new Color(0.7f, 0.3f, 1f, 0.5f), 16, Vector2.zero, Vector2.one * 3.4f, "Aura");
            CodeSprites.Add(parasite, CodeSprites.Circle, new Color(0.25f, 0.05f, 0.35f), 17, Vector2.zero, Vector2.one * 1.9f, "Contorno");
            CodeSprites.Add(parasite, CodeSprites.Circle, new Color(0.62f, 0.3f, 0.85f), 18, Vector2.zero, Vector2.one * 1.72f, "Cuerpo");
            for (int i = 0; i < 9; i++)
            {
                float a = i * 40f * Mathf.Deg2Rad;
                CodeSprites.Add(parasite, CodeSprites.Circle, new Color(0.85f, 0.6f, 1f), 19, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.55f, Vector2.one * 0.34f, "Merozoito");
            }
            foreach (int side in new[] { -1, 1 })
            {
                CodeSprites.Add(parasite, CodeSprites.Circle, Color.white, 20, new Vector2(0.15f, side * 0.28f), new Vector2(0.38f, 0.34f), "Ojo");
                CodeSprites.Add(parasite, CodeSprites.Circle, new Color(0.15f, 0f, 0.2f), 21, new Vector2(0.2f, side * 0.26f), Vector2.one * 0.18f, "Pupila");
            }
            parasite.gameObject.SetActive(false);
            if (box != null) box.enabled = false;
            health.Shelled = true;
            Infect(Random.Range(0, cells.Count));
        }

        private Vector2 CellPos(RedCell c) => (Vector2)transform.position + new Vector2(Mathf.Cos((c.angle + orbit) * Mathf.Deg2Rad),
            Mathf.Sin((c.angle + orbit) * Mathf.Deg2Rad)) * ringRadius;

        private void Infect(int index)
        {
            if (infected >= 0 && infected < cells.Count) cells[infected].ringMark.gameObject.SetActive(false);
            infected = index;
            cracks = 0;
            if (infected >= 0) cells[infected].ringMark.gameObject.SetActive(true);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            FlightRoute route = FlightRoute.Current;
            if (ended || route == null) return;
            if (!started)
            {
                if (route.View.xMax < transform.position.x - ringRadius) return;
                started = true;
                feverTimer = feverEvery * 0.8f;
                GameplayEvents.RaiseBossStarted(bossName, health);
                GameplayEvents.RaiseMessage("¡El PLASMODIUM (malaria)! Está ESCONDIDO dentro de un glóbulo rojo: el que tiene un ANILLITO morado adentro. " +
                    "Pégale a ese o espera la FIEBRE: cuando reviente, ¡ANTIPARASITARIO!");
            }

            orbit += dt * (exposed ? 40f : 24f);
            float beat = 1f + Heartbeat.SurgeNow * 0.05f;
            PlayerController player = PlayerController.Current;
            ShipFlight ship = player != null ? player.Flight : null;

            for (int i = 0; i < cells.Count; i++)
            {
                RedCell c = cells[i];
                if (!c.alive)
                {
                    // Se revienta (se hace grande y se apaga) y luego la médula ósea manda uno nuevo.
                    c.pop += dt;
                    c.t.localScale = Vector3.one * (1f + c.pop * 2f);
                    if (c.pop > 0.25f) c.t.gameObject.SetActive(false);
                    c.regrow -= dt;
                    if (c.regrow <= 0f) { c.alive = true; c.pop = 0f; c.t.gameObject.SetActive(true); }
                    continue;
                }
                Vector2 p = CellPos(c);
                c.t.position = new Vector3(p.x, p.y, 0f);
                c.t.localScale = Vector3.one * beat;
                AbsorbBullets(i, p);
                // Te empujan suave si te les encimas.
                if (ship != null && player.Health.IsAlive)
                {
                    Vector2 d = (player.Position + new Vector2(0f, 0.55f)) - p;
                    float reach = cellRadius + 0.6f;
                    if (d.sqrMagnitude < reach * reach && d.sqrMagnitude > 0.0001f) ship.AddPush(d.normalized * 10f * (1f - d.magnitude / reach));
                }
            }

            if (exposed)
            {
                exposeTimer -= dt;
                // El parásito flota donde reventó el glóbulo, girando con el anillo.
                parasitePos = Vector2.Lerp(parasitePos, (Vector2)transform.position + (parasitePos - (Vector2)transform.position).normalized * ringRadius * 0.6f, dt);
                parasite.position = new Vector3(parasitePos.x, parasitePos.y, 0f);
                parasite.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 6f) * 0.06f);
                if (box != null) box.SetBox(parasitePos - (Vector2)transform.position, new Vector2(1.7f, 1.7f));
                if (exposeTimer <= 0f) Hide();
            }
            else
            {
                feverTimer -= dt;
                if (feverTimer <= 0f || cracks >= cracksToBurst) Burst(feverTimer <= 0f);
            }
        }

        /// <summary>Los glóbulos atrapan las balas; las tuyas que le pegan al infectado lo van rompiendo.</summary>
        private void AbsorbBullets(int index, Vector2 p)
        {
            float r2 = cellRadius * cellRadius;
            for (int i = Projectile.Flying.Count - 1; i >= 0; i--)
            {
                Projectile pr = Projectile.Flying[i];
                if (pr == null || ((Vector2)pr.Position - p).sqrMagnitude > r2) continue;
                bool mine = pr.SourceTeam == Team.Player;
                pr.Consume();
                if (!mine) continue;
                if (index == infected && !exposed) cracks++;
                else if (!toldHealthy)
                {
                    toldHealthy = true;
                    GameplayEvents.RaiseMessage("Ese glóbulo está SANO: te tapa las balas. Busca el del ANILLITO morado.");
                }
            }
        }

        /// <summary>El glóbulo infectado revienta (fiebre): sale el parásito y sus merozoítos.</summary>
        private void Burst(bool fever)
        {
            if (infected < 0 || infected >= cells.Count) { Hide(); return; }
            RedCell c = cells[infected];
            parasitePos = CellPos(c);
            c.alive = false;
            c.pop = 0f;
            c.regrow = 6f;
            c.ringMark.gameObject.SetActive(false);
            infected = -1;
            exposed = true;
            exposeTimer = exposedTime;
            bursts++;
            parasite.gameObject.SetActive(true);
            parasite.position = new Vector3(parasitePos.x, parasitePos.y, 0f);
            if (box != null) box.enabled = true;
            health.Shelled = false;
            Sfx.EffectAt("Coagulo_Disuelve", parasitePos, 1f);
            Camera cam = Camera.main;
            if (cam != null && cam.TryGetComponent(out GameCamera gc)) gc.Shake(0.25f, 0.3f);
            SpeedBanner.Show(fever ? "¡FIEBRE!" : "¡LO REVENTASTE!", new Color(1f, 0.6f, 0.4f), 1.2f);
            if (bursts == 1)
                GameplayEvents.RaiseMessage("¡El glóbulo REVENTÓ y salieron MEROZOÍTOS! (Eso da la FIEBRE de la malaria.) El Plasmodium está al descubierto: ¡dale!");
            SpawnMerozoites(health.Health01 < 0.5f ? 4 : 3);
        }

        /// <summary>Se mete a otro glóbulo sano y se vuelve a esconder.</summary>
        private void Hide()
        {
            exposed = false;
            parasite.gameObject.SetActive(false);
            if (box != null) box.enabled = false;
            health.Shelled = true;
            feverTimer = health.Health01 < 0.5f ? feverEvery * 0.75f : feverEvery;
            var healthy = new List<int>();
            for (int i = 0; i < cells.Count; i++) if (cells[i].alive) healthy.Add(i);
            if (healthy.Count == 0)
            {
                // Sin glóbulos: la médula manda uno ya.
                cells[0].alive = true; cells[0].pop = 0f; cells[0].t.gameObject.SetActive(true);
                healthy.Add(0);
            }
            Infect(healthy[Random.Range(0, healthy.Count)]);
            if (!toldHide)
            {
                toldHide = true;
                GameplayEvents.RaiseMessage("Se metió a OTRO glóbulo. Busca el anillito morado... o espera la próxima fiebre.");
            }
        }

        private void SpawnMerozoites(int count)
        {
            FlightRoute route = FlightRoute.Current;
            if (merozoitePrefab == null || route == null) return;
            for (int i = 0; i < count; i++)
            {
                float a = (i * 360f / count) * Mathf.Deg2Rad;
                Vector2 world = parasitePos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.8f;
                EnemyHealth e = Instantiate(merozoitePrefab, new Vector3(world.x, world.y, 0f), Quaternion.identity);
                var spec = new FlightWaveSpec { prefab = merozoitePrefab, pattern = FlightPattern.Chase, speed = 2.4f + i * 0.25f, hoverTime = 6f };
                e.gameObject.AddComponent<FlightEnemy>().Begin(null, spec, world - route.CameraPosition, Random.value * 10f);
            }
        }

        /// <summary>Al descubierto dispara esporas moradas al ritmo del latido (cada dos).</summary>
        private void OnBeat(int count)
        {
            if (!started || ended || !exposed || projectilePrefab == null) return;
            beats++;
            if (beats % 2 != 0) return;
            PlayerController player = PlayerController.Current;
            if (player == null) return;
            if (pool == null) pool = new ComponentPool<Projectile>(projectilePrefab, 6, 30);
            Vector2 dir = (player.Position + new Vector2(0f, 0.6f) - parasitePos).normalized;
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            const int shots = 5;
            const float spread = 60f;
            for (int i = 0; i < shots; i++)
            {
                float a = (baseAngle - spread * 0.5f + spread * i / (shots - 1)) * Mathf.Deg2Rad;
                Projectile p = pool.Get();
                p.Launch(new ProjectileShot
                {
                    origin = parasitePos,
                    direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a)),
                    speed = 5f,
                    range = 26f,
                    damage = 9f,
                    knockback = 2f,
                    radius = 0.22f,
                    sourceTeam = Team.Enemy,
                    targetTeam = Team.Player,
                    color = new Color(0.8f, 0.5f, 1f),
                    groundMask = 0
                }, finished => pool.Release(finished));
            }
            Sfx.PlayAt(Sfx.EnemySound(health.Data, "Disparo"), parasitePos, CombatAudio.Enemies);
        }

        private void OnDied(EnemyHealth _)
        {
            if (ended) return;
            ended = true;
            foreach (RedCell c in cells) if (c.ringMark != null) c.ringMark.gameObject.SetActive(false);
            GameplayEvents.RaiseBossEnded();
            GameplayEvents.RaiseMessage("¡Malaria vencida! En la vida real se previene con mosquiteros y repelente, y se cura con " +
                "medicamentos antipalúdicos que receta un médico. Los glóbulos rojos ya están a salvo.");
        }
    }
}
