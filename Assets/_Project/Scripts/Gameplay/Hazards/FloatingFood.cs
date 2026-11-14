using HealingKnight.Gameplay.Audio;
using HealingKnight.Gameplay.Player;
using UnityEngine;

namespace HealingKnight.Gameplay.Hazards
{
    /// <summary>
    /// Comida a medio digerir que FLOTA en el ácido y sirve de plataforma (de un solo sentido).
    ///  - Si el ácido está bajo (o seco), descansa en el piso ('restY').
    ///  - Si sube la marea, la comida sube con ella y te lleva encima: es la forma de no quemarte.
    ///  - Mientras flota se mece un poco (solo el dibujo; el piso donde te paras se queda plano).
    /// Cada comida se comporta distinto ('kind'):
    ///  - Normal:   solo flota (manzana).
    ///  - Rebota:   la dona es esponjosa: al caer encima te lanza hacia arriba, como trampolín.
    ///  - Rueda:    la salchicha rueda hacia donde caminas (y te lleva), hasta la orilla del lago.
    ///  - Se hunde: la hamburguesa se va digiriendo: si te quedas encima se hunde poco a poco; al bajarte, sube.
    ///  - Balsa:    (íleon) al subirte avanza sola río abajo hasta la orilla y te lleva, aunque brinques encima.
    ///              Si te caes, se detiene y te espera; si mueres y reapareces atrás, regresa a su lugar.
    /// El objeto raíz marca la parte de ABAJO de la comida; el colisionador va a 'topHeight'.
    /// </summary>
    public class FloatingFood : MonoBehaviour
    {
        public enum FoodKind { Normal, Bouncy, Rolling, Sinking, Raft }

        [SerializeField] private FoodKind kind = FoodKind.Normal;
        [SerializeField] private AcidLake lake;
        [Tooltip("Altura del fondo de la comida cuando no flota (el piso o el fondo del lago).")]
        [SerializeField] private float restY;
        [Tooltip("Alto del dibujo (unidades).")]
        [SerializeField] private float height = 1.4f;
        [Tooltip("Dónde está la parte que se pisa, medida desde abajo (unidades).")]
        [SerializeField] private float topHeight = 1.2f;
        [SerializeField] private float width = 1.6f;
        [Tooltip("Qué parte de la comida queda hundida en el ácido al flotar (0.35 = un tercio).")]
        [Range(0f, 0.9f)]
        [SerializeField] private float submerged = 0.35f;
        [Tooltip("Qué tan rápido sigue al ácido (más chico = más pesada, se queda atrás).")]
        [SerializeField] private float followTime = 0.25f;
        [SerializeField] private Transform visual;

        [Header("Rebota (dona)")]
        [SerializeField] private float bounceSpeed = 17f;
        [Header("Rueda (salchicha)")]
        [SerializeField] private float rollSpeed = 1.8f;
        [Header("Se hunde (hamburguesa)")]
        [SerializeField] private float sinkSpeed = 0.45f;
        [SerializeField] private float maxSink = 1.1f;
        [SerializeField] private float recoverSpeed = 0.6f;
        [Header("Balsa (íleon)")]
        [SerializeField] private float raftSpeed = 3.2f;

        private float bottom, velocity, seed, sink, squash, roll;
        private bool playerWasOnTop, raftStarted;
        private float raftStartX;
        private Vector3 visualBase, visualScale = Vector3.one;

        /// <summary>¿El jugador está parado encima ahora? (lo usa, por ejemplo, el sushi con anisakis)</summary>
        public bool PlayerOnTop { get; private set; }
        public float TopY => transform.position.y + topHeight;
        public FoodKind Kind => kind;

        public void Configure(AcidLake acid, float rest, float foodHeight, float top, float foodWidth, Transform art, FoodKind foodKind = FoodKind.Normal)
        {
            lake = acid;
            restY = rest;
            height = foodHeight;
            topHeight = top;
            width = foodWidth;
            visual = art;
            kind = foodKind;
        }

        private void Start()
        {
            bottom = transform.position.y;
            raftStartX = transform.position.x;
            seed = transform.position.x * 1.7f;
            if (visual != null)
            {
                visualBase = visual.localPosition;
                visualScale = visual.localScale;
            }
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            PlayerController player = PlayerController.Current;
            Vector3 p = transform.position;
            PlayerOnTop = IsOnTop(player, p.x, p.y + topHeight);

            // --- Vertical: flota en el ácido (y la hamburguesa se hunde con tu peso)
            if (kind == FoodKind.Sinking)
                sink = PlayerOnTop ? Mathf.Min(maxSink, sink + sinkSpeed * dt) : Mathf.Max(0f, sink - recoverSpeed * dt);

            float target = restY;
            if (lake != null && lake.Level > 0.05f) target = Mathf.Max(restY, lake.SurfaceY - height * submerged - sink);

            float before = bottom;
            bottom = Mathf.SmoothDamp(bottom, target, ref velocity, followTime, Mathf.Infinity, dt);
            float dy = bottom - before;

            // --- Horizontal: la salchicha rueda hacia donde camina el jugador, sin salirse del lago
            float dx = 0f;
            if (kind == FoodKind.Rolling && PlayerOnTop && lake != null)
            {
                float input = Mathf.Clamp(player.Motor.Velocity.x / 4f, -1f, 1f);
                float half = lake.Width * 0.5f - width * 0.5f - 0.1f;
                float cx = lake.transform.position.x;
                float nx = Mathf.Clamp(p.x + input * rollSpeed * dt, cx - half, cx + half);
                dx = nx - p.x;
                roll -= dx / Mathf.Max(0.2f, height * 0.5f) * Mathf.Rad2Deg;
            }

            // --- La balsa avanza sola mientras vas en ella (parado o brincando encima)
            bool riding = false;
            if (kind == FoodKind.Raft && lake != null && player != null && player.Motor != null)
            {
                if (PlayerOnTop) raftStarted = true;
                Vector2 feet = player.Position;
                riding = PlayerOnTop || (raftStarted && Mathf.Abs(feet.x - p.x) <= width * 0.5f + 0.6f && feet.y > p.y + topHeight - 0.1f);
                float endX = lake.transform.position.x + lake.Width * 0.5f - width * 0.5f - 0.2f;
                if (riding && p.x < endX) dx = Mathf.Min(raftSpeed * dt, endX - p.x);
                // Reapareciste muy atrás: la balsa regresa al inicio del río.
                if (raftStarted && !riding && player.Health.IsAlive && feet.x < p.x - 8f)
                {
                    raftStarted = false;
                    dx = raftStartX - p.x;
                }
            }

            transform.position = new Vector3(p.x + dx, bottom, p.z);

            // --- Lleva al jugador (sube, baja y rueda con la comida sin rebotar)
            if (PlayerOnTop && (Mathf.Abs(dy) > 0.0001f || Mathf.Abs(dx) > 0.0001f))
                player.Motor.Body.position += new Vector2(dx, dy);
            else if (riding && !PlayerOnTop && Mathf.Abs(dx) > 0.0001f && Mathf.Abs(dx) < 1f)
                player.Motor.Body.position += new Vector2(dx, 0f);   // brincando encima: no te quedas atrás

            // --- La dona rebota: al caer encima, te lanza
            if (kind == FoodKind.Bouncy && PlayerOnTop && !playerWasOnTop && player.Motor.Velocity.y <= 0.5f)
            {
                player.Motor.SetVelocity(new Vector2(player.Motor.Velocity.x, bounceSpeed));
                squash = 1f;
                Sfx.EffectAt("Alveolo_Rebote", transform.position, 0.8f);
            }
            playerWasOnTop = PlayerOnTop;
        }

        private bool IsOnTop(PlayerController player, float x, float top)
        {
            if (player == null || player.Motor == null) return false;
            Vector2 feet = player.Position;
            return Mathf.Abs(feet.x - x) <= width * 0.5f + 0.1f && Mathf.Abs(feet.y - top) < 0.18f
                   && player.Motor.Velocity.y <= 0.5f;
        }

        private void Update()
        {
            if (visual == null) return;
            bool floating = lake != null && lake.Level > 0.05f && bottom > restY + 0.02f;
            float k = floating ? 1f : 0f;
            float t = Time.time;
            squash = Mathf.MoveTowards(squash, 0f, Time.deltaTime * 4f);
            float s = Mathf.Sin(squash * Mathf.PI) * 0.18f;   // la dona se aplasta y se estira al rebotar
            visual.localScale = new Vector3(visualScale.x * (1f + s), visualScale.y * (1f - s), visualScale.z);
            visual.localPosition = visualBase + new Vector3(0f, Mathf.Sin(t * 2.1f + seed) * 0.03f * k, 0f);
            float sway = Mathf.Sin(t * 1.3f + seed) * 3.5f * k;
            visual.localRotation = Quaternion.Euler(0f, 0f, kind == FoodKind.Rolling ? sway + roll * 0.15f : sway);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.8f);
            Vector3 p = transform.position;
            Gizmos.DrawLine(p + new Vector3(-width * 0.5f, topHeight, 0f), p + new Vector3(width * 0.5f, topHeight, 0f));
        }
    }
}
