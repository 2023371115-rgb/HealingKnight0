using System;
using HealingKnight.Core.Combat2D;
using HealingKnight.Core.Damage;
using HealingKnight.Gameplay.Combat;
using HealingKnight.Gameplay.Movement;
using UnityEngine;

namespace HealingKnight.Gameplay.Player
{
    /// <summary>
    /// Cerebro de Healing Knight (estilo Metal Slug): correr a velocidad constante, salto de altura fija,
    /// encogerse, apuntar en 8 direcciones (diagonal = dos teclas), disparar, cuchillazo automático,
    /// bomba de medicamento y escudo burbuja. Cada habilidad vive en su propio componente.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor2D), typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour
    {
        public enum State { Normal, Hurt, Dead }

        [SerializeField] private PlayerConfig config;
        [Tooltip("El cuerpo que se encoge al presionar abajo (incluye brazo y escudo).")]
        [SerializeField] private Transform bodyVisual;

        private CharacterMotor2D motor;
        private PlayerInputReader input;
        private PlayerShooter shooter;
        private PlayerBombs bombs;
        private PlayerShield shield;
        private PlayerHealth health;
        private Hurtbox hurtbox;

        private State state = State.Normal;
        private float stateTimer;
        private bool crouching;
        private int airJumpsUsed;
        private float slowFallLeft;
        private bool slowFalling;
        private bool locked;
        private bool bounceAirborne;
        private float bounceTime;
        private float slowMultiplier = 1f;
        private float slowUntil;
        private Vector2 aim = Vector2.right;
        private Vector2 lastSafePosition;
        private Vector3 bodyVisualBaseScale = Vector3.one;

        /// <summary>El jugador actual (el juego es para un jugador).</summary>
        public static PlayerController Current { get; private set; }
        public static event Action<PlayerController> Spawned;

        /// <summary>Al saltar. true = salto en el aire (para animación, sonido o partículas).</summary>
        public event Action<bool> Jumped;

        /// <summary>Al rebotar en un alvéolo inflado.</summary>
        public event Action Bounced;

        public PlayerConfig Config => config;
        public CharacterMotor2D Motor => motor;
        public PlayerInputReader Input => input;
        public PlayerShooter Shooter => shooter;
        public PlayerBombs Bombs => bombs;
        public PlayerShield Shield => shield;
        public PlayerHealth Health => health;
        public State CurrentState => state;
        public bool IsCrouching => crouching;

        /// <summary>
        /// Tamaño actual del caballero (1 = normal). Al presionar abajo se ENCOGE (lore: el nanocaballero cambia de tamaño)
        /// en vez de aplastarse; pasa suave de un tamaño al otro. El brazo, el escudo y el disparo se encogen con él.
        /// </summary>
        public float Size => size;
        private float size = 1f;

        /// <summary>Altura del hombro ahora mismo (de ahí salen las cápsulas).</summary>
        public float MuzzleHeight => config.muzzleHeight * size;
        /// <summary>Planeando con caída lenta.</summary>
        public bool IsSlowFalling => slowFalling;
        /// <summary>Fijado en su lugar (botón de Cuphead).</summary>
        public bool IsLocked => locked;
        /// <summary>Segundos de caída lenta que quedan (0..slowFallDuration).</summary>
        public float SlowFallLeft => slowFallLeft;
        public Vector2 Aim => aim;
        public Vector2 Position => motor.Position;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            Spawned = null;
        }

        private void Awake()
        {
            motor = GetComponent<CharacterMotor2D>();
            input = GetComponent<PlayerInputReader>();
            shooter = GetComponent<PlayerShooter>();
            bombs = GetComponent<PlayerBombs>();
            shield = GetComponent<PlayerShield>();
            health = GetComponent<PlayerHealth>();
            hurtbox = GetComponent<Hurtbox>();
            if (bodyVisual != null) bodyVisualBaseScale = bodyVisual.localScale;
            if (GetComponent<PlayerAudio>() == null) gameObject.AddComponent<PlayerAudio>(); // salto, daño, escudo, bombas

            if (config == null) Debug.LogError("[PlayerController] Falta asignar PlayerConfig.", this);
        }

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        private void Start()
        {
            lastSafePosition = motor.Position;
            // En Start todos los componentes del jugador ya hicieron su Awake.
            Spawned?.Invoke(this);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || config == null) return;

            // La caída lenta se decide cada cuadro en UpdateNormal; fuera de él la gravedad es normal.
            motor.FallGravityScale = 1f;
            motor.FallSpeedLimit = 0f;
            slowFalling = false;
            if (motor.IsGrounded) slowFallLeft = config.slowFallDuration;

            switch (state)
            {
                case State.Normal: UpdateNormal(); break;
                case State.Hurt:
                    stateTimer -= dt;
                    if (stateTimer <= 0f) state = State.Normal;
                    break;
                case State.Dead:
                    motor.SetHorizontalVelocity(0f);
                    break;
            }

            UpdateSize(dt);
            CheckFall();
        }

        private void UpdateSize(float dt)
        {
            float target = crouching ? config.crouchHeightFactor : 1f;
            size = Mathf.MoveTowards(size, target, config.shrinkSpeed * dt);
            if (bodyVisual != null) bodyVisual.localScale = bodyVisualBaseScale * size;
        }

        private void UpdateNormal()
        {
            Vector2 move = input.Move;
            bool shielding = shield != null && shield.IsActive;
            locked = input.LockHeld && !shielding;

            // Encogerse (abajo: solo en el suelo y sin fijar; fijado, S apunta en diagonal abajo).
            SetCrouch(motor.IsGrounded && move.y < -0.5f && !shielding && !locked);

            // Hacia dónde mira.
            if (Mathf.Abs(move.x) > 0.3f) motor.SetFacing(move.x > 0f ? 1 : -1);

            // Correr: velocidad constante, sin inercia (como Metal Slug). Fijado no se mueve.
            float speed = crouching ? config.crouchSpeed : config.runSpeed;
            if (shielding) speed *= config.shieldMoveSpeedMultiplier;
            if (locked) speed = motor.IsGrounded ? 0f : config.lockedAirSpeed;
            if (Time.time < slowUntil) speed *= slowMultiplier;
            else slowMultiplier = 1f;
            float target = Mathf.Abs(move.x) > 0.3f ? Mathf.Sign(move.x) * speed : 0f;
            float vx = motor.IsGrounded ? target : Mathf.Lerp(motor.Velocity.x, target, config.airControl);
            motor.SetHorizontalVelocity(vx);

            // Rebote de alvéolo: mientras dura el vuelo no hay salto (ni normal ni en el aire).
            if (bounceAirborne && motor.IsGrounded && Time.time > bounceTime + 0.15f) bounceAirborne = false;

            // Salto de altura fija + doble salto en el aire.
            if (motor.IsGrounded && !bounceAirborne) airJumpsUsed = 0;
            if (input.JumpPressed && !shielding && !bounceAirborne)
            {
                if (motor.IsGrounded)
                {
                    SetCrouch(false);
                    motor.Jump(config.jumpHeight, config.timeToApex);
                    Jumped?.Invoke(false);
                }
                else if (airJumpsUsed < config.airJumps)
                {
                    airJumpsUsed++;
                    motor.Jump(config.airJumpHeight, config.airJumpTimeToApex);
                    Jumped?.Invoke(true);
                }
            }

            // Caída lenta: mantener saltar mientras caes (se recarga al pisar el suelo).
            if (!motor.IsGrounded && motor.Velocity.y < 0f && input.JumpHeld && !shielding && slowFallLeft > 0f)
            {
                slowFalling = true;
                slowFallLeft -= Time.deltaTime;
                motor.FallGravityScale = config.slowFallGravityScale;
                motor.FallSpeedLimit = config.slowFallMaxSpeed;
            }

            // Apuntar.
            //  - Mouse / stick derecho: libre en cualquier ángulo y miras hacia donde apuntas
            //    (puedes correr hacia atrás disparando).
            //  - Teclas: 8 direcciones como Metal Slug (arriba, diagonales con dos teclas, abajo en el aire).
            switch (input.AimSource)
            {
                case AimSource.Mouse:
                {
                    UnityEngine.Camera cam = UnityEngine.Camera.main;
                    Vector2 shoulder = Position + new Vector2(0f, MuzzleHeight);
                    if (cam != null)
                    {
                        Vector3 sp = input.PointerScreenPosition;
                        sp.z = -cam.transform.position.z;
                        aim = AimMath.Toward(shoulder, cam.ScreenToWorldPoint(sp), motor.Facing);
                    }
                    break;
                }
                case AimSource.Stick:
                    aim = input.AimStick;
                    break;
                default:
                    if (crouching) aim = new Vector2(motor.Facing, 0f);
                    else if (locked) aim = AimMath.ResolveLocked(move, motor.Facing, motor.IsGrounded);
                    else aim = AimMath.Resolve(move, motor.Facing, motor.IsGrounded);
                    break;
            }
            if (input.AimSource != AimSource.Directional && Mathf.Abs(aim.x) > 0.05f)
                motor.SetFacing(aim.x > 0f ? 1 : -1);

            // Medicamentos.
            if (shooter != null)
            {
                if (input.MedicationStep != 0) shooter.CycleMedication(input.MedicationStep);
                if (input.DirectMedicationIndex >= 0) shooter.SelectMedication(input.DirectMedicationIndex);
            }

            // Escudo.
            if (shield != null) shield.SetWantsBlock(input.ShieldHeld);

            if (shielding) return;

            // Disparo (o cuchillazo automático si hay un enemigo pegado).
            if (shooter != null && (input.ShootPressed || input.ShootHeld))
            {
                shooter.TryAttack(aim, crouching, input.ShootPressed);

                // Disparar hacia abajo en el aire frena la caída (como un pequeño retroceso).
                if (!motor.IsGrounded && aim.y < -0.5f && !slowFalling)
                    motor.FallSpeedLimit = config.downShotMaxFallSpeed;
            }

            // Bomba de medicamento.
            if (bombs != null && input.BombPressed) bombs.TryThrow();

            if (motor.IsGrounded) lastSafePosition = motor.Position;
        }

        private void SetCrouch(bool value)
        {
            if (crouching == value) return;
            crouching = value;
            motor.SetColliderHeight(value ? config.standingColliderHeight * config.crouchHeightFactor : config.standingColliderHeight);
            if (hurtbox != null) hurtbox.SetHeightFactor(value ? config.crouchHeightFactor : 1f);
            // El dibujo se encoge poco a poco en UpdateSize.
        }

        private void CheckFall()
        {
            if (state == State.Dead || motor.Position.y > WalkableArea.KillY) return;

            // Caída al vacío: daño y regreso al último suelo seguro.
            DamageInfo info = DamageInfo.Create(config.fallDamage, DamageKind.Environment, Team.Neutral, motor.Position);
            info.blockable = false;
            health?.ApplyDamage(info);
            if (state != State.Dead) motor.Teleport(lastSafePosition + Vector2.up * 0.5f);
        }

        /// <summary>
        /// Rebote de alvéolo inflado: altura fija (siempre la misma, no depende de la caída ni del botón).
        /// No cuenta como salto: no lo gasta ni lo recarga, y durante el vuelo no se puede saltar.
        /// </summary>
        public void Bounce(float height, float velocity)
        {
            if (state == State.Dead) return;
            SetCrouch(false);
            motor.Jump(height, 2f * height / velocity);
            bounceAirborne = true;
            bounceTime = Time.time;
            Bounced?.Invoke();
        }

        /// <summary>Frena al jugador un momento (mucosidad: 0.5 = mitad de velocidad).</summary>
        public void ApplySlow(float multiplier, float duration)
        {
            if (Time.time >= slowUntil || multiplier < slowMultiplier) slowMultiplier = multiplier;
            slowUntil = Mathf.Max(slowUntil, Time.time + duration);
        }

        /// <summary>Lo llama PlayerHealth al recibir un golpe.</summary>
        public void OnHurt(float knockbackDirection, float knockbackForce)
        {
            if (state == State.Dead) return;
            state = State.Hurt;
            stateTimer = config.hurtStunTime;
            SetCrouch(false);
            motor.SetVelocity(new Vector2(knockbackDirection * knockbackForce, Mathf.Min(4f, knockbackForce)));
        }

        public void OnDied()
        {
            state = State.Dead;
            SetCrouch(false);
            motor.SetHorizontalVelocity(0f);
            shield?.SetWantsBlock(false);
        }

        public void Respawn(Vector2 position)
        {
            motor.Teleport(position);
            motor.SetFacing(1);
            lastSafePosition = position;
            state = State.Normal;
            health?.ResetHealth();
            shield?.ResetEnergy();
            bombs?.Refill(); // como en Metal Slug: vida nueva, bombas completas
        }
    }
}
