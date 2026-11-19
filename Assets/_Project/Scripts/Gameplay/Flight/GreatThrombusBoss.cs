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
    /// JEFE DEL SISTEMA CIRCULATORIO: EL GRAN TROMBO. Un coágulo enorme que se soltó del corazón y sube por la carótida
    /// rumbo al CEREBRO. Va siempre adelante de ti (arriba de la pantalla) y lo persigues.
    /// - Suelta ÉMBOLOS (pedazos de coágulo) que bajan hacia ti; con la mitad de vida suelta REDES de fibrina (una fila
    ///   con un solo hueco) y al final le da TAQUICARDIA al corazón (todo más rápido).
    /// - Solo el ANTICOAGULANTE lo deshace. Se va haciendo chico.
    /// - Hay una barra al lado: si el coágulo llega al cerebro (la pantalla llega a 'brainX') antes de deshacerlo, es una
    ///   EMBOLIA y regresas al último punto de guardado (el coágulo NO se cura: sigues donde te quedaste).
    /// Biología: un coágulo que se suelta es un ÉMBOLO; si tapa una arteria del cerebro causa un infarto cerebral
    /// (embolia). Con arritmias como la del Chagas se forman coágulos dentro del corazón.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class GreatThrombusBoss : MonoBehaviour
    {
        [SerializeField] private string bossName = "EL GRAN TROMBO";
        [SerializeField] private float startX = 20f, brainX = 680f;
        [SerializeField] private EnemyHealth emboliPrefab;
        [SerializeField] private float emboliEvery = 2.4f;
        [SerializeField] private float netEvery = 9f;

        private EnemyHealth health;
        private Hurtbox box;
        private Transform art, meter, marker;
        private bool started, ended, toldHalf, toldLast, toldWarn;
        private float emboliTimer, netTimer, t, embolismCooldown;
        private Vector2 offset;

        public void Configure(float fromX, float brain, EnemyHealth emboli)
        {
            startX = fromX;
            brainX = brain;
            emboliPrefab = emboli;
        }

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            box = GetComponent<Hurtbox>();
            health.Died += OnDied;
        }

        private void Start()
        {
            // El coágulo: plaquetas moradas, glóbulos rojos atrapados y fibrina; con cara enojada.
            art = new GameObject("Dibujo").transform;
            art.SetParent(transform, false);
            var rnd = new System.Random(77);
            CodeSprites.Add(art, CodeSprites.Glow, new Color(0.6f, 0.2f, 0.4f, 0.4f), 2, Vector2.zero, Vector2.one * 6.5f, "Aura");
            for (int i = 0; i < 46; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = (float)rnd.NextDouble() * 1.7f;
                Vector2 p = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 1.15f);
                bool red = i % 4 == 0;
                float s = 0.6f + (float)rnd.NextDouble() * 0.5f;
                CodeSprites.Add(art, CodeSprites.Circle, red ? new Color(0.35f, 0.02f, 0.06f) : new Color(0.3f, 0.1f, 0.34f), 3, p, new Vector2(s * 1.2f, s), "Contorno");
                CodeSprites.Add(art, CodeSprites.Circle, red ? new Color(0.85f, 0.18f, 0.22f) : new Color(0.8f, 0.6f, 0.9f), 4, p, new Vector2(s, s * 0.82f), red ? "GloboAtrapado" : "Plaqueta");
            }
            for (int i = 0; i < 9; i++)
            {
                SpriteRenderer f = CodeSprites.Add(art, CodeSprites.Square, new Color(1f, 0.95f, 0.8f, 0.85f), 5,
                    new Vector2(0f, -1.6f + i * 0.4f), new Vector2(4.2f, 0.07f), "Fibrina");
                f.transform.localRotation = Quaternion.Euler(0f, 0f, ((float)rnd.NextDouble() - 0.5f) * 70f);
            }
            // Cara (mira hacia abajo de la pantalla, hacia ti: en el mundo, hacia -X).
            foreach (int side in new[] { -1, 1 })
            {
                CodeSprites.Add(art, CodeSprites.Circle, Color.white, 6, new Vector2(-0.9f, side * 0.55f), new Vector2(0.6f, 0.55f), "Ojo");
                CodeSprites.Add(art, CodeSprites.Circle, new Color(0.15f, 0f, 0.1f), 7, new Vector2(-1f, side * 0.52f), Vector2.one * 0.28f, "Pupila");
            }
            if (box != null) box.SetBox(Vector2.zero, new Vector2(3.4f, 3.8f));
            emboliTimer = emboliEvery;
            netTimer = netEvery;
            BuildMeter();
            art.gameObject.SetActive(false);
            if (box != null) box.enabled = false;
        }

        /// <summary>Barra al lado de la pantalla: del corazón (abajo) al cerebro (arriba), con el coágulo marcado.</summary>
        private void BuildMeter()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            meter = new GameObject("BarraCerebro").transform;
            meter.SetParent(cam.transform, false);
            float h = cam.orthographicSize;
            meter.localPosition = new Vector3(h * cam.aspect - 0.9f, 0f, 5f);
            CodeSprites.Add(meter, CodeSprites.Square, new Color(0.1f, 0.02f, 0.05f, 0.8f), 940, Vector2.zero, new Vector2(0.5f, h * 1.5f), "Fondo");
            CodeSprites.Add(meter, CodeSprites.Square, new Color(0.9f, 0.3f, 0.35f, 0.8f), 941, Vector2.zero, new Vector2(0.22f, h * 1.42f), "Arteria");
            // Arriba el cerebro (rosita) y abajo el corazón (rojo).
            CodeSprites.Add(meter, CodeSprites.Circle, new Color(1f, 0.72f, 0.8f), 942, new Vector2(0f, h * 0.8f), Vector2.one * 0.9f, "Cerebro");
            CodeSprites.Add(meter, CodeSprites.Circle, new Color(0.9f, 0.15f, 0.2f), 942, new Vector2(0f, -h * 0.8f), Vector2.one * 0.75f, "Corazon");
            marker = CodeSprites.Add(meter, CodeSprites.Circle, new Color(0.65f, 0.3f, 0.75f), 943, Vector2.zero, Vector2.one * 0.6f, "Coagulo").transform;
            meter.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (ended) return;
            float dt = Time.deltaTime;
            FlightRoute route = FlightRoute.Current;
            if (route == null) return;
            t += dt;
            if (!started)
            {
                if (!route.Launched || route.CameraPosition.x < startX) return;
                started = true;
                art.gameObject.SetActive(true);
                if (box != null) box.enabled = true;
                if (meter != null) meter.gameObject.SetActive(true);
                GameplayEvents.RaiseBossStarted(bossName, health);
                GameplayEvents.RaiseMessage("¡EL GRAN TROMBO! Va rumbo al CEREBRO: si llega, es una EMBOLIA. Solo lo deshace el ANTICOAGULANTE. " +
                    "Mira la barra: ¡alcánzalo antes del cerebro!");
            }

            // Va arriba de la pantalla (adelante), meciéndose de lado a lado.
            float ahead = route.HalfWidth - 2.1f;
            offset = new Vector2(ahead + Mathf.Sin(t * 0.8f) * 0.4f, Mathf.Sin(t * 0.55f) * 1.6f);
            Vector2 pos = route.CameraPosition + offset;
            transform.position = new Vector3(pos.x, pos.y, 0f);
            float size = Mathf.Lerp(0.55f, 1f, health.Health01) * (1f + Heartbeat.SurgeNow * 0.05f);
            art.localScale = Vector3.one * size;
            art.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.7f) * 12f);
            if (box != null) box.SetBox(Vector2.zero, new Vector2(3.4f, 3.8f) * Mathf.Max(0.6f, size));

            float progress = Mathf.InverseLerp(startX, brainX, route.CameraPosition.x);
            if (marker != null) marker.localPosition = new Vector3(0f, Mathf.Lerp(-0.8f, 0.8f, progress) * Camera.main.orthographicSize, 0f);
            if (!toldWarn && progress > 0.8f)
            {
                toldWarn = true;
                GameplayEvents.RaiseMessage("¡Ya casi llega al CEREBRO! ¡ANTICOAGULANTE, rápido!");
                SpeedBanner.Show("¡CASI EN EL CEREBRO!", new Color(1f, 0.6f, 0.7f), 1.6f);
            }

            // Fases: a media vida, redes de fibrina; al final, taquicardia.
            if (!toldHalf && health.Health01 <= 0.6f)
            {
                toldHalf = true;
                GameplayEvents.RaiseMessage("¡Suelta REDES de fibrina! Busca el HUECO o disuélvelas.");
            }
            if (!toldLast && health.Health01 <= 0.3f)
            {
                toldLast = true;
                if (Heartbeat.Current != null) Heartbeat.Current.Tachycardia = 30f;
                route.SpeedMultiplier = 1.15f;
                SpeedBanner.Show("¡TAQUICARDIA!", new Color(1f, 0.5f, 0.5f), 1.4f);
                GameplayEvents.RaiseMessage("¡TAQUICARDIA! El corazón late rapidísimo y el coágulo sube más rápido. ¡Ya casi lo deshaces!");
            }

            emboliTimer -= dt;
            if (emboliTimer <= 0f)
            {
                emboliTimer = health.Health01 <= 0.3f ? emboliEvery * 0.65f : emboliEvery;
                DropEmboli(route, health.Health01 <= 0.6f ? 3 : 2);
            }
            if (toldHalf)
            {
                netTimer -= dt;
                if (netTimer <= 0f) { netTimer = netEvery; FibrinNet(route); }
            }

            // ¿Llegó al cerebro?
            if (embolismCooldown > 0f) embolismCooldown -= dt;
            if (route.CameraPosition.x >= brainX && embolismCooldown <= 0f)
            {
                PlayerController player = PlayerController.Current;
                if (player != null && player.Health.IsAlive)
                {
                    embolismCooldown = 3f;
                    SpeedBanner.Show("¡EMBOLIA!", new Color(1f, 0.4f, 0.4f), 1.8f);
                    GameplayEvents.RaiseMessage("¡EMBOLIA! El coágulo tapó una arteria del cerebro. Regresas un poco: el coágulo sigue igual de chico. ¡Otra vez!");
                    DamageInfo info = DamageInfo.Create(9999f, DamageKind.Environment, Team.Neutral, player.Position);
                    info.blockable = false;
                    player.Health.ApplyDamage(info);
                }
            }
        }

        private void DropEmboli(FlightRoute route, int count)
        {
            if (emboliPrefab == null) return;
            for (int i = 0; i < count; i++)
            {
                Vector2 world = (Vector2)transform.position + new Vector2(-1.6f, (i - (count - 1) * 0.5f) * 1.6f);
                EnemyHealth e = Instantiate(emboliPrefab, new Vector3(world.x, world.y, 0f), Quaternion.identity);
                var spec = new FlightWaveSpec
                {
                    prefab = emboliPrefab,
                    pattern = i == 0 ? FlightPattern.Chase : FlightPattern.Line,
                    speed = i == 0 ? 2.4f : 3.2f,
                    hoverTime = 4f,
                    amplitude = 0.6f,
                    frequency = 0.7f
                };
                e.gameObject.AddComponent<FlightEnemy>().Begin(null, spec, world - route.CameraPosition, Random.value * 10f);
            }
            Sfx.EffectAt("Coagulo_Disuelve", transform.position, 0.4f);
        }

        /// <summary>Red de fibrina: una fila de émbolos de lado a lado con UN hueco.</summary>
        private void FibrinNet(FlightRoute route)
        {
            if (emboliPrefab == null) return;
            int gap = Random.Range(1, 5);
            for (int i = 0; i < 6; i++)
            {
                if (i == gap) continue;
                Vector2 world = new Vector2(transform.position.x - 1f, route.CameraPosition.y - 3.75f + i * 1.5f);
                EnemyHealth e = Instantiate(emboliPrefab, new Vector3(world.x, world.y, 0f), Quaternion.identity);
                var spec = new FlightWaveSpec { prefab = emboliPrefab, pattern = FlightPattern.Line, speed = 2.2f };
                e.gameObject.AddComponent<FlightEnemy>().Begin(null, spec, world - route.CameraPosition, 0f);
            }
            SpeedBanner.Show("¡RED DE FIBRINA!", new Color(1f, 0.95f, 0.8f), 1f);
        }

        private void OnDied(EnemyHealth _)
        {
            if (ended) return;
            ended = true;
            if (meter != null) Destroy(meter.gameObject);
            if (Heartbeat.Current != null) Heartbeat.Current.Tachycardia = 0f;
            FlightRoute route = FlightRoute.Current;
            if (route != null) route.SpeedMultiplier = 1f;
            GameplayEvents.RaiseBossEnded();
            SpeedBanner.Show("¡EMBOLIA EVITADA!", new Color(0.6f, 1f, 0.8f), 2f);
            GameplayEvents.RaiseMessage("¡Deshiciste EL GRAN TROMBO! La sangre llega al cerebro sin problema. En la vida real, ante señales de " +
                "embolia (cara chueca, brazo débil, hablar raro) hay que ir al hospital YA: cada minuto cuenta.");
        }
    }
}
