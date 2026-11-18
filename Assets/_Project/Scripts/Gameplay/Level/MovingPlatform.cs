using HealingKnight.Gameplay.Movement;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Level
{
    /// <summary>
    /// Plataforma que va y viene entre dos puntos y te lleva (el BOLO del colon descendente: masas que bajan
    /// empujadas por la pared del intestino). Espera un poco en cada punta. Plataforma de un solo sentido.
    /// Se dibuja por código (bola café aplastada) o con un sprite en 'art'.
    /// </summary>
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector2 pointA;
        [SerializeField] private Vector2 pointB;
        [SerializeField] private float speed = 2f;
        [SerializeField] private float wait = 0.8f;
        [SerializeField] private float width = 2.6f;
        [SerializeField] private float phase;
        [SerializeField] private Sprite art;
        [SerializeField] private Color fill = new Color(0.55f, 0.38f, 0.2f, 1f);
        [SerializeField] private Color shade = new Color(0.42f, 0.27f, 0.13f, 1f);
        [SerializeField] private Color outline = new Color(0.2f, 0.12f, 0.05f, 1f);

        public enum Style { Bolus, BloodCell, Impulse }

        [Header("Viaje (glóbulo rojo / impulso nervioso)")]
        [Tooltip("Espera en A hasta que te subas; te lleva a B y, si te bajas, regresa por ti.")]
        [SerializeField] private bool rideOnly;
        [SerializeField] private float waveAmplitude;
        [SerializeField] private float waveLength = 6f;
        [SerializeField] private Style style = Style.Bolus;
        [SerializeField, TextArea] private string rideMessage;

        private float t, waitTimer, idleTimer;
        private bool riding, saidMessage;
        private Transform glow;
        private int dir = 1;
        private Vector2 last;

        public void Configure(Vector2 from, Vector2 to, float moveSpeed, float startPhase, float platformWidth, Sprite drawing)
        {
            pointA = from;
            pointB = to;
            speed = moveSpeed;
            phase = startPhase;
            width = platformWidth;
            art = drawing;
        }

        /// <summary>Viaje de ida que espera a que te subas (glóbulo rojo por un capilar, impulso por el axón).</summary>
        public void ConfigureRide(Style look, float wave, string message)
        {
            rideOnly = true;
            style = look;
            waveAmplitude = wave;
            rideMessage = message;
        }

        private void Start()
        {
            t = Mathf.Clamp01(phase);
            transform.position = Vector2.Lerp(pointA, pointB, t);
            last = transform.position;
            Build();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            float length = Mathf.Max(0.01f, Vector2.Distance(pointA, pointB));
            PlayerController rider = PlayerController.Current;
            bool riderOnTop = rider != null && rider.Motor != null && Mathf.Abs(rider.Position.x - last.x) <= width * 0.5f + 0.1f
                              && Mathf.Abs(rider.Position.y - last.y) < 0.25f && rider.Motor.Velocity.y <= 0.5f;
            if (rideOnly)
            {
                // Arranca cuando te subes; si te bajas (o te caes) un rato, regresa al inicio por ti.
                if (riderOnTop)
                {
                    idleTimer = 0f;
                    if (!riding)
                    {
                        riding = true;
                        waitTimer = 0.35f;
                        if (!saidMessage && !string.IsNullOrEmpty(rideMessage)) { saidMessage = true; GameplayEvents.RaiseMessage(rideMessage); }
                    }
                }
                else idleTimer += dt;
                if (waitTimer > 0f) waitTimer -= dt;
                else if (riding) t = Mathf.Min(1f, t + speed * dt / length);
                if (t >= 1f && idleTimer > 1.5f) riding = false;
                if (!riding && idleTimer > 1.5f) t = Mathf.Max(0f, t - speed * 1.5f * dt / length);
            }
            else if (waitTimer > 0f) waitTimer -= dt;
            else
            {
                t += dir * speed * dt / length;
                if (t >= 1f) { t = 1f; dir = -1; waitTimer = wait; }
                else if (t <= 0f) { t = 0f; dir = 1; waitTimer = wait; }
            }
            float eased = rideOnly ? t : Mathf.SmoothStep(0f, 1f, t);
            Vector2 now = Vector2.Lerp(pointA, pointB, eased);
            if (waveAmplitude != 0f) now.y += Mathf.Sin(eased * length / Mathf.Max(0.5f, waveLength) * Mathf.PI * 2f) * waveAmplitude * Mathf.Sin(eased * Mathf.PI);
            if (glow != null) glow.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 12f) * 0.12f);
            Vector2 delta = now - last;

            PlayerController player = PlayerController.Current;
            if (player != null && player.Motor != null)
            {
                Vector2 feet = player.Position;
                bool onTop = Mathf.Abs(feet.x - last.x) <= width * 0.5f + 0.1f && Mathf.Abs(feet.y - last.y) < 0.25f
                             && player.Motor.Velocity.y <= 0.5f;
                if (onTop) player.Motor.Body.position += delta;
            }
            transform.position = now;
            last = now;
        }

        private void Build()
        {
            gameObject.layer = PhysicsLayers.Ground;
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width * 0.85f, 0.3f);
            box.offset = new Vector2(0f, -0.15f);
            box.usedByEffector = true;
            var effector = gameObject.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;

            if (art != null)
            {
                SpriteRenderer drawn = CodeSprites.Art(transform, art, width * 1.05f, -4, new Vector2(0f, -0.42f));
                // Glóbulo rojo / impulso: que la orilla de arriba del dibujo quede justo donde te paras.
                if (style != Style.Bolus) drawn.transform.localPosition = new Vector3(0f, 0.08f - drawn.bounds.size.y * 0.5f, 0f);
                return;
            }
            Sprite c = CodeSprites.Circle;
            if (style == Style.BloodCell)
            {
                // Glóbulo rojo: disco rojo con el centro hundido (bicóncavo), visto de lado-arriba.
                CodeSprites.Add(transform, c, new Color(0.3f, 0.02f, 0.05f, 1f), -6, new Vector2(0f, -0.35f), new Vector2(width + 0.16f, 0.9f));
                CodeSprites.Add(transform, c, new Color(0.88f, 0.16f, 0.2f, 1f), -5, new Vector2(0f, -0.35f), new Vector2(width, 0.76f));
                CodeSprites.Add(transform, c, new Color(0.62f, 0.06f, 0.12f, 1f), -4, new Vector2(0f, -0.3f), new Vector2(width * 0.5f, 0.28f));
                CodeSprites.Add(transform, c, new Color(1f, 0.6f, 0.6f, 0.7f), -3, new Vector2(-width * 0.25f, -0.15f), new Vector2(width * 0.3f, 0.1f));
                return;
            }
            if (style == Style.Impulse)
            {
                // Impulso nervioso: chispa eléctrica que corre por el axón.
                glow = CodeSprites.Add(transform, c, new Color(1f, 0.9f, 0.3f, 0.35f), -7, new Vector2(0f, -0.25f), new Vector2(width + 1.2f, 1.4f)).transform;
                CodeSprites.Add(transform, c, new Color(0.15f, 0.2f, 0.6f, 1f), -6, new Vector2(0f, -0.3f), new Vector2(width + 0.14f, 0.7f));
                CodeSprites.Add(transform, c, new Color(0.45f, 0.85f, 1f, 1f), -5, new Vector2(0f, -0.3f), new Vector2(width, 0.56f));
                CodeSprites.Add(transform, c, new Color(1f, 0.95f, 0.6f, 1f), -4, new Vector2(0f, -0.28f), new Vector2(width * 0.6f, 0.26f));
                return;
            }
            CodeSprites.Add(transform, c, outline, -6, new Vector2(0f, -0.4f), new Vector2(width + 0.14f, 1.04f));
            CodeSprites.Add(transform, c, fill, -5, new Vector2(0f, -0.4f), new Vector2(width, 0.9f));
            CodeSprites.Add(transform, c, shade, -4, new Vector2(width * 0.12f, -0.55f), new Vector2(width * 0.55f, 0.45f));
            for (int i = 0; i < 3; i++)
                CodeSprites.Add(transform, c, shade, -4, new Vector2((i - 1) * width * 0.25f, -0.25f), new Vector2(0.22f, 0.16f));
            CodeSprites.Add(transform, c, new Color(1f, 0.9f, 0.75f, 0.6f), -3, new Vector2(-width * 0.22f, -0.12f), new Vector2(width * 0.28f, 0.12f));
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.6f, 0.4f, 0.2f, 0.8f);
            Gizmos.DrawLine(pointA, pointB);
        }
    }
}
