using UnityEngine;

namespace HealingKnight.Gameplay.Movement
{
    /// <summary>
    /// Movimiento de plataformero en vista lateral sobre un Rigidbody2D:
    /// detección de suelo, salto de altura fija, velocidad máxima de caída y hacia dónde mira.
    /// Lo usan el jugador y los enemigos que caminan.
    /// Estructura esperada:
    ///   Personaje (Rigidbody2D, BoxCollider2D, CharacterMotor2D)
    ///   └── Visual (sprites o piezas animadas; se voltea con Facing)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class CharacterMotor2D : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private LayerMask groundMask = 1 << 6; // capa "Ground"
        [SerializeField] private float gravity = 45f;
        [SerializeField] private float fallGravityMultiplier = 1.4f;
        [SerializeField] private float maxFallSpeed = 20f;
        [Tooltip("Mantener dentro de los límites del nivel/arena en X.")]
        [SerializeField] private bool clampToBounds = true;

        private Rigidbody2D rb;
        private BoxCollider2D box;
        private int facing = 1;
        private bool grounded;
        private float jumpGravity;
        private bool useJumpGravity;
        private Vector3 visualBaseScale = Vector3.one;

        /// <summary>Multiplica la gravedad mientras cae (caída lenta). 1 = normal.</summary>
        public float FallGravityScale { get; set; } = 1f;
        /// <summary>Límite extra de velocidad de caída (caída lenta). 0 o menos = sin límite extra.</summary>
        public float FallSpeedLimit { get; set; }

        public Rigidbody2D Body => rb;
        public BoxCollider2D Collider => box;
        public Transform Visual => visual;
        public int Facing => facing;
        public bool IsGrounded => grounded;
        public Vector2 Velocity => rb.linearVelocity;
        public Vector2 Position => transform.position;

        /// <summary>Centro del cuerpo (para apuntar o recibir golpes).</summary>
        public Vector2 Center => box != null ? (Vector2)box.bounds.center : (Vector2)transform.position;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            rb.gravityScale = 0f;              // la gravedad se aplica aquí para controlarla exacto
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            if (visual != null)
                visualBaseScale = new Vector3(Mathf.Abs(visual.localScale.x), visual.localScale.y, visual.localScale.z);
        }

        private void FixedUpdate()
        {
            grounded = CheckGround();

            Vector2 v = rb.linearVelocity;
            float g = useJumpGravity ? jumpGravity : gravity;
            if (v.y < 0f) g *= fallGravityMultiplier * FallGravityScale;
            if (!grounded || v.y > 0f) v.y -= g * Time.fixedDeltaTime;
            float limit = FallSpeedLimit > 0f ? Mathf.Min(maxFallSpeed, FallSpeedLimit) : maxFallSpeed;
            if (v.y < -limit)
            {
                // Al empezar a planear frena suave en vez de en seco.
                v.y = FallSpeedLimit > 0f ? Mathf.MoveTowards(v.y, -limit, 60f * Time.fixedDeltaTime) : -limit;
            }
            if (grounded && v.y <= 0f) useJumpGravity = false;

            rb.linearVelocity = v;

            if (clampToBounds)
            {
                Vector2 p = rb.position;
                float minX = WalkableArea.MinX + box.size.x * 0.5f;
                float maxX = WalkableArea.MaxX - box.size.x * 0.5f;
                if (p.x < minX || p.x > maxX)
                {
                    p.x = Mathf.Clamp(p.x, minX, maxX);
                    rb.position = p;
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
            }
        }

        private bool CheckGround()
        {
            Bounds b = box.bounds;
            Vector2 origin = new Vector2(b.center.x, b.min.y + 0.05f);
            Vector2 size = new Vector2(b.size.x * 0.9f, 0.05f);
            RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, 0.1f, groundMask);
            // distance 0 = los pies están DENTRO de una plataforma (atravesándola al saltar): eso no es suelo.
            return hit.collider != null && hit.distance > 0.001f && hit.normal.y > 0.5f && rb.linearVelocity.y <= 0.01f;
        }

        /// <summary>¿Hay suelo un paso adelante? (para que los enemigos no caigan de las plataformas)</summary>
        public bool HasGroundAhead(float distance = 0.6f)
        {
            Bounds b = box.bounds;
            Vector2 origin = new Vector2(b.center.x + facing * (b.extents.x + distance), b.min.y + 0.1f);
            return Physics2D.Raycast(origin, Vector2.down, 1.0f, groundMask).collider != null;
        }

        public void SetHorizontalVelocity(float vx)
        {
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        }

        public void SetVelocity(Vector2 v) => rb.linearVelocity = v;

        public void Jump(float height, float timeToApex)
        {
            jumpGravity = 2f * height / (timeToApex * timeToApex);
            useJumpGravity = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 2f * height / timeToApex);
            grounded = false;
        }

        public void SetFacing(int direction)
        {
            if (direction == 0) return;
            facing = direction > 0 ? 1 : -1;
            if (visual != null)
                visual.localScale = new Vector3(visualBaseScale.x * facing, visualBaseScale.y, visualBaseScale.z);
        }

        public void Teleport(Vector2 position)
        {
            rb.position = position;
            transform.position = position;
            rb.linearVelocity = Vector2.zero;
        }

        /// <summary>Cambia el alto del colisionador (agacharse) manteniendo los pies en su lugar.</summary>
        public void SetColliderHeight(float height)
        {
            Vector2 size = box.size;
            if (Mathf.Approximately(size.y, height)) return;
            box.size = new Vector2(size.x, height);
            box.offset = new Vector2(box.offset.x, height * 0.5f);
        }
    }
}
