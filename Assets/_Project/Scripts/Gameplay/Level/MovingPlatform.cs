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

        private float t, waitTimer;
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
            if (waitTimer > 0f) waitTimer -= dt;
            else
            {
                t += dir * speed * dt / length;
                if (t >= 1f) { t = 1f; dir = -1; waitTimer = wait; }
                else if (t <= 0f) { t = 0f; dir = 1; waitTimer = wait; }
            }
            Vector2 now = Vector2.Lerp(pointA, pointB, Mathf.SmoothStep(0f, 1f, t));
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
                CodeSprites.Art(transform, art, width * 1.05f, -4, new Vector2(0f, -0.35f));
                return;
            }
            Sprite c = CodeSprites.Circle;
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
