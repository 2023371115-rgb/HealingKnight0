using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Enemies;
using HealingKnight.Gameplay.Flight;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// VÁLVULA DEL CORAZÓN (a pie): dos valvas (una del techo y una del piso) que se abren justo DESPUÉS de cada
    /// latido y se cierran antes del siguiente. Cerradas son una pared. Si se cierran contigo en medio te avientan
    /// para atrás. Con arritmia se abren a destiempo.
    /// Biología: las válvulas dejan pasar la sangre hacia un solo lado; el "lub-dub" del latido es el ruido al cerrarse.
    /// </summary>
    public class BeatValve : MonoBehaviour
    {
        [SerializeField] private float height = 9f;
        [Tooltip("Parte del latido en la que está abierta (0..1), justo después del 'lub'.")]
        [SerializeField] private float openPart = 0.55f;
        [SerializeField] private float damage = 8f;
        [SerializeField] private string label;

        private BoxCollider2D wall;
        private Transform upper, lower;
        private bool wasOpen, told;
        private float angle;

        public void Configure(float wallHeight, float openFraction, string firstMessage)
        {
            height = wallHeight; openPart = openFraction; label = firstMessage;
        }

        private void Start()
        {
            gameObject.layer = PhysicsLayers.Ground;
            wall = gameObject.AddComponent<BoxCollider2D>();
            wall.size = new Vector2(0.9f, height);
            wall.offset = new Vector2(0f, height * 0.5f);
            upper = Leaf("ValvaArriba", height, -1f);
            lower = Leaf("ValvaAbajo", 0f, 1f);
        }

        private Transform Leaf(string name, float y, float dir)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(transform, false);
            pivot.localPosition = new Vector3(0f, y, 0f);
            float len = height * 0.52f;
            var leaf = new GameObject("Hoja").transform;
            leaf.SetParent(pivot, false);
            leaf.localPosition = new Vector3(0f, dir * len * 0.5f, 0f);
            CodeSprites.Add(leaf, CodeSprites.Circle, new Color(0.4f, 0.05f, 0.1f), 5, Vector2.zero, new Vector2(0.95f, len + 0.2f), "Orilla");
            CodeSprites.Add(leaf, CodeSprites.Circle, new Color(1f, 0.72f, 0.75f), 6, Vector2.zero, new Vector2(0.75f, len), "Valva");
            CodeSprites.Add(leaf, CodeSprites.Circle, new Color(1f, 0.95f, 0.95f, 0.6f), 7, new Vector2(-0.12f, 0f), new Vector2(0.2f, len * 0.6f), "Brillo");
            return pivot;
        }

        private void Update()
        {
            if (wall == null) return;
            float phase = Heartbeat.Current != null ? Heartbeat.PhaseNow : Mathf.Repeat(Time.time / 0.85f, 1f);
            bool open = phase > 0.04f && phase < 0.04f + openPart;
            float k = open ? 1f : 0f;
            float a = Mathf.Lerp(0f, 78f, k);
            // Se abren hacia adelante (como una puerta de vaivén que solo abre para un lado).
            angle = Mathf.Lerp(angle, a, 1f - Mathf.Exp(-30f * Time.deltaTime));
            if (upper != null) upper.localRotation = Quaternion.Euler(0f, 0f, angle);
            if (lower != null) lower.localRotation = Quaternion.Euler(0f, 0f, -angle);
            wall.enabled = !open;

            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            float dx = player.Position.x - transform.position.x;
            if (!told && Mathf.Abs(dx) < 8f && !string.IsNullOrEmpty(label))
            {
                told = true;
                GameplayEvents.RaiseMessage(label);
            }
            if (wasOpen && !open && Mathf.Abs(dx) < 0.75f && player.Position.y < transform.position.y + height)
            {
                DamageInfo info = DamageInfo.Create(damage, DamageKind.Environment, Team.Neutral, transform.position);
                player.Health.ApplyDamage(info);
                player.Motor.Teleport(new Vector2(transform.position.x - 1.2f, player.Position.y));
            }
            wasOpen = open;
        }
    }

    /// <summary>
    /// CÁMARA QUE SE APRIETA (ventrículo): el techo de músculo BAJA en cada latido (sístole) y sube después. Si en ese
    /// momento estás arriba (en una repisa alta o saltando), te aplasta. Quédate abajo en el latido y sube entre latidos.
    /// </summary>
    public class SqueezingChamber : MonoBehaviour
    {
        [SerializeField] private float fromX, toX, ceilingY = 8f, drop = 3.4f, floorY;
        [SerializeField] private float damage = 10f;

        private Transform ceiling;
        private float hurtCooldown;
        private bool told;

        public void Configure(float startX, float endX, float groundY, float topY, float squeeze)
        {
            fromX = startX; toX = endX; floorY = groundY; ceilingY = topY; drop = squeeze;
        }

        private void Start()
        {
            ceiling = new GameObject("TechoMusculo").transform;
            ceiling.SetParent(transform, false);
            float w = toX - fromX;
            CodeSprites.Add(ceiling, CodeSprites.Square, new Color(0.28f, 0.03f, 0.07f), 8, new Vector2(w * 0.5f, 3f), new Vector2(w, 6f), "Musculo");
            for (float x = 0.6f; x < w; x += 1.3f)
            {
                CodeSprites.Add(ceiling, CodeSprites.Circle, new Color(0.25f, 0.02f, 0.06f), 9, new Vector2(x, 0.05f), new Vector2(1.45f, 1f), "Contorno");
                CodeSprites.Add(ceiling, CodeSprites.Circle, new Color(0.75f, 0.15f, 0.2f), 10, new Vector2(x, 0.1f), new Vector2(1.3f, 0.85f), "Fibra");
            }
            // Rayas de aviso en la pared del fondo: hasta dónde baja el techo.
            for (float x = 1f; x < w; x += 3f)
                CodeSprites.Add(transform, CodeSprites.Square, new Color(1f, 0.6f, 0.5f, 0.25f), -40, new Vector2(x, ceilingY - drop - floorY - transform.position.y + floorY), new Vector2(1.4f, 0.08f), "Marca");
        }

        private void Update()
        {
            if (ceiling == null) return;
            float dt = Time.deltaTime;
            float surge = Heartbeat.SurgeNow;
            float bottom = ceilingY - drop * surge;
            ceiling.position = new Vector3(fromX, bottom, 0f);
            if (hurtCooldown > 0f) hurtCooldown -= dt;

            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            Vector2 p = player.Position;
            if (p.x < fromX || p.x > toX) return;
            if (!told)
            {
                told = true;
                GameplayEvents.RaiseMessage("¡El VENTRÍCULO se aprieta en cada latido! El techo baja: en el 'lub' quédate ABAJO y salta entre latidos.");
            }
            float head = p.y + 1.8f * player.Size;
            if (head > bottom && hurtCooldown <= 0f && surge > 0.3f)
            {
                hurtCooldown = 0.9f;
                DamageInfo info = DamageInfo.Create(damage, DamageKind.Environment, Team.Neutral, new Vector2(p.x, bottom));
                info.blockable = false;
                player.Health.ApplyDamage(info);
                player.Motor.SetVelocity(new Vector2(player.Motor.Velocity.x, -8f));
            }
        }
    }

    /// <summary>
    /// CUERDAS TENDINOSAS: los hilos que sostienen las válvulas. Son elásticos: si caes encima te lanzan muy alto.
    /// </summary>
    public class ChordaeTrampoline : MonoBehaviour
    {
        [SerializeField] private float width = 2.4f;
        [SerializeField] private float launch = 17f;

        private Transform cords;
        private float bounce;

        public void Configure(float padWidth, float launchSpeed)
        {
            width = padWidth; launch = launchSpeed;
        }

        private void Start()
        {
            cords = new GameObject("Cuerdas").transform;
            cords.SetParent(transform, false);
            // Músculo papilar (el poste) y las cuerdas blancas en abanico.
            CodeSprites.Add(transform, CodeSprites.Circle, new Color(0.3f, 0.03f, 0.08f), 4, new Vector2(0f, -1.2f), new Vector2(1.5f, 2.6f), "PapilarContorno");
            CodeSprites.Add(transform, CodeSprites.Circle, new Color(0.75f, 0.16f, 0.22f), 5, new Vector2(0f, -1.2f), new Vector2(1.3f, 2.4f), "Papilar");
            for (int i = 0; i < 5; i++)
            {
                float x = -width * 0.5f + width * i / 4f;
                SpriteRenderer c = CodeSprites.Add(cords, CodeSprites.Square, new Color(1f, 0.95f, 0.88f), 6, new Vector2(x * 0.5f, -0.3f), new Vector2(0.07f, 1f), "Cuerda");
                c.transform.localRotation = Quaternion.Euler(0f, 0f, -x * 18f);
            }
            CodeSprites.Add(cords, CodeSprites.Square, new Color(1f, 0.92f, 0.85f), 7, new Vector2(0f, 0.15f), new Vector2(width, 0.16f), "Borde");
        }

        private void Update()
        {
            if (cords == null) return;
            float dt = Time.deltaTime;
            bounce = Mathf.MoveTowards(bounce, 0f, dt * 3f);
            cords.localScale = new Vector3(1f, 1f - bounce * 0.35f, 1f);
            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            Vector2 p = player.Position;
            float top = transform.position.y + 0.25f;
            if (Mathf.Abs(p.x - transform.position.x) > width * 0.5f || p.y < top - 0.4f || p.y > top + 0.35f) return;
            if (player.Motor.Velocity.y > 0.5f) return;
            player.Motor.SetVelocity(new Vector2(player.Motor.Velocity.x, launch));
            bounce = 1f;
            Sfx.EffectAt("Jugador_Salto", transform.position, 0.8f);
        }
    }

    /// <summary>
    /// CAMINO ELÉCTRICO del corazón (nodo sinusal → haz de His): una tira brillante por donde corre un IMPULSO en
    /// cada latido. Si estás parado en la tira cuando pasa el impulso, ¡lo SURFEAS! (te avienta hacia adelante).
    /// A los enemigos que estén encima les da toque.
    /// </summary>
    public class ElectricPath : MonoBehaviour
    {
        [SerializeField] private float fromX, toX, y;
        [SerializeField] private float speed = 18f;
        [SerializeField] private float boost = 15f;

        private Transform pulse;
        private float x;
        private bool running, told;
        private int lastBeat = -1;

        public void Configure(float startX, float endX, float pathY)
        {
            fromX = startX; toX = endX; y = pathY;
        }

        private void OnEnable() => Heartbeat.Beat += OnBeat;
        private void OnDisable() => Heartbeat.Beat -= OnBeat;

        private void Start()
        {
            float w = toX - fromX;
            CodeSprites.Add(transform, CodeSprites.Square, new Color(1f, 0.9f, 0.4f, 0.35f), -45, new Vector2(w * 0.5f + fromX - transform.position.x, y - transform.position.y + 0.06f), new Vector2(w, 0.16f), "Camino");
            pulse = new GameObject("Impulso").transform;
            pulse.SetParent(transform, false);
            CodeSprites.Add(pulse, CodeSprites.Glow, new Color(1f, 0.95f, 0.5f, 0.8f), 20, Vector2.zero, new Vector2(2f, 1.2f), "Brillo");
            CodeSprites.Add(pulse, CodeSprites.Circle, new Color(1f, 1f, 0.8f), 21, Vector2.zero, new Vector2(0.6f, 0.4f), "Chispa");
            pulse.gameObject.SetActive(false);
        }

        private void OnBeat(int count)
        {
            if (running || count == lastBeat) return;
            lastBeat = count;
            running = true;
            x = fromX;
            if (pulse != null) pulse.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!running || pulse == null) return;
            float dt = Time.deltaTime;
            float before = x;
            x += speed * dt;
            pulse.position = new Vector3(x, y + 0.15f, 0f);
            if (x > toX) { running = false; pulse.gameObject.SetActive(false); return; }

            PlayerController player = PlayerController.Current;
            if (player != null && player.Health.IsAlive && player.Motor.IsGrounded)
            {
                Vector2 p = player.Position;
                if (Mathf.Abs(p.y - y) < 0.4f && p.x >= before - 0.3f && p.x <= x + 0.3f)
                {
                    player.Motor.SetVelocity(new Vector2(boost, 7f));
                    Sfx.EffectAt("Jugador_Dash", p, 0.7f);
                    if (!told)
                    {
                        told = true;
                        GameplayEvents.RaiseMessage("¡Surfeaste el IMPULSO ELÉCTRICO del corazón! Así viaja la señal que hace latir al músculo.");
                    }
                }
            }
            foreach (EnemyHealth e in EnemyHealth.Active)
            {
                if (e == null || !e.IsAlive) continue;
                Vector2 ep = e.transform.position;
                if (Mathf.Abs(ep.y - y) < 1.2f && ep.x >= before - 0.3f && ep.x <= x + 0.3f)
                    e.ApplyDamage(DamageInfo.Create(15f, DamageKind.Environment, Team.Player, ep));
            }
        }
    }
}
