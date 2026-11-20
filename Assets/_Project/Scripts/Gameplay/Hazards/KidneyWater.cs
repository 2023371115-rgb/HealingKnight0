using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Level;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// CORRIENTE de agua del riñón (en cualquier dirección): mientras estás en su rectángulo te arrastra. Se ve como
    /// rayitas de luz que corren hacia donde empuja. Con 'pulse' > 0 va y viene (empuja fuerte, luego casi nada): ahí
    /// están las ventanas para cruzar. Con el DIURÉTICO, las corrientes de lado te llevan hacia ADELANTE.
    /// Usos: las "bombas" de las paredes del túbulo proximal (te jalan de lado), el chorro que sube por la rama
    /// ascendente del asa de Henle y la ola de orina del uréter.
    /// </summary>
    [DefaultExecutionOrder(70)]   // después de BuoyancyZone, para que el agua no le borre el empujón
    public class WaterCurrent : MonoBehaviour
    {
        [SerializeField] private Rect area = new Rect(0f, 0f, 6f, 4f);
        [SerializeField] private Vector2 push = new Vector2(3f, 0f);
        [SerializeField] private float pulse;
        [SerializeField] private Color color = new Color(1f, 1f, 0.9f, 0.5f);

        private Transform[] streaks;
        private float phase;

        public void Configure(Rect zone, Vector2 flow, float pulseSpeed, Color streakColor)
        {
            area = zone; push = flow; pulse = pulseSpeed; color = streakColor;
        }

        private float Strength => pulse > 0f ? 0.25f + 0.75f * (0.5f + 0.5f * Mathf.Sin(Time.time * pulse + phase)) : 1f;

        private Vector2 Flow
        {
            get
            {
                Vector2 f = push;
                // Diurético: todo fluye hacia la salida (más orina): las corrientes de lado te llevan adelante.
                if (DiureticPickup.Active && Mathf.Abs(f.x) > Mathf.Abs(f.y)) f.x = Mathf.Abs(f.x) * 1.3f;
                return f * Strength;
            }
        }

        private void Start()
        {
            phase = area.x * 0.37f;
            int n = Mathf.Clamp(Mathf.RoundToInt(area.width * area.height / 3f), 4, 40);
            streaks = new Transform[n];
            var rnd = new System.Random(Mathf.RoundToInt(area.x * 13f));
            float angle = Mathf.Atan2(push.y, push.x) * Mathf.Rad2Deg;
            for (int i = 0; i < n; i++)
            {
                Transform t = CodeSprites.Add(transform, CodeSprites.Glow, color, 33, Vector2.zero, new Vector2(0.9f, 0.12f), "Corriente").transform;
                t.rotation = Quaternion.Euler(0f, 0f, angle);
                t.position = new Vector3(area.x + (float)rnd.NextDouble() * area.width, area.y + (float)rnd.NextDouble() * area.height, 0f);
                streaks[i] = t;
            }
        }

        private void Update()
        {
            if (streaks == null) return;
            Vector2 f = Flow;
            float speed = Mathf.Max(1.5f, f.magnitude * 1.4f);
            Vector2 dir = f.sqrMagnitude > 0.001f ? f.normalized : push.normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float alpha = Mathf.Lerp(0.15f, 1f, Strength);
            foreach (Transform t in streaks)
            {
                Vector2 p = (Vector2)t.position + dir * speed * Time.deltaTime;
                // Al salir del rectángulo vuelve a entrar por el otro lado.
                if (p.x > area.xMax) p.x -= area.width; else if (p.x < area.xMin) p.x += area.width;
                if (p.y > area.yMax) p.y -= area.height; else if (p.y < area.yMin) p.y += area.height;
                t.position = p;
                t.rotation = Quaternion.Euler(0f, 0f, angle);
                var sr = t.GetComponent<SpriteRenderer>();
                Color c = color; c.a *= alpha; sr.color = c;
            }
        }

        private void FixedUpdate()
        {
            PlayerController player = PlayerController.Current;
            if (player == null || player.Motor == null || player.IsFlying || !player.Health.IsAlive) return;
            if (!area.Contains(player.Motor.Center)) return;
            // Se mueve la posición (como las cintas de cilios): así ni el control ni el agua le quitan el empujón.
            player.Motor.Body.position += Flow * Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// REABSORCIÓN: burbujas de GLUCOSA, AMINOÁCIDOS o AGUA que flotan en el túbulo. El riñón las regresa a la sangre
    /// (no se desperdician en la orina): al tocarlas te curan un poquito. La primera vez NODO explica qué es.
    /// </summary>
    public class ReabsorptionBubble : MonoBehaviour
    {
        public enum Kind { Glucosa, Aminoacido, Agua }

        [SerializeField] private Kind kind;
        [SerializeField] private float heal = 4f;

        private static bool told;
        private Transform visual;
        private float seed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => told = false;

        public void Configure(Kind what, float amount) { kind = what; heal = amount; }

        private void Start()
        {
            seed = transform.position.x;
            visual = new GameObject("Dibujo").transform;
            visual.SetParent(transform, false);
            Color c = kind == Kind.Glucosa ? new Color(1f, 0.9f, 0.4f) : kind == Kind.Aminoacido ? new Color(0.55f, 0.95f, 0.6f) : new Color(0.55f, 0.85f, 1f);
            CodeSprites.Add(visual, CodeSprites.Glow, new Color(c.r, c.g, c.b, 0.5f), 34, Vector2.zero, Vector2.one * 1.3f, "Aura");
            CodeSprites.Add(visual, CodeSprites.Circle, new Color(1f, 1f, 1f, 0.35f), 35, Vector2.zero, Vector2.one * 0.75f, "Burbuja");
            CodeSprites.Add(visual, CodeSprites.Ring, Color.white, 36, Vector2.zero, Vector2.one * 0.78f, "Borde");
            if (kind == Kind.Glucosa)
            {
                // Glucosa: un anillo de 6 lados.
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI / 3f;
                    CodeSprites.Add(visual, CodeSprites.Circle, c, 37, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.18f, Vector2.one * 0.13f, "Atomo");
                }
            }
            else if (kind == Kind.Aminoacido)
            {
                // Aminoácidos: cadenita de cuentas.
                for (int i = 0; i < 4; i++)
                    CodeSprites.Add(visual, CodeSprites.Circle, c, 37, new Vector2(-0.21f + i * 0.14f, Mathf.Sin(i * 1.7f) * 0.07f), Vector2.one * 0.14f, "Cuenta");
            }
            else CodeSprites.Add(visual, CodeSprites.Circle, c, 37, new Vector2(0f, -0.03f), new Vector2(0.24f, 0.3f), "Gota");
        }

        private void Update()
        {
            if (visual == null) return;
            visual.localPosition = new Vector3(Mathf.Sin(Time.time * 1.3f + seed) * 0.2f, Mathf.Sin(Time.time * 1.9f + seed) * 0.25f, 0f);
            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            if (((Vector2)visual.position - player.Motor.Center).sqrMagnitude > 0.9f * 0.9f) return;
            player.Health.Heal(heal);
            Sfx.EffectAt("Recoger_Vida", visual.position, 0.5f);
            if (!told)
            {
                told = true;
                GameplayEvents.RaiseMessage("¡REABSORCIÓN! El riñón filtra la sangre y luego RECUPERA lo que sí sirve: glucosa, aminoácidos y " +
                    "casi toda el agua. Solo lo que sobra se va en la orina. Cada burbuja te cura un poquito.");
            }
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// CRISTAL DE SAL que flota en el asa de Henle (allá abajo el agua está muy salada): tiene picos, gira y sube y baja.
    /// Si lo tocas te lastima; tres balazos lo rompen.
    /// </summary>
    public class SaltCrystal : MonoBehaviour
    {
        [SerializeField] private float bob = 1.2f;
        [SerializeField] private float damage = 8f;

        private Transform visual;
        private Vector3 home;
        private float seed, hurtCooldown;
        private int hits;

        public void Configure(float bobDistance) => bob = bobDistance;

        private void Start()
        {
            home = transform.position;
            seed = home.x * 0.7f;
            visual = new GameObject("Cristal").transform;
            visual.SetParent(transform, false);
            Color edge = new Color(0.35f, 0.3f, 0.25f);
            for (int i = 0; i < 4; i++)
            {
                Transform spike = new GameObject("Pico").transform;
                spike.SetParent(visual, false);
                spike.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
                CodeSprites.Add(spike, CodeSprites.Square, edge, 34, Vector2.zero, new Vector2(1.3f, 0.3f), "Contorno");
                CodeSprites.Add(spike, CodeSprites.Square, new Color(0.97f, 0.97f, 1f), 35, Vector2.zero, new Vector2(1.15f, 0.18f), "Sal");
            }
            CodeSprites.Add(visual, CodeSprites.Square, edge, 36, Vector2.zero, Vector2.one * 0.6f, "Centro");
            CodeSprites.Add(visual, CodeSprites.Square, new Color(0.92f, 0.95f, 1f), 37, Vector2.zero, Vector2.one * 0.46f, "CentroClaro");
            CodeSprites.Add(visual, CodeSprites.Glow, new Color(1f, 1f, 1f, 0.35f), 33, Vector2.zero, Vector2.one * 1.6f, "Brillo");
        }

        private void Update()
        {
            if (visual == null) return;
            float dt = Time.deltaTime;
            if (hurtCooldown > 0f) hurtCooldown -= dt;
            transform.position = home + new Vector3(0f, Mathf.Sin(Time.time * 1.1f + seed) * bob, 0f);
            visual.localRotation = Quaternion.Euler(0f, 0f, Time.time * 40f + seed * 10f);

            Vector2 c = transform.position;
            for (int i = Projectile.Flying.Count - 1; i >= 0; i--)
            {
                Projectile p = Projectile.Flying[i];
                if (p == null || p.SourceTeam != Team.Player || ((Vector2)p.Position - c).sqrMagnitude > 0.55f * 0.55f) continue;
                p.Consume();
                if (++hits >= 3)
                {
                    Sfx.EffectAt("Coagulo_Disuelve", c, 0.6f);
                    Destroy(gameObject);
                    return;
                }
                visual.localScale = Vector3.one * (1f - hits * 0.15f);
            }

            PlayerController player = PlayerController.Current;
            if (player == null || hurtCooldown > 0f || !player.Health.IsAlive) return;
            if ((player.Motor.Center - c).sqrMagnitude > 0.75f * 0.75f) return;
            DamageInfo info = DamageInfo.Create(damage, DamageKind.Contact, Team.Enemy, c);
            info.knockback = 6f;
            player.Health.ApplyDamage(info);
            hurtCooldown = 0.8f;
        }
    }

    /// <summary>
    /// DIURÉTICO (cápsula azul que flota): unos segundos nadas más rápido y las corrientes de lado te llevan hacia
    /// adelante. Biología: un diurético hace que el riñón saque más agua y sal en la orina (se usa, por ejemplo, para
    /// la presión alta).
    /// </summary>
    public class DiureticPickup : MonoBehaviour
    {
        [SerializeField] private float seconds = 8f;

        private static float until;
        private Transform visual;

        public static bool Active => Time.time < until;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => until = 0f;

        private void Start()
        {
            visual = new GameObject("Capsula").transform;
            visual.SetParent(transform, false);
            CodeSprites.Add(visual, CodeSprites.Glow, new Color(0.4f, 0.75f, 1f, 0.6f), 34, Vector2.zero, Vector2.one * 1.8f, "Aura");
            CodeSprites.Add(visual, CodeSprites.Circle, new Color(0.1f, 0.15f, 0.3f), 35, Vector2.zero, new Vector2(1f, 0.5f), "Contorno");
            CodeSprites.Add(visual, CodeSprites.Circle, new Color(0.35f, 0.65f, 1f), 36, new Vector2(-0.2f, 0f), new Vector2(0.5f, 0.38f), "Mitad");
            CodeSprites.Add(visual, CodeSprites.Circle, Color.white, 36, new Vector2(0.2f, 0f), new Vector2(0.5f, 0.38f), "MitadBlanca");
            CodeSprites.Add(visual, CodeSprites.Circle, new Color(1f, 1f, 1f, 0.7f), 37, new Vector2(-0.15f, 0.08f), new Vector2(0.4f, 0.08f), "Brillo");
            WorldText.Create(transform, "DIURÉTICO", new Vector2(0f, 0.85f), new Vector2(3f, 0.5f), new Color(0.7f, 0.9f, 1f), 38, null, 2.4f);
        }

        private void Update()
        {
            if (visual == null) return;
            visual.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 2f) * 0.2f, 0f);
            visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.3f) * 15f);
            PlayerController player = PlayerController.Current;
            if (player == null || !player.Health.IsAlive) return;
            if (((Vector2)visual.position - player.Motor.Center).sqrMagnitude > 1f) return;
            until = Time.time + seconds;
            Sfx.EffectAt("Recoger_Capsula", visual.position);
            Flight.SpeedBanner.Show("¡DIURÉTICO!", new Color(0.5f, 0.8f, 1f), 1.2f);
            GameplayEvents.RaiseMessage($"¡DIURÉTICO! Hace que el riñón saque más agua y sal en la orina: por {seconds:0} segundos todo FLUYE " +
                "hacia adelante y nadas más rápido. (Los médicos lo usan, por ejemplo, para la presión alta.)");
            Destroy(gameObject);
        }
    }
}
