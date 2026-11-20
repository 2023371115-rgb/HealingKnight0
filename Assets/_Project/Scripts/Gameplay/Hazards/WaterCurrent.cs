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
}
